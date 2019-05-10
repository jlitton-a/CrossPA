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

namespace Matrix
{
namespace Common
{
   class IWorkerThread
   {
   public:
      virtual ~IWorkerThread() {}
      virtual std::string GetName() = 0;
      virtual void StartThread() = 0;
      virtual void ShutDown() = 0;
      virtual bool IsShuttingDown() = 0;
      virtual bool WaitForShutdown(int retryTimes = 40, int msBetweenRetries = 50) = 0;
   };

   /// <summary>
   /// Abstract class that provides a simple of way of starting a thread that runs the Run method.
   /// It provides the ability to Stop and determine when it is safe to destroy the class.
   /// Derive and perform the following
   ///   override the Run method to perform the thread actions
   ///   _shuttingDown is set by the ShutDown method.  It signifies that the Run method should shutdown
   ///   when the Run has been safely stopped, _shutdownComplete should be set to true
   /// </summary>
   class WorkerThread : public std::enable_shared_from_this<WorkerThread>, public virtual IWorkerThread
   {
      //****************************************
      // Constructors/Destructors
      //****************************************
   public:
      /// <summary>
      /// Initializes a new instance of the <see cref="WorkerThread"/> class.
      /// </summary>
      /// <param name="threadName">A name to use when logging</param>
      COMMONUTILS_API WorkerThread(std::string threadName);
      COMMONUTILS_API WorkerThread() {}
      COMMONUTILS_API virtual ~WorkerThread();

      //****************************************
      // Fields
      //****************************************
   protected:
      /// <summary>
      /// The flag indicating that the thread should shut down
      /// </summary>
      bool _shuttingDown;
      /// <summary>
      /// The flag that should be set when shutdown has completed (or thread was never started)
      /// </summary>
      bool _shutdownComplete;
      /// <summary>
      /// Name to use during logging
      /// </summary>
      std::string mThreadName;
      /// <summary>
      /// a String representation of the thread identifier
      /// </summary>
      std::string mThreadID;
      /// <summary>
      /// Amount of time between calls to ExecuteLoopActions()
      /// </summary>
      uint32_t mLoopFreqMS;
   private:
      /// <summary>
      /// The thread that is running
      /// </summary>
      std::thread* mpThread;

      //****************************************
      // Methods
      //****************************************
   public:
      /// <summary>
      /// Creates and start the thread that executes the Run method
      /// </summary>
      COMMONUTILS_API void StartThread();

      /// <summary>
      /// Sets a shutdown flag to signal that the thread should shut down
      /// </summary>
      COMMONUTILS_API virtual void ShutDown() { _shuttingDown = true; }
      /// <summary>
      /// Waits up to retryTimes*msBetweenRetries milliseconds for IsShutdownComplete() to be true.
      /// </summary>
      /// <param name="retryTimes">Number of times to retry before giving up.</param>
      /// <param name="msBetweenRetries">Milliseconds to wait between retries.</param>
      ///<returns>true if shutdown was completed before returning</returns>
      COMMONUTILS_API virtual bool WaitForShutdown(int retryTimes = 40, int msBetweenRetries = 50) override
      {
         while (!_shutdownComplete && retryTimes > 0)
         {
            std::this_thread::sleep_for(std::chrono::milliseconds(msBetweenRetries));
            retryTimes--;
         }
         return _shutdownComplete;
      }
      /// <summary>
      /// Gets the thread name (for logging and debugging)
      /// </summary>
      /// <returns>the name of the thread</returns>
      COMMONUTILS_API virtual std::string GetName() { return mThreadName; }
      /// <summary>
      /// Determines whether shutdown has completed.
      /// </summary>
      /// <returns>
      ///   <c>true</c> if shutdown is completed; otherwise, <c>false</c>.
      /// </returns>
      COMMONUTILS_API virtual bool IsShuttingDown() { return _shuttingDown; }
      /// <summary>
      /// Determines whether shutdown has completed.
      /// </summary>
      /// <returns>
      ///   <c>true</c> if shutdown is completed; otherwise, <c>false</c>.
      /// </returns>
      COMMONUTILS_API virtual bool IsShutdownComplete() { return _shutdownComplete; }
      /// <summary>
      /// Gets the instance of the thread.
      /// </summary>
      /// <returns>the thread instance</returns>
      COMMONUTILS_API std::thread* GetThread() { return mpThread; }
      /// <summary>
      /// Gets the thread identifier (for debugging)
      /// </summary>
      /// <returns>the thread instance identifier</returns>
      COMMONUTILS_API virtual std::string GetThreadID()
      {
         return mThreadID;
      }

   protected:
      /// <summary>
      /// Process used by the StartThread.
      /// </summary>
      COMMONUTILS_API virtual void Run() = 0;
      /// <summary>
      /// Sets the thread instance.
      /// </summary>
      /// <param name="pThread">The thread instance to set.</param>
      void SetThread(std::thread* pThread)
      {
         mpThread = pThread;
         //std::thread::id is not valid after calling detach or join
         //so get the id now and store it as a string
         if (mpThread)
         {
            std::stringstream ss;
            std::thread::id id = mpThread->get_id();
            ss << std::hex << id << std::dec;
            mThreadID = ss.str();
         }
      }
   };
}
}