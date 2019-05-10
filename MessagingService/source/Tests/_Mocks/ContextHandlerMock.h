#pragma once
#include <gmock/gmock.h>
#include "IContextHandler.h"

namespace testing
{
class ContextHandlerMock : public virtual Matrix::MsgService::CommunicationUtils::IContextHandler
{
public:
   virtual ~ContextHandlerMock() {};

   //IWorkerThread
   MOCK_METHOD0(GetName, std::string());
   MOCK_METHOD0(StartThread, void());
   MOCK_METHOD0(ShutDown, void());
   MOCK_METHOD0(IsShuttingDown, bool());
   MOCK_METHOD2(WaitForShutdown, bool(int, int));

   //IContextHandler
   MOCK_METHOD0(GetIOContext, boost::asio::io_context&());
#ifdef USING_SSL
   MOCK_METHOD0(GetSSLContext, boost::asio::ssl::context&());
#endif
};
}