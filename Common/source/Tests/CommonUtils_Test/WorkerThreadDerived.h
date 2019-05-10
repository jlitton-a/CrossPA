#include <gtest/gtest.h>

#include "WorkerThread.h"

using namespace Matrix::Common;

/// <summary>
/// WorkerThread is an abstract class so to test it, we must create a class that is not abstract
/// </summary>
class WorkerThreadDerived : public WorkerThread
{
public:
   int mRan;
   int mNumLoops;

   /// <summary>
   /// WorkerThread is an abstract class so to test it, we must create a class that is not abstract
   /// </summary>
   /// <param name="name">Name to use in base constructor.</param>
   /// <param name="msFreq">Frequency to use in base constructor.</param>
   /// <returns>a structure containing values indicating the values of standard command line args</returns>
   WorkerThreadDerived(std::string name) : WorkerThread(name)
   {
      mRan = false;
      mNumLoops = 0;
   }

   void Run() override
   {
      mRan = true;
      while (!_shuttingDown)
      {
         mNumLoops++;
         std::this_thread::sleep_for(std::chrono::milliseconds(50));
      }
      _shutdownComplete = true;
   }

};
