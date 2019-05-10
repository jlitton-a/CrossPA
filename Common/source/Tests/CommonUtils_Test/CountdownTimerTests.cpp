#include <gtest/gtest.h>
#include <functional>
#include "CountdownTimer.h"

using namespace Matrix::Common;

class CountdownTimerTest : public testing::Test
{
protected:
   CountdownTimer* mpTimer;
   virtual void SetUp()
   {
      mpTimer = new CountdownTimer();
   }
   virtual void TearDown()
   {
      if (mpTimer)
      {
         delete mpTimer;
      }
   }
};

TEST_F(CountdownTimerTest, StartTimerCallsFunction)
{
   //Setup
   int times = 0;

   //Test
   mpTimer->StartTimer(10, [&]() -> void { times++; });

   //Expectations
   std::this_thread::sleep_for(std::chrono::milliseconds(150));
   mpTimer->StopAndWait(100);
   EXPECT_LE(2, times);
}

TEST_F(CountdownTimerTest, StopStopsTimer)
{
   //Setup
   bool timedout = false;
   mpTimer->StartTimer(50, [&]() -> void { timedout = true; });

   //Test
   mpTimer->Stop();
   std::this_thread::sleep_for(std::chrono::milliseconds(60));

   //Expectations
   EXPECT_FALSE(timedout);
}
TEST_F(CountdownTimerTest, StopAndWait_Waits)
{
   //Setup
   mpTimer->StartTimer(100, [&]() -> void { std::this_thread::sleep_for(std::chrono::milliseconds(110)); });

   //Test
   std::this_thread::sleep_for(std::chrono::milliseconds(50));
   mpTimer->StopAndWait(100);

   //Expectations
   EXPECT_TRUE(mpTimer->IsStopCompleted());
}

TEST_F(CountdownTimerTest, IsStopCompletedGetsSet)
{
   //Setup
   bool timedout = false;
   mpTimer->StartTimer(50, [&]() -> void { timedout = true; });

   //Test
   mpTimer->Stop();
   std::this_thread::sleep_for(std::chrono::milliseconds(70));

   //Expectations
   EXPECT_TRUE(mpTimer->IsStopCompleted());
}
TEST_F(CountdownTimerTest, IsStopCompleted_IsNotSetTilComplete)
{
   //Setup
   mpTimer->StartTimer(10, [&]() -> void { std::this_thread::sleep_for(std::chrono::milliseconds(300)); });

   //Test
   std::this_thread::sleep_for(std::chrono::milliseconds(100));
   mpTimer->Stop();

   //Expectations
   EXPECT_FALSE(mpTimer->IsStopCompleted());
}

TEST_F(CountdownTimerTest, StartTimer_OnceOnly_CallsOnce)
{
   //Setup
   int times = 0;

   //Test
   mpTimer->StartTimer(10, [&]() -> void { times++; }, true);

   //Expectations
   std::this_thread::sleep_for(std::chrono::milliseconds(150));
   mpTimer->StopAndWait(100);
   EXPECT_EQ(1, times);
}
