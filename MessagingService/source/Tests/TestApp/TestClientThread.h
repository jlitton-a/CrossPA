#pragma once

#include <boost/asio.hpp>

#include "stdafx.h"
#include "TimedThread.h"
#include "Message.h"
#include "CommHandlerSignals.h"

namespace asio = boost::asio;
namespace CommunicationUtils = Matrix::MsgService::CommunicationUtils;

namespace Matrix
{
namespace MsgService
{
namespace CommunicationUtils
{
   class ContextHandler;
   class ClientComm;
}
}
}

namespace TestApp
{
   class TestClientThread : public Matrix::Common::TimedThread
   {
   public:
      TestClientThread(std::string ipAddress, std::string port, int clienttype, int clientID, int freqMS = 2000);
      ~TestClientThread();

   private:
      std::shared_ptr<CommunicationUtils::ContextHandler> _pContextHandler;
      std::shared_ptr<CommunicationUtils::ClientComm> _pClient;
      int _msgKey;
      Matrix::MsgService::CommonMessages::Message _msg;
      bool _sendRandomMsgs;
      bool _subscribe;

   public:
      void ShutDown() override;
      void ToggleSendMessages() { _sendRandomMsgs = !_sendRandomMsgs; }
      void ToggleSubscribe();
      void ShowDiagnostics();
   private:
      virtual bool Initialize() override;
      virtual bool ExecuteLoopActions() override;
      void SendAMessage();
      void HandleSocketStateChange(CommunicationUtils::SocketState oldState, CommunicationUtils::SocketState newState, CommunicationUtils::DisconnectReason reason);
      void HandleMsgRx(Matrix::MsgService::CommonMessages::Header* pMsg);
   };
}
