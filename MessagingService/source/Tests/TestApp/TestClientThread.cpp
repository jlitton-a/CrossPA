#include <boost/asio.hpp>
#include <memory>
#include <random>

#include "TestClientThread.h"
#include "ContextHandler.h"
#include "ClientComm.h"
#include "Logger.h"
#include "Message.h"

namespace CommonMessages = Matrix::MsgService::CommonMessages;
namespace CommunicationUtils = Matrix::MsgService::CommunicationUtils;

using namespace Matrix::Common;
using namespace TestApp;

TestClientThread::TestClientThread(std::string ipAddress, std::string port, int clienttype, int clientID, int freqMS)
      : TimedThread("TestClientThread", freqMS)
      , _msgKey(1)
      , _sendRandomMsgs(false)
      , _subscribe(false)
{
#ifdef USING_SSL
   boost::asio::ssl::context sslContext(boost::asio::ssl::context::sslv23);
   _pContextHandler = std::make_shared<CommunicationUtils::ContextHandler>(sslContext);
#else
   _pContextHandler = std::make_shared<CommunicationUtils::ContextHandler>();
#endif
   _pClient = std::make_shared<CommunicationUtils::ClientComm>(_pContextHandler, ipAddress, port, clienttype, clientID, 2000);
   auto callback = std::bind(&TestClientThread::HandleSocketStateChange, this, std::placeholders::_1, std::placeholders::_2, std::placeholders::_3);
   _pClient->AddSocketStateChangeObserver(callback);
   auto rxCallback = std::bind(&TestClientThread::HandleMsgRx, this, std::placeholders::_1);
   _pClient->AddMessageRxObserver(rxCallback);
}
void TestClientThread::ShowDiagnostics()
{
   if (_pClient != nullptr)
      _pClient->Diagnostics(CommunicationUtils::DiagnosticTypes::Display);
}
void TestClientThread::HandleSocketStateChange(CommunicationUtils::SocketState oldState, CommunicationUtils::SocketState newState, CommunicationUtils::DisconnectReason reason)
{
   LOG_MESSAGE(Logging::LogLevels::TRACE_LVL) << "Connection status changed oldState=" << (int)oldState << ", newState=" << (int)newState << ", reason=" << (int)reason;
}
void TestClientThread::HandleMsgRx(CommonMessages::Header* pMsg)
{
   if (pMsg != NULL)
   {
      LOG_MESSAGE(Logging::LogLevels::DEBUG_LVL) << "Received message with key=" << pMsg->msgkey() << ", type=" << pMsg->msgtypeid() << "";
   }
}

TestClientThread::~TestClientThread()
{
   ShutDown();
}

void TestClientThread::ShutDown()
{
   TimedThread::ShutDown();
   WaitForShutdown();
   if (_pClient)
   {
      _pClient->ShutDown();
      _pClient->WaitForShutdown(10, 10);
      _pClient = nullptr;
   }
   if (_pContextHandler)
   {
      _pContextHandler->ShutDown();
      _pContextHandler->WaitForShutdown(10, 10);
      _pContextHandler = nullptr;
   }
}

bool TestClientThread::Initialize()
{
   _pContextHandler->StartThread();
   _pClient->Connect();
   return true;
}
void TestClientThread::ToggleSubscribe()
{
   if (_pClient && _pClient->IsLoggedOn())
   {
      _subscribe = !_subscribe;
      _msgKey++;
      CommonMessages::Header payload;
      payload.set_msgkey(_msgKey);
      if (_subscribe)
      {
         payload.set_msgtypeid(CommonMessages::MsgType::SUBSCRIBE);
         LOG_MESSAGE(Logging::LogLevels::DEBUG_LVL) << "Sending Subscribe Message for clienttype 1 with key " << _msgKey << "!";
      }
      else
      {
         payload.set_msgtypeid(CommonMessages::MsgType::UNSUBSCRIBE);
         LOG_MESSAGE(Logging::LogLevels::DEBUG_LVL) << "Sending Unsubscribe Message for clienttype 1 with key " << _msgKey << "!";
      }

      CommonMessages::Subscribe request;
      request.set_clienttype(1);
      payload.set_allocated_msg(new std::string(request.SerializeAsString()));
      //and send it
      _pClient->SendMsg(payload);
   }
}

void TestClientThread::SendAMessage()
{
   if (_pClient->IsLoggedOn())
   {
      if (_sendRandomMsgs)
      {
         _msgKey++;
         CommonMessages::Header payload;
         payload.set_msgkey(_msgKey);
         payload.set_msgtypeid(CommonMessages::MsgType::CUSTOM);
         CommonMessages::Subscribe request;
         std::random_device rd;  //Will be used to obtain a seed for the random number engine
         std::mt19937 gen(rd()); //Standard mersenne_twister_engine seeded with rd()
         std::uniform_int_distribution<> dis(1, 6);
         request.set_clientid(dis(gen));
         payload.set_allocated_msg(new std::string(request.SerializeAsString()));
         //and send it
         LOG_MESSAGE(Logging::LogLevels::DEBUG_LVL) << "Sending CUSTOM message with key " << _msgKey << " - client id " << request.clientid() << "!";
         _pClient->SendMsg(payload);
      }
   }
}
bool TestClientThread::ExecuteLoopActions()
{
   SendAMessage();
   return true;
}
