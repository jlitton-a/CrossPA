#pragma once
#include "WorkerThread.h"

namespace testing
{
class WorkerThreadMock : public virtual Matrix::Common::IWorkerThread
{
public:
   WorkerThreadMock() {};
   virtual ~WorkerThreadMock() {};
   MOCK_METHOD0(GetName, std::string());
   MOCK_METHOD0(StartThread, void());
   MOCK_METHOD0(ShutDown, void());
   MOCK_METHOD0(IsShuttingDown, bool());
   MOCK_METHOD2(WaitForShutdown, bool(int, int));
};
}