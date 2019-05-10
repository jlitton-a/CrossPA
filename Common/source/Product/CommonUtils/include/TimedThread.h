#pragma once
#include <string>
#include <vector>
#include <thread>
#include <utility>
#include <memory>
#include <mutex>

#ifndef _WIN32
#include <unistd.h>
#endif

#include "../stdafx.h"
#include "WorkerThread.h"
#include "SharedFromThis.h"

namespace Matrix
{
namespace Common
{
   /// <summary>
   /// Abstract class that provides a simple loop that runs every loopFreqMS milliseconds.
   /// Derive and override the methods:
   ///   Initialize which will be called before the first loop
   ///   ExecuteLoopActions to perform the work - return false to end the run
   /// </summary>
   class TimedThread : public WorkerThread
   {
      //****************************************
      // Constructors/Destructors
      //****************************************
   public:
      /// <summary>
      /// Initializes a new instance of the <see cref="TimedThread"/> class.
      /// </summary>
      /// <param name="threadName">A name to use when logging</param>
      /// <param name="loopFreqMS">Milliseconds between loop execution.</param>
      COMMONUTILS_API TimedThread(std::string threadName, uint32_t  loopFreqMS);
      COMMONUTILS_API virtual ~TimedThread();

      //****************************************
      // Fields
      //****************************************
   protected:
      /// <summary>
      /// Will be true after Initialization and false just before shutdown is completed
      /// </summary>
      bool _isListening;
      /// <summary>
      /// Amount of time between calls to ExecuteLoopActions()
      /// </summary>
      uint32_t _loopFreqMS;

      //****************************************
      // Methods
      //****************************************
   public:
      /// <summary>
      /// True after Initialization is complete and just before Shutdown is completed
      /// </summary>
      /// <returns>
      ///   <c>true</c> if Run was called and the ExecuteLoopActions are currently running; otherwise, <c>false</c>.
      /// </returns>
      COMMONUTILS_API virtual bool IsListening() { return _isListening; }
   protected:
      /// <summary>
      /// Process used by the StartThread. It calls Initialize, then 
      /// call ExecuteLoopActions repeatedly until ShutDown() is called or if ExecuteLoopActions returns false.
      /// </summary>
      COMMONUTILS_API virtual void Run() override;
      /// <summary>
      /// Initializes this instance.  Occurs at the start of Run thread processing.
      /// Derived classes should override this function to perform thread specific initialization.
      /// </summary>
      COMMONUTILS_API virtual bool Initialize() { return true; }
      /// <summary>
      /// This will be run for each polling loop.
      /// Derived classes should override this function to perform thread specific loop functions.
      /// Return false to end the Run
      /// </summary>
      COMMONUTILS_API virtual bool ExecuteLoopActions()
      {
         return false;
      };

   private:
      //needed to to have shared_from_this work for derived classes 
      std::shared_ptr<TimedThread> shared_from_this() { return shared_from(this); }
      std::shared_ptr<const TimedThread> shared_from_this() const { return shared_from(this); }

   };
}
}