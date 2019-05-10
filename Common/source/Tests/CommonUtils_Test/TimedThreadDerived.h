#include <gtest/gtest.h>

#include "TimedThread.h"

using namespace Matrix::Common;

/// <summary>
/// TimedThread is an abstract class so to test it, we must create a class that is not abstract
/// </summary>
class TimedThreadDerived : public TimedThread
{
public:
   int mNumLoops;
   bool mIsInitialized;
   //Allows the test to Pause the initialization
   bool mPauseInit;

   /// <summary>
   /// WorkerThread is an abstract class so to test it, we must create a class that is not abstract
   /// </summary>
   /// <param name="name">Name to use in base constructor.</param>
   /// <param name="msFreq">Frequency to use in base constructor.</param>
   /// <returns>a structure containing values indicating the values of standard command line args</returns>
   TimedThreadDerived(std::string name, uint32_t msFreq) : TimedThread(name, msFreq)
   {
      mNumLoops = 0;
      mIsInitialized = false;
      mPauseInit = false;
   }

   /// <summary>
   /// Allow mPauseIt to control when it finishes
   /// </summary>
   bool Initialize() override
   {
      while (mPauseInit);
      mIsInitialized = true;
      return true;
   }
   /// <summary>
   /// Increment a loop counter
   /// </summary>
   bool ExecuteLoopActions() override
   {
      mNumLoops++;
      if (mNumLoops > 150)
         return false;
      return true;
   }
};
