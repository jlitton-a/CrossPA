#include <random>

#include "Logger.h"
#include "StringUtils.h"
#include "ClientMsgHandler.h"
#include "SubscriptionHandler.h"
#include "ClientManager.h"
#include "Message.h"
#include "MessageUtils.h"

using namespace Matrix::Common;
using namespace Matrix::MsgService::MessageThreads;

ClientMsgHandler::ClientMsgHandler(std::shared_ptr<CommunicationUtils::IContextHandler> pContextHandler, std::shared_ptr<ClientManager> pClientManager) 
      : CommHandler(pContextHandler, "", true)
      , _isAuthenticated(false)
      , _pClientManager(pClientManager)
{
}
ClientMsgHandler::~ClientMsgHandler() 
{
   _pClientManager = nullptr;
};
std::string ClientMsgHandler::GetDiagnosticsInfo()
{
   return StringUtils::Format("Client Type=%d; Client ID=%d; %s", _clientType, _clientID, CommHandler::GetDiagnosticsInfo().c_str());
}
std::string ClientMsgHandler::GetName()
{
   //TODO: reset the name to when these change
   if (_clientType > 0 || _clientID > 0)
      return StringUtils::Format("%s (%d, %d)", CommHandler::GetName().c_str(), _clientType, _clientID);
   else
      return CommHandler::GetName();
}
//When disconnecting, always shutdown completely
void ClientMsgHandler::HandleDisconnect(CommunicationUtils::DisconnectReason reason)
{
   _pClientManager->GetSubscriptionHandler()->RemoveSubscriptionsFor(this);
   //send LOGOFF message to subscribers
   if (_clientType > 0)
   {
      CommonMessages::Header msgToSend;
      msgToSend.set_msgtypeid(CommonMessages::MsgType::LOGOFF);
      CommonMessages::Logon msgLogoff;
      msgLogoff.set_clienttype(_clientType);
      msgLogoff.set_clientid(_clientID);
      msgToSend.set_allocated_msg(new std::string(msgLogoff.SerializeAsString()));
      _pClientManager->GetSubscriptionHandler()->SendToSubscribers(this, msgToSend);
   }
   _shuttingDown = true;
   CommHandler::HandleDisconnect(reason);
}
void ClientMsgHandler::HandleMessageReceived(std::unique_ptr<CommonMessages::Header> pMsg)
{
   if (pMsg == nullptr)
   {
      SendHeartbeat();
   }
   else
   {
      //if client is not authenticated and this is not a LOGON message, shut em down.
      if (!_isAuthenticated && pMsg->msgtypeid() != CommonMessages::MsgType::LOGON)
      {
         LOG_MESSAGE(Logging::LogLevels::DEBUG_LVL) << GetName() << ": Received " << CommonMessages::MessageUtils::ToString(pMsg->msgtypeid()) << " msg from UNAUTHENTICATED client.";
         ShutDown();
         return;
      }
      if (pMsg->origclienttype() == 0 && _clientType != 0)
      {
         pMsg->set_origclienttype(_clientType);
         pMsg->set_origclientid(_clientID);
      }
      bool sendAck = false;
      bool sendToSubscribers = false;
      switch (pMsg->msgtypeid())
      {
         case CommonMessages::MsgType::LOGON:
         {
            CommonMessages::Logon request;
            if (request.ParseFromString(pMsg->msg()))
            {
               _isAuthenticated = false;
               _clientType = request.clienttype();
               _clientID = request.clientid();
               //TODO: actual authentication of some kind
               _isAuthenticated = true;
               //TODO: if the client is not authenticated, need to call ShutDown after acking
               LOG_MESSAGE(Logging::LogLevels::INFO_LVL) << GetName() << ": Received LOGON request (key " << pMsg->msgkey() << ")";
               sendAck = true;
               sendToSubscribers = true;
            }
            break;
         }
         case CommonMessages::MsgType::LOGOFF:
         {
            CommonMessages::Logon request;
            if (request.ParseFromString(pMsg->msg()))
            {
               LOG_MESSAGE(Logging::LogLevels::INFO_LVL) << GetName() << ": Received LOGOFF request (key " << pMsg->msgkey() << ")";
               _clientType = 0;
               _clientID = 0;
               sendToSubscribers = true;
            }
            break;
         }
         case CommonMessages::MsgType::SUBSCRIBE:
         {
            CommonMessages::Subscribe request;
            if (request.ParseFromString(pMsg->msg()))
            {
               LOG_MESSAGE(Logging::LogLevels::DEBUG_LVL) << GetName() << ": Received SUBSCRIBE (key " << pMsg->msgkey() << ") for client (" << request.clienttype() << "," << request.clientid() << ")";
               _pClientManager->GetSubscriptionHandler()->AddSubscription(shared_from_this(), request);
//TODO: change to GetClients to handle when clienttype and/or clientid is 0
               //if the client we are subscribing to is logged on, send a logon message from that client
               auto pClient = _pClientManager->GetClient(request.clienttype(), request.clientid());
               if(pClient != nullptr)
               {
                  CommonMessages::Header sendLogon;
                  sendLogon.set_msgtypeid(CommonMessages::MsgType::LOGON);
                  sendLogon.set_origclienttype(request.clienttype());
                  sendLogon.set_origclientid(request.clientid());
                  CommonMessages::Logon msgLogon;
                  msgLogon.set_clienttype(request.clienttype());
                  msgLogon.set_clientid(request.clientid());
                  sendLogon.set_allocated_msg(new std::string(msgLogon.SerializeAsString()));
                  SendMsg(sendLogon);
               }
               sendAck = true;
            } 
            break;
         }
         case CommonMessages::MsgType::UNSUBSCRIBE:
         {
            CommonMessages::Subscribe request;
            if (request.ParseFromString(pMsg->msg()))
            {
               LOG_MESSAGE(Logging::LogLevels::DEBUG_LVL) << GetName() << ": Received UNSUBSCRIBE request (key " << pMsg->msgkey()
                     << ") for client (" << request.clienttype() << ", " << request.clientid() << ")";
               _pClientManager->GetSubscriptionHandler()->RemoveSubscription(this, request);
            }
            sendAck = true;
            break;
         }
         default:
         {
            LOG_MESSAGE(Logging::LogLevels::DEBUG_LVL) << GetName() << ": Received " << CommonMessages::MessageUtils::ToString(pMsg->msgtypeid()) << " msg (key " << pMsg->msgkey() << ")";
            //send it to the subscribers before giving it up to CommHandler::HandleMessageReceived
            sendToSubscribers = true;
         }
      }
      if (sendAck && !IsStopped())
      {
         if (pMsg->msgtypeid() != CommonMessages::MsgType::ACK)
         {
            CommonMessages::Header ackMsg;
            ackMsg.set_msgtypeid(Matrix::MsgService::CommonMessages::MsgType::ACK);
            ackMsg.set_msgkey(pMsg->msgkey());
            SendMsg(ackMsg);
         }
      }
      if (sendToSubscribers && !IsStopped())
      {
         SendMessageToSubscribers(pMsg.get());
      }
      CommHandler::HandleMessageReceived(move(pMsg));
   }
}
void ClientMsgHandler::SendMessageToSubscribers(CommonMessages::Header* pMsg)
{
   //if it is directed to a specific client, send it to that client only
   if (pMsg->destclienttype() > 0) 
   {
      auto pClient = _pClientManager->GetClient(pMsg->destclienttype(), pMsg->destclientid());
      if (pClient != nullptr)
      {
         LOG_MESSAGE(Logging::LogLevels::TRACE_LVL) << GetName() << ": Sending " << CommonMessages::MessageUtils::ToString(pMsg->msgtypeid()) << " msg (key " << pMsg->msgkey() << ") to dest client (" << pMsg->destclienttype() << ", " << pMsg->destclientid() << ")";
         pClient->SendMsg(*pMsg);
      }
   }
   //otherwise, send it to all subscribed clients
   else
   {
      _pClientManager->GetSubscriptionHandler()->SendToSubscribers(this, *pMsg);
   }

}