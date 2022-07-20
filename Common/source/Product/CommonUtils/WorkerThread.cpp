#include <iostream>
#include <utility>
#include <memory>
#include <vector>
#include <functional>

#include "WorkerThread.h"

using namespace Matrix::Common;

WorkerThread::WorkerThread(std::string threadName)
   : _shuttingDown(false)
   , _shutdownComplete(true)
   , mThreadName(threadName)
   , mThreadID("")
   , mpThread(nullptr)
{
}

WorkerThread::~WorkerThread()
{
//   ShutDown();
   if (mpThread != nullptr)
   {
//      WaitForShutdown();
      delete mpThread;
      mpThread = nullptr;
   }
}

void WorkerThread::StartThread()
{
   this->_shutdownComplete = false;
    std::thread* pWorkerThread = new std::thread(std::bind(&WorkerThread::Run, this));
    this->SetThread(pWorkerThread);
    pWorkerThread->detach();
}