#include <gtest/gtest.h>

#include "TimedThreadDerived.h"

using namespace Matrix::Common;

//Test Fixture - use TEST_F when using a test fixture
class TimedThreadTest : public testing::Test {
protected:
   std::string threadName;
   std::unique_ptr<TimedThreadDerived> pTestThread;
   virtual void SetUp() {
      threadName = "My Thread";
      pTestThread = std::unique_ptr<TimedThreadDerived>(new TimedThreadDerived(threadName, 50));
      pTestThread->mPauseInit = false;
   }
   virtual void TearDown() {
      //this will be called after each test
      pTestThread->ShutDown();
      pTestThread->WaitForShutdown(10, 25);
      pTestThread = nullptr;
   }
};

// Tests IsListening
TEST_F(TimedThreadTest, IsListening_BeforeInit) {
   //Setup
   pTestThread->mPauseInit = true;
   pTestThread->StartThread();

   //Test
   auto result = pTestThread->IsListening();

   //Expectations
   EXPECT_FALSE(result);
   pTestThread->mPauseInit = false;
}
// Tests IsListening
TEST_F(TimedThreadTest, IsListening_Started) {
   //Setup
   //start the thread and wait for at least one loop to occur
   pTestThread->StartThread();
   std::this_thread::sleep_for(std::chrono::milliseconds(200));

   //Test
   auto result = pTestThread->IsListening();

   //Expectations
   EXPECT_TRUE(result);
}
// Tests IsListening
TEST_F(TimedThreadTest, IsListening_AfterShutDown) {
   //Setup
   //start the thread and wait for at least one loop to occur
   pTestThread->StartThread();
   pTestThread->ShutDown();
   while (!pTestThread->IsShutdownComplete());

   //Test
   auto result = pTestThread->IsListening();

   //Expectations
   EXPECT_FALSE(result);
}

// Tests IsShutdownComplete
TEST_F(TimedThreadTest, IsShutdownComplete_BeforeShutDown) {
   //Setup
   pTestThread->StartThread();
   std::this_thread::sleep_for(std::chrono::milliseconds(200));

   //Test
   auto result = pTestThread->IsShutdownComplete();

   //Expectations
   EXPECT_FALSE(result);
}
// Tests IsShutdownComplete
TEST_F(TimedThreadTest, IsShutdownComplete_AfterShutDown) {
   //Setup
   pTestThread->StartThread();
   std::this_thread::sleep_for(std::chrono::milliseconds(200));

   pTestThread->ShutDown();
   std::this_thread::sleep_for(std::chrono::milliseconds(200));

   //Test
   auto result = pTestThread->IsShutdownComplete();

   //Expectations
   EXPECT_TRUE(result);
}