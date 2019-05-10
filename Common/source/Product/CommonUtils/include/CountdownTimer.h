#pragma once
#include <thread>
#include <string>
#include <chrono>
#include <mutex>
#include <condition_variable>
#include <future>

namespace Matrix
{
namespace Common
{
   /// A struct that can be used to kill a timer before it has elapsed
   /// </summary>
   struct timer_killer
   {
      // returns false if killed:
      template<class R, class P>
      bool wait_for(std::chrono::duration<R, P> const& time)
      {
         std::unique_lock<std::mutex> lock(_lock);
         return _cv.wait_for(lock, time, [&]
         {
            return _terminate;
         });
      }
      void Reset()
      {
         _terminate = false;
      }
      void kill()
      {
         std::unique_lock<std::mutex> lock(_lock);
         _terminate = true;
         _cv.notify_all();
      }
   private:
      std::condition_variable _cv;
      std::mutex _lock;
      bool _terminate = false;
   };

   /// <summary>
   /// A timer that allows a function to be called after a specified period of time
   /// or to be called every so many milliseconds
   ///
   ///Usage:
   ///to allow instance member function calls
   ///
   ///#include <functional>
   ///using namespace std::placeholders;
   ///class MyClass
   ///{
   ///  MyClass()
   ///  {
   ///    CountdownTimer timer;
   ///    std::function<void(std::string)> f = std::bind(&SocketInfo::PrintInfo, this, _1);
   ///    timer.TestThis(f, "constructor");
   ///
   ///    auto func = std::bind(&MyClass::MyMemberfunction, this, rxOffset);
   ///    timer.StartTimer(100, func);
   ///  }
   ///};
   /// </summary>
   class CountdownTimer
   {
      //****************************************
      // Constructors/Destructors
      //****************************************
   public:
      CountdownTimer()
      {
      }
      ~CountdownTimer()
      {
         StopAndWait(100);
      };
      //****************************************
      // Fields
      //****************************************
   protected:
      int _numMilliseconds;
      std::function<void()> _timeoutFunction;
      timer_killer _timerKiller;
      bool _stopCompleted;
      bool _onceOnly;

      //****************************************
      // Methods
      //****************************************
   public:
      //void TestThis(std::function<void(std::string)> functionToCall, std::string arg)
      //{
      //   functionToCall(arg);
      //}

      /// <summary>
      /// Starts a timer that will call functionToCall every numMilliseconds ms
      /// </summary>
      /// <param name="numMilliseconds">Number of milliseconds between calls.</param>
      /// <param name="functionToCall">the function to call.</param>
      /// <param name="onceOnly">true to quit after the first call of functionToCall.</param>
      void StartTimer(int numMilliseconds, std::function<void()> functionToCall, bool onceOnly = false)
      {
         _onceOnly = onceOnly;
         _numMilliseconds = numMilliseconds;
         _timeoutFunction = functionToCall;
         _timerKiller.Reset();
         std::thread monitorThread(&CountdownTimer::TaskFunction, this);
         monitorThread.detach();
      }
      static void TaskFunction(CountdownTimer* pTimer)
      {
         pTimer->_stopCompleted = false;
         while (!pTimer->_timerKiller.wait_for(std::chrono::milliseconds(pTimer->_numMilliseconds)))
         {
            pTimer->_timeoutFunction();
            if (pTimer->_onceOnly)
               break;
         }
         pTimer->_stopCompleted = true;
      }
      /// <summary>
      /// Sets a flag to tell the timer to stop.
      /// </summary>
      void Stop()
      {
         _timerKiller.kill();
      }
      /// <summary>
      /// Sets a flag to tell the timer to stop then loops every 10ms for up to num10ms 
      /// until IsStopCompleted is true before returning
      /// </summary>
      /// <param name="num10ms">Number of 10 millisecond periods to wait for stop.</param>
      void StopAndWait(int num10ms)
      {
         _timerKiller.kill();
         int quit = num10ms;
         while (!this->_stopCompleted && quit > 0)
         {
            std::this_thread::sleep_for(std::chrono::milliseconds(10));
            quit--;
         }
      }
      /// <summary>
      /// Returns true if the timer loop is no longer running
      /// </summary>
      bool IsStopCompleted()
      {
         return _stopCompleted;
      }
   };
}
}