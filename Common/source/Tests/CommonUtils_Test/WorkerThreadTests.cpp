#include <gtest/gtest.h>

#include "WorkerThreadDerived.h"

using namespace Matrix::Common;

//Test Fixture - use TEST_F when using a test fixture
class WorkerThreadTest : public testing::Test {
protected:
   std::string threadName;
   WorkerThreadDerived* pTestThread;
   virtual void SetUp() {
      threadName = "My Thread";
      pTestThread = new WorkerThreadDerived(threadName);
   }
   virtual void TearDown() {
      //this will be called after each test
      pTestThread->ShutDown();
      pTestThread->WaitForShutdown();
      delete pTestThread;
      pTestThread = nullptr;
   }
};

// Tests GetName
TEST_F(WorkerThreadTest, GetName) {
   //Setup

   //Test
   auto result = pTestThread->GetName();

   //Expectations
   EXPECT_EQ(result, threadName);
}

// Tests IsShutdownComplete
TEST_F(WorkerThreadTest, IsShutdownComplete_BeforeStop) {
   //Setup
   pTestThread->StartThread();

   //Test
   auto result = pTestThread->IsShutdownComplete();

   //Expectations
   EXPECT_FALSE(result);
}
// Tests GetThread
TEST_F(WorkerThreadTest, GetThread_NotStarted) {
   //Setup

   //Test
   auto result = pTestThread->GetThread();

   //Expectations
   EXPECT_EQ(result, nullptr);
}

// Tests GetThread
TEST_F(WorkerThreadTest, GetThread_Started) {
   //Setup
   pTestThread->StartThread();
   int retries = 1;
   while (pTestThread->mNumLoops < 1 && retries < 5)
   {
      std::this_thread::sleep_for(std::chrono::milliseconds(100));
      retries++;
   }

   //Test
   auto result = pTestThread->GetThread();

   //Expectations
   EXPECT_NE(result, nullptr);
}

// Tests GetThreadID
TEST_F(WorkerThreadTest, GetThreadID_NotStarted) {
   //Setup

   //Test
   auto result = pTestThread->GetThreadID();

   //Expectations
   EXPECT_EQ(result, "");
}

// Tests GetThreadID
TEST_F(WorkerThreadTest, GetThreadID_Started) {
   //Setup
   pTestThread->StartThread();
   int retries = 1;
   while (pTestThread->mNumLoops < 1 && retries < 5)
   {
      std::this_thread::sleep_for(std::chrono::milliseconds(100));
      retries++;
   }

   //Test
   auto result = pTestThread->GetThreadID();

   //Expectations
   EXPECT_NE(result, "");
}
