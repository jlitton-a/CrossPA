#ifdef _WIN32
#pragma warning( push )
#pragma warning( disable: 4251 4100 4146)
#endif
#include "Message.h"
#ifdef _WIN32
#pragma warning( pop )
#endif
#include <random>
#include <inttypes.h>
#include "Logger.h"
#include "CommHandler.h"
#include "IContextHandler.h"
#include "StringUtils.h"
#include "MessageUtils.h"

namespace CommonMessages = Matrix::MsgService::CommonMessages;

using namespace Matrix::Common;
using namespace Matrix::MsgService::CommunicationUtils;

CommHandler::CommHandler(
         std::shared_ptr<IContextHandler> pContextHandler
         , std::string name
         , bool isServer
         , int clientType
         , int clientID
         ) : WorkerThread(name)
      , _pContextHandler(pContextHandler)
      , _strand(pContextHandler->GetIOContext())
      , _pSocket(nullptr)
      , _name(name)
      , _isServer(isServer)
      , _stopped(false)
      , _socketState(SocketState::Disconnected)
      , _disconnectReason(DisconnectReason::None)
      , _pRxMsg(std::make_shared<CommonMessages::Message>())
      , _pSendMsg(std::make_shared<CommonMessages::Message>())
      , _pEmptyMsg(nullptr)
      , _emptySize(0)
      , _msgRxCount(0)
      , _msgSendCount(0)
      , _clientType(clientType)
      , _clientID(clientID)
{
#ifdef USING_SSL
   _pSocket = std::make_shared<ssl::stream<boost::asio::ip::tcp::socket>>(_pContextHandler->GetIOContext(), _pContextHandler->GetSSLContext());
   _pSocket->set_verify_mode(boost::asio::ssl::verify_peer);
   _pSocket->set_verify_callback(std::bind(&CommHandler::VerifyCertificate, this, std::placeholders::_1, std::placeholders::_2));
#else
   _pSocket = std::make_shared<tcp::socket>(pContextHandler->GetIOContext());
#endif
}
socket_type* CommHandler::GetSocket()
{
   return _pSocket.get();
}

#ifdef USING_SSL
bool CommHandler::VerifyCertificate(bool preVerified, boost::asio::ssl::verify_context& ctx)
{
   // The verify callback can be used to check whether the certificate that is
   // being presented is valid for the peer. For example, RFC 2818 describes
   // the steps involved in doing this for HTTPS. Consult the OpenSSL
   // documentation for more details. Note that the callback is called once
   // for each certificate in the certificate chain, starting from the root
   // certificate authority.

   // In this example we will simply print the certificate's subject name.
   char subject_name[256];
   X509* cert = X509_STORE_CTX_get_current_cert(ctx.native_handle());
   X509_NAME_oneline(X509_get_subject_name(cert), subject_name, 256);
   std::cout << "Verifying " << subject_name << "\n";

   return preVerified;
}
#endif
CommHandler::~CommHandler()
{
   ShutDown();
}
void CommHandler::ShutDown()
{ 
   //if we are not already shutting down
   if (!_shuttingDown)
   {
      LOG_MESSAGE(Logging::LogLevels::DEBUG_LVL) << GetName() << ": Shutting down";
      WorkerThread::ShutDown();
      Disconnect();
      WaitForShutdown();
   }
}
void CommHandler::Disconnect()
{
   _stopped = true;
   Disconnect(DisconnectReason::Manual);
}
void CommHandler::Disconnect(DisconnectReason reason)
{
   if (_socketState != SocketState::Disconnected)
   {
      CheckConnectionChanged(SocketState::Disconnecting, reason);
      _strand.dispatch(std::bind(&CommHandler::HandleDisconnect, shared_from_this(), reason));
   }
}
void CommHandler::HandleDisconnect(DisconnectReason reason)
{
   if (_pSocket != nullptr)
   {
      try
      {
         LOG_MESSAGE(Logging::LogLevels::DEBUG_LVL) << GetName() << ": Closing socket";
         boost::system::error_code ec;
#ifdef USING_SSL
         _pSocket->shutdown(ec);
#else
         _pSocket->shutdown(asio::socket_base::shutdown_type::shutdown_both, ec);
#endif
         _pSocket->lowest_layer().close();
      }
      catch (std::exception ex)
      {
         LOG_MESSAGE(Logging::LogLevels::DEBUG_LVL) << GetName() << ": Error disconnecting - " << ex.what();
      }
   }
   if (_shuttingDown)
   {
      _shutdownComplete = true;
   }

   CheckConnectionChanged(SocketState::Disconnected, reason);
}
void CommHandler::Run()
{
   auto pSocket = GetSocket();
   if (pSocket != nullptr)
   {
      _stopped = false;
      _shuttingDown = false;
      _shutdownComplete = false;
      LOG_MESSAGE(Logging::LogLevels::DEBUG_LVL) << GetName() << ": Started.";
      if (_name == "")
      {
         auto endpoint = pSocket->lowest_layer().remote_endpoint();
         _name = endpoint.address().to_string();
      }
      CheckConnectionChanged(SocketState::Connected, DisconnectReason::None);

#ifdef USING_SSL
      auto handshakeType = boost::asio::ssl::stream_base::client;
      if (_isServer)
         handshakeType = boost::asio::ssl::stream_base::server;
      pSocket->async_handshake(handshakeType,
            std::bind(&CommHandler::HandleHandshake, shared_from_this(), std::placeholders::_1));
#else
      StartRead();
#endif
   }
}
void CommHandler::HandleHandshake(const boost::system::error_code& error)
{
   if (error)
   {
      if ((boost::asio::error::eof == error)
            || (boost::asio::error::connection_reset == error)
            || (boost::asio::error::connection_aborted == error))
      {
         LOG_MESSAGE(Logging::LogLevels::INFO_LVL) << GetName() << ": Disconnected - " << error.message();
         Disconnect(DisconnectReason::ServerDisconnected);
      }
      else
      {
         LOG_MESSAGE(Logging::LogLevels::WARNING_LVL) << GetName() << ": HandleHandshake error - " << error.message();
      }
   }
   else
   {
      StartRead();
   }
}

std::string CommHandler::GetName()
{
   return _name;
}
void CommHandler::Diagnostics(DiagnosticTypes type)
{
   switch (type)
   {
      case DiagnosticTypes::Display:
      {
         LOG_MESSAGE(Logging::LogLevels::NO_LVL) 
               << "\n===========DIAGNOSTICS===========\nClient " << GetName()
               << ":  " << GetDiagnosticsInfo() 
               << "\n=================================";
         break;
      }
      default:
         //TODO: i don't know
         break;
   }
}
std::string CommHandler::GetDiagnosticsInfo()
{
   return StringUtils::Format("Connected=%d; Rx count: %d; Send count %d", IsConnected(), _msgRxCount , _msgSendCount);
}

void CommHandler::StartRead()
{
   if (!IsStopped() && _pSocket != nullptr)
   {
      LOG_MESSAGE(Logging::LogLevels::TRACE_LVL) << GetName() << ": waiting for read.";

      asio::async_read(*_pSocket, asio::buffer(_pRxMsg->GetBuffer(), CommonMessages::HDR_SIZE + CommonMessages::MAX_MESSAGE_SIZE), asio::transfer_at_least(1),
         std::bind(&CommHandler::HandleRead, shared_from_this(),
            std::placeholders::_1, std::placeholders::_2));
   }
}

void CommHandler::HandleRead(const boost::system::error_code& error, size_t /*bytes_transferred*/)
{
   if (error)
   {
      if ((boost::asio::error::eof == error)
            || (boost::asio::error::connection_reset == error)
            || (boost::asio::error::connection_aborted == error))
      {
         LOG_MESSAGE(Logging::LogLevels::INFO_LVL) << GetName() << ": Disconnected - " << error.message();
         Disconnect(DisconnectReason::ServerDisconnected);
      }
      else
      {
         LOG_MESSAGE(Logging::LogLevels::WARNING_LVL) << GetName() << ": HandleRead error - " << error.message().c_str();
         StartRead();
      }
   }
   else
   {
      _msgRxCount++;
      auto pMsg = std::unique_ptr<CommonMessages::Header>(new CommonMessages::Header());
      auto size = _pRxMsg->Decode(pMsg.get());
      if (size == 0)
      {
         //0 size message is a keep alive
         HandleMessageReceived(nullptr);
      }
      else if(size > 0)
      {
         HandleMessageReceived(std::move(pMsg));
      }
      else
      {
         LOG_MESSAGE(Logging::LogLevels::WARNING_LVL) << GetName() << ": Could not decode msg.";
      }
      StartRead();
   }
}
void CommHandler::OnMessageReceived(CommonMessages::Header* pMsg)
{
   _messageRxEvent(pMsg);
}
void CommHandler::HandleMessageReceived(std::unique_ptr<CommonMessages::Header> pMsg)
{
   if(pMsg != nullptr)
      OnMessageReceived(pMsg.get());
}
bool CommHandler::SendHeartbeat()
{
   if (!IsStopped() && _pSocket != nullptr)
   {
      if (_pEmptyMsg == nullptr)
      {
         _pEmptyMsg = std::unique_ptr<CommonMessages::Message>(new CommonMessages::Message());
         _emptySize = _pEmptyMsg->CreateEmptyMsg();
      }
      asio::async_write(*_pSocket, asio::buffer(_pEmptyMsg->GetBuffer(), _emptySize), asio::transfer_at_least(1),
         _strand.wrap(std::bind(&CommHandler::HandleWrite, shared_from_this(),
            std::placeholders::_1, std::placeholders::_2)));
   }
   return true;

}
bool CommHandler::SendMsg(CommonMessages::Header& msg)
{
   if (!IsStopped() && _pSocket != nullptr)
   {
      auto totalSize = _pSendMsg->PackMsg(msg);
      //TODO: _pSendMsg is shared - need to ensure additional calls to this SendMsg don't happen before it is completed in HandleWrite
      if (totalSize < 0)
      {
         LOG_MESSAGE(Logging::LogLevels::WARNING_LVL) << GetName() << ": Pack Message error!";
      }
      else
      {
         _msgSendCount++;
         LOG_MESSAGE(Logging::LogLevels::DEBUG_LVL) << GetName() << ": Sending " << CommonMessages::MessageUtils::ToString(msg.msgtypeid()) 
               << " msg (key=" << msg.msgkey() << ")" 
               << ((msg.destclienttype() == 0 || msg.destclienttype() == _clientType) ? "" : StringUtils::Format(" to client (%d,%d)", msg.destclienttype(), msg.destclientid()))
               << ((msg.origclienttype() == 0) ? "" : StringUtils::Format(" from client (%d,%d)", msg.origclienttype(), msg.origclientid()));


         asio::async_write(*_pSocket, asio::buffer(_pSendMsg->GetBuffer(), totalSize), asio::transfer_at_least(1),
            _strand.wrap(std::bind(&CommHandler::HandleWrite, shared_from_this(),
               std::placeholders::_1, std::placeholders::_2)));
         return true;
      }
   }
   return false;
}
void CommHandler::HandleWrite(const boost::system::error_code& error, size_t /*bytes_transferred*/)
{
   if (error)
   {
      if ((boost::asio::error::eof == error) ||
         (boost::asio::error::connection_reset == error))
      {
         LOG_MESSAGE(Logging::LogLevels::INFO_LVL) << GetName() << ": Disconnect - " << error.message();
         Disconnect(DisconnectReason::ServerDisconnected);
      }
      else
      {
         LOG_MESSAGE(Logging::LogLevels::WARNING_LVL) << GetName() << ": HandleWrite error - " << error.message();
      }
   }
   else
   {
      LOG_MESSAGE(Logging::LogLevels::TRACE_LVL) << GetName() << ": sent message.";
   }
}
bool CommHandler::CheckConnectionChanged(SocketState newState, DisconnectReason reason)
{
   if (_stopped && newState != SocketState::Disconnecting)
      newState = SocketState::Disconnected;
   if (_socketState != newState)
   {
      auto oldState = _socketState;
      auto oldConnected = IsConnected();
      _socketState = newState;
      _disconnectReason = reason;
      try
      {
         _socketStateChangeEvent(oldState, newState, reason);
         if (oldConnected != IsConnected())
            _connectionChangeEvent(IsConnected());
      }
      catch (std::exception ex)
      {
         LOG_MESSAGE(Logging::LogLevels::DEBUG_LVL) << GetName() << ": Error calling CheckConnectionChanged - " << ex.what();
      }
      if (_shuttingDown && newState == SocketState::Disconnected)
         _shutdownComplete = true;
      return true;
   }
   return false;
}
void CommHandler::OnConnectionChanged(bool isConnected)
{
   _connectionChangeEvent(isConnected);
}
/// <summary>
/// Register to receive notifications if IsConnected changes
/// </summary>
/// <param name="callback">A callback function that will be called when the parameter is updated.</param>
/// <returns>A connection that can be disconnected when notification is no longer needed</returns>
ConnectionChangeConnection CommHandler::AddConnectionChangeObserver(const ConnectionChangeCallback& callback)
{
   return _connectionChangeEvent.connect(callback);
}
/// <summary>
/// Register to receive notifications if SocketState changes
/// </summary>
/// <param name="callback">A callback function that will be called when the parameter is updated.</param>
/// <returns>A connection that can be disconnected when notification is no longer needed</returns>
SocketStateChangeConnection CommHandler::AddSocketStateChangeObserver(const SocketStateChangeCallback& callback)
{
   return _socketStateChangeEvent.connect(callback);
}
MessageRxConnection CommHandler::AddMessageRxObserver(const MessageRxCallback& callback)
{
   return _messageRxEvent.connect(callback);
}

