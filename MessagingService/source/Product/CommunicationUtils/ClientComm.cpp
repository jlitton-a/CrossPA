#include <boost/asio.hpp>
#include <memory>

#include "ClientComm.h"
#include "ContextHandler.h"
#include "StringUtils.h"
#include "Logger.h"
#include "CountdownTimer.h"
#include "Message.h"
#include "SubscriberMessageLists.h"

using namespace Matrix::Common;
using namespace Matrix::MsgService::CommunicationUtils;

ClientComm::ClientComm(std::shared_ptr<IContextHandler> pContextHandler, const std::string ipAddress, const std::string port
         , int clientType, int clientID
         , uint32_t reconnectRetryTimeMS, const std::string name
         , const Matrix::MsgService::CommonMessages::Logon* const pLogonSubMsg
         , bool trackSentMessages
         , std::shared_ptr<ISubscriberMessageLists> pSubscriberMsgLists)
      : CommHandler(pContextHandler, StringUtils::Format("%s: %s-%s", name.c_str(), ipAddress.c_str(), port.c_str()), false, clientType, clientID)
      , _ipAddress(ipAddress)
      , _port(port)
      , _reconnectRetryTimeMS(reconnectRetryTimeMS)
      , _resolver(pContextHandler->GetIOContext())
      , _isLoggedOn(false)
      , _msgKey(1)
{
#ifdef USING_SSL
   pContextHandler->GetSSLContext().set_default_verify_paths();
#endif
   _pLogonMsg = std::unique_ptr<Matrix::MsgService::CommonMessages::Header>(new CommonMessages::Header());
   _pLogonMsg->set_msgtypeid(CommonMessages::MsgType::LOGON);
   _pLogonMsg->set_msgkey(_msgKey++);
   _pLogonMsg->set_origclienttype(clientType);
   _pLogonMsg->set_origclientid(clientID);
   if (pLogonSubMsg != nullptr)
      _pLogonMsg->set_allocated_msg(new std::string(pLogonSubMsg->SerializeAsString()));
   else
   {
      Matrix::MsgService::CommonMessages::Logon logonMsg;
      logonMsg.set_clienttype(clientType);
      logonMsg.set_clientid(clientID);
      _pLogonMsg->set_allocated_msg(new std::string(logonMsg.SerializeAsString()));
   }
   if (trackSentMessages)
   {
      _pSubscriberMsgLists = (pSubscriberMsgLists != nullptr) ? pSubscriberMsgLists : std::make_shared<SubscriberMessageLists>();
   }
}

ClientComm::~ClientComm()
{
   _isLoggedOn = false;
}
bool ClientComm::Connect()
{
   if (!_shuttingDown)
   {
      _stopped = false;
      _strand.dispatch(std::bind(&ClientComm::WrappedConnect, shared_from_this(), false));
      return true;
   }
   return false;
}
void ClientComm::RetryConnect()
{
   if (!_shuttingDown && !_stopped && GetSocketState() != SocketState::Connecting)
   {
      _strand.dispatch(std::bind(&ClientComm::WrappedConnect, shared_from_this(), true));
   }
}
bool ClientComm::WrappedConnect(bool isRetry)
{
   if (_shuttingDown || _stopped)
   {
      _isLoggedOn = false;
      CheckConnectionChanged(SocketState::Disconnected, DisconnectReason::Manual);
      return false;
   }
   LOG_MESSAGE(isRetry ? Logging::LogLevels::TRACE_LVL: Logging::LogLevels::INFO_LVL) << "Client " << GetName() << ": Attempting to connect";
   try
   {
      _isLoggedOn = false;
      CheckConnectionChanged(SocketState::Connecting);
      asio::ip::tcp::resolver::query query(_ipAddress, _port);
      boost::system::error_code error = boost::asio::error::host_not_found;
      boost::asio::ip::tcp::resolver::iterator endpoint_iterator = _resolver.resolve(query, error);
      if(error)
      {
         LOG_MESSAGE(isRetry ? Logging::LogLevels::TRACE_LVL : Logging::LogLevels::WARNING_LVL) << "Client " << GetName() << ": Could not resolve - " << error.message();
      }
      else
      {
         LOG_MESSAGE(Logging::LogLevels::TRACE_LVL) << "Client " << GetName() << ": Resolved";
         if (_pSocket != nullptr)
         {
            _pSocket.reset(new boost::asio::ip::tcp::socket(_pContextHandler->GetIOContext()));
            LOG_MESSAGE(Logging::LogLevels::TRACE_LVL) << "Client " << GetName() << ": pSocket is not null";
            boost::asio::ip::tcp::resolver::iterator end;
            error = boost::asio::error::host_not_found;
            while (error && endpoint_iterator != end && !(_shuttingDown || _stopped))
            {
               _pSocket->lowest_layer().connect(*endpoint_iterator, error);
               if (error)
               {
                  LOG_MESSAGE(isRetry ? Logging::LogLevels::TRACE_LVL : Logging::LogLevels::INFO_LVL) << "Client " << GetName() << ": Did not connect-" << error.message();
               }
               else
               {
                  ConnectComplete(endpoint_iterator);
                  return true;
               }
               endpoint_iterator++;
            }
         }
      }
   }
   catch (std::exception ex)
   {
      LOG_MESSAGE(Logging::LogLevels::EXCEPTION_LVL) << "Client " << GetName() << ": Exception connecting - ", ex.what();
   }
   _isLoggedOn = false;
   CheckConnectionChanged(SocketState::Disconnected, DisconnectReason::CouldNotConnect);
   if (_reconnectRetryTimeMS > 0)
   {
      auto func = std::bind(&ClientComm::RetryConnect, shared_from_this());
      _reconnectTimer.StartTimer(_reconnectRetryTimeMS, func, true);
   }
   return false;
}
void ClientComm::ConnectComplete(asio::ip::tcp::resolver::iterator iterator)
{
   LOG_MESSAGE(Logging::LogLevels::INFO_LVL) << "Client " << GetName() << ":  Connected to server " << iterator->host_name() << "!";
   _isLoggedOn = false;
   CheckConnectionChanged(SocketState::Connected);

   StartRead();
   if(_pLogonMsg != nullptr)
      SendMsg(*_pLogonMsg);
}
/// <summary>
/// Raises the Connection Changed event
/// </summary>
/// <param name="isConnected">true if it is now connected, false if it is disconnected</param>
void ClientComm::OnConnectionChanged(bool isConnected)
{
   if (isConnected)
   {
      if (!_isLoggedOn)
      {
         SendMsg(*_pLogonMsg);
      }
   }
   else
   {
      _isLoggedOn = false;
   }

   CommHandler::OnConnectionChanged(isConnected);
}
bool ClientComm::SendAckMessage(const Matrix::MsgService::CommonMessages::Header msgToAck)
{
   return SendCommonMsgInternal(CommonMessages::MsgType::ACK, nullptr, msgToAck.msgkey(), 0, msgToAck.origclienttype(), msgToAck.origclientid());
}
bool ClientComm::SendNackMessage(const Matrix::MsgService::CommonMessages::Header msgToNack
   , int reason
   , const std::string details )
{
   CommonMessages::NackDetails nackDetails;
   nackDetails.set_reason(reason);
   nackDetails.set_details(details);
   return SendCommonMsgInternal(CommonMessages::MsgType::NACK, &nackDetails, msgToNack.msgkey(), 0, msgToNack.origclienttype(), msgToNack.origclientid());
}

int ClientComm::SendCommonMsg(CommonMessages::MsgType msgType
   , const google::protobuf::MessageLite* const pMessage
   , int topic
   , int destClientType
   , int destClientID
   , bool isArchived)
{
   int msgKey = _msgKey++;
   return SendCommonMsgInternal(msgType, pMessage, msgKey, topic, destClientType, destClientID, isArchived);
}

std::unique_ptr<CommonMessages::Header> ClientComm::SendCommonMsgAndWait(CommonMessages::MsgType msgType
   , const google::protobuf::MessageLite* const pMessage
   , int topic
   , int destClientType
   , int destClientID
   , int waitTimeoutMS)
{
   int msgKey = _msgKey++;
   auto pWaitResponse = std::make_shared<WaitResponse>();
   _waitResponses.insert(std::make_pair(msgKey, pWaitResponse));
   SendCommonMsgInternal(msgType, pMessage, msgKey, topic, destClientType, destClientID, false);
   auto pRxMsg = pWaitResponse->Wait(waitTimeoutMS);
   _waitResponses.erase(msgKey);
   return pRxMsg;
}

int ClientComm::SendCommonMsgInternal(CommonMessages::MsgType msgType
      , const google::protobuf::MessageLite* const pMessage
      , int msgKey
      , int topic
      , int destClientType
      , int destClientID
      , bool isArchived )
{
   CommonMessages::Header headerMsg;
   headerMsg.set_msgtypeid(msgType);
   headerMsg.set_topic(topic);
   headerMsg.set_destclienttype(destClientType);
   headerMsg.set_destclientid(destClientID);
   headerMsg.set_msgkey(msgKey);
   headerMsg.set_isarchived(isArchived);
   if (pMessage != nullptr)
      headerMsg.set_allocated_msg(new std::string(pMessage->SerializeAsString()));
   std::vector<int> ackKeyList;
   if (_pSubscriberMsgLists != nullptr)
   {
      if(NeedToTrackSentMsg(&headerMsg))
         _pSubscriberMsgLists->AddSentMessage(headerMsg);

      //if this message is for a specific client, add any acks to the message
      if (destClientType > 0)
      {
         ackKeyList = _pSubscriberMsgLists->GetNeedToAckList(destClientType, destClientID);
         for each (auto key in ackKeyList)
         {
            headerMsg.add_ackkeys(key);
         }
      }

   }
   if (SendMsg(headerMsg))
   {
      if (_pSubscriberMsgLists != nullptr && destClientType > 0)
      {
         _pSubscriberMsgLists->RemoveFromNeedToAckList(destClientType, destClientID, ackKeyList);
      }

   }
   return msgKey;
}

ClientComm::OnlineStatus ClientComm::IsClientOnline(int clientType, int clientID)
{
   if (_pSubscriberMsgLists == nullptr)
      return ClientComm::OnlineStatus::UNKNOWN;

   return _pSubscriberMsgLists->IsClientOnline(clientType, clientID) ? ClientComm::OnlineStatus::OnLine : ClientComm::OnlineStatus::OffLine;
}

bool ClientComm::Subscribe(CommonMessages::ClientTypes clientType, int clientID, bool subscribe)
{
   bool changed = false;
   CommonMessages::MsgType msgtype;
   CommonMessages::SubscriptionParams params((int)clientType, clientID);

   CommonMessages::Subscribe msgToSend;
   msgToSend.set_clienttype((int)clientType);
   if (clientID > 0)
      msgToSend.set_clientid(clientID);

   if (subscribe)
   {
      msgtype = CommonMessages::MsgType::SUBSCRIBE;
      if (std::find(_subscriptions.begin(), _subscriptions.end(), params) == _subscriptions.end())
      {
         _subscriptions.push_back(params);
         changed = true;
      }
   }
   else
   {
      msgtype = CommonMessages::MsgType::UNSUBSCRIBE;
      const auto last = std::remove(_subscriptions.begin(), _subscriptions.end(), params);
      changed = (last != _subscriptions.end());
      _subscriptions.erase(last, _subscriptions.end());
   }

   if (_isLoggedOn)
      SendCommonMsg(msgtype, &msgToSend);
   return changed;
}

void ClientComm::SendSubscribeMessages()
{
   CommonMessages::Subscribe msgToSend;
   for (auto params : _subscriptions)
   {
      msgToSend.set_clienttype((int)params._clientType);
      if (params._clientID > 0)
         msgToSend.set_clientid(params._clientID);

      SendCommonMsg(CommonMessages::MsgType::SUBSCRIBE, &msgToSend);
   }
}

void ClientComm::HandleDisconnect(DisconnectReason reason)
{
   CommHandler::HandleDisconnect(reason);
   //if we should retry connecting and we are not shutting down
   if (_reconnectRetryTimeMS > 0 && reason != DisconnectReason::None && reason != DisconnectReason::Manual)
   {
      auto func = std::bind(&ClientComm::RetryConnect, shared_from_this());
      _reconnectTimer.StartTimer(_reconnectRetryTimeMS, func, true);
   }
}
void ClientComm::Disconnect(DisconnectReason reason)
{
   _reconnectTimer.Stop();
   _isLoggedOn = false;
   CommHandler::Disconnect(reason);
}

void ClientComm::HandleMessageReceived(std::unique_ptr<CommonMessages::Header> pMsg)
{
   if (pMsg != nullptr)
   {
      LOG_MESSAGE(Logging::LogLevels::DEBUG_LVL) << GetName() << ": Received " << CommonMessages::MessageUtils::ToString(pMsg->msgtypeid()) << " msg (key="
         << pMsg->msgkey() << ")" << ((pMsg->origclienttype() == 0) ? "" : StringUtils::Format(" from client (%d, %d)", pMsg->origclienttype(), pMsg->origclientid()));
      if (pMsg->msgtypeid() == CommonMessages::MsgType::ACK && pMsg->msgkey() == _pLogonMsg->msgkey())
      {
         //we are now logged on, send subscribe messages
         _isLoggedOn = true;
         SendSubscribeMessages();
      }
      //if we are tracking
      if (_pSubscriberMsgLists != nullptr)
      {
         if (pMsg->msgtypeid() == CommonMessages::MsgType::LOGOFF)
         {
            _pSubscriberMsgLists->SetClientOnLine(pMsg->origclienttype(), pMsg->origclientid(), false);
         }
         else
         {
            _pSubscriberMsgLists->SetClientOnLine(pMsg->origclienttype(), pMsg->origclientid(), true);
            int replyMsgKey = 0;
            //if this is a reply to a message, remove it from sent messages
            if (pMsg->msgtypeid() == CommonMessages::MsgType::ACK)
               replyMsgKey = pMsg->msgkey();
            else
               replyMsgKey = pMsg->replymsgkey();
            if (replyMsgKey > 0)
               _pSubscriberMsgLists->RemoveSentMessage(pMsg->origclienttype(), pMsg->origclientid(), replyMsgKey);
         }
         _pSubscriberMsgLists->RemoveSentMessages(pMsg->origclienttype(), pMsg->origclientid(), pMsg->ackkeys());
      }

      CommHandler::OnMessageReceived(pMsg.get());

      //TODO: eventually add the actual tracking of sent messages and acks and only ack periodically
      bool needToAck = NeedToAckRxMsg(pMsg.get());
      if (needToAck)
         SendAckMessage(*pMsg.get());

      int replyMsgKey = 0;
      //if this is a reply to a message, signal if there is one waiting in SendCommonMessageAndWait
      if (pMsg->msgtypeid() == CommonMessages::MsgType::ACK)
         replyMsgKey = pMsg->msgkey();
      else
         replyMsgKey = pMsg->replymsgkey();
      if(replyMsgKey > 0)
      {
         auto find = _waitResponses.find(pMsg->msgkey());
         if (find != _waitResponses.end())
         {
            find->second->Signal(std::move(pMsg));
            return;
         }
      }
      //if there are any acks, signal there is one waiting in SendCommonMessageAndWait
      for(auto key: pMsg->ackkeys())
      {
         //if this acked key matches the received message and it is an ack msg, we've already set the event
         auto find = _waitResponses.find(key);
         if (find != _waitResponses.end())
         {
            find->second->Signal(std::move(pMsg));
            return;
         }
      }
   }
}
/// <summary>
/// Return true if an ack should be sent for this msg
/// </summary>
/// <param name="rxMsg">the message to check</param>
/// <returns>true if an ack should be sent</returns>
bool ClientComm::NeedToAckRxMsg(const Matrix::MsgService::CommonMessages::Header* const pRxMsg)
{
   //only CUSTOM message sent to a specific client should be acked
   return pRxMsg->msgtypeid() == CommonMessages::MsgType::CUSTOM && pRxMsg->destclienttype() > 0;
}
/// <summary>
/// Return true if this msg needs to be tracked and resent until acked
/// </summary>
/// <param name="sentMsg">the message to check</param>
/// <returns>true if we should resend if not acked</returns>
bool ClientComm::NeedToTrackSentMsg(const Matrix::MsgService::CommonMessages::Header* const pSentMsg)
{
   //only CUSTOM message sent to a specific client should be acked
   return pSentMsg->msgtypeid() == CommonMessages::MsgType::CUSTOM && pSentMsg->destclienttype() > 0;
}
