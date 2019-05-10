#include <iostream>
#include <utility>
#include <memory>
#include <vector>

#include "TimedThread.h"

using namespace Matrix::Common;

TimedThread::TimedThread(std::string threadName, uint32_t loopFreqMS) : WorkerThread(threadName)
   , _isListening(false)
   , _loopFreqMS(loopFreqMS)
{
}

TimedThread::~TimedThread()
{
}

void TimedThread::Run()
{
   if (Initialize())
   {
      _shutdownComplete = false;
      _isListening = true;

      while (!_shuttingDown)
      {
         if (!ExecuteLoopActions())
         {
            break;
         }
         std::this_thread::sleep_for(std::chrono::milliseconds(_loopFreqMS));
      }
   }
   _isListening = false;
   _shutdownComplete = true;
}

