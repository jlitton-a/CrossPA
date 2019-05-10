#pragma once
#include <gmock/gmock.h>

#include "WorkerThreadMock.h"
#include "ClientMsgHandler.h"
#include "CommHandlerSignals.h"

namespace CommunicationUtils= Matrix::MsgService::CommunicationUtils;
namespace MessageThreads = Matrix::MsgService::MessageThreads;

namespace testing
{
   class ClientMsgHandlerMock : public MessageThreads::IClientMsgHandler
   {
   public:
      ClientMsgHandlerMock() {};
      virtual ~ClientMsgHandlerMock() {};
      //IWorkerThread
      MOCK_METHOD0(GetName, std::string());
      MOCK_METHOD0(StartThread, void());
      MOCK_METHOD0(ShutDown, void());
      MOCK_METHOD0(IsShuttingDown, bool());
      MOCK_METHOD2(WaitForShutdown, bool(int, int));

      //ICommHandler
      MOCK_METHOD1(SendMsg, bool(Matrix::MsgService::CommonMessages::Header&));
      MOCK_METHOD0(IsConnected, bool());
      MOCK_METHOD0(Disconnect, void());
      MOCK_METHOD1(AddConnectionChangeObserver, CommunicationUtils::ConnectionChangeConnection(const CommunicationUtils::ConnectionChangeCallback&));
      MOCK_METHOD1(AddSocketStateChangeObserver, CommunicationUtils::SocketStateChangeConnection(const CommunicationUtils::SocketStateChangeCallback&));
      MOCK_METHOD1(AddMessageRxObserver, CommunicationUtils::MessageRxConnection(const CommunicationUtils::MessageRxCallback&));
      MOCK_METHOD1(Diagnostics, void(CommunicationUtils::DiagnosticTypes));
      MOCK_METHOD0(GetDiagnosticsInfo, std::string());

      //IClientMsgHandler
      MOCK_METHOD0(GetClientType, int());
      MOCK_METHOD0(GetClientID, int());
   };

}