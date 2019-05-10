#include "SafeQueue.h"
#include <gtest/gtest.h>
#include <memory>
#include "CountdownTimer.h"

using namespace Matrix::Common;

class SafeQueueTest : public testing::Test
{
protected:
   virtual void SetUp()
   {
   }
   virtual void TearDown()
   {
      //this will be called after each test
   }
};

TEST(SafeQueueTest, GetName)
{
   //SETUP
   std::string name = "MyQueue";

   //TEST
   SafeQueue<int> underTest(0, name);

   //EXPECTATIONS
   EXPECT_EQ(name, underTest.GetName());
}

TEST(SafeQueueTest, GetMaxItems)
{
   //SETUP
   uint32_t maxItems = 15;

   //TEST
   SafeQueue<int> underTest(maxItems);

   //EXPECTATIONS
   EXPECT_EQ(maxItems, underTest.GetMaxItems());
}

TEST(SafeQueueTest, Enqueue_Adds)
{
   //SETUP
   SafeQueue<int> underTest;
   underTest.Enqueue(1);

   //TEST
   bool added = underTest.Enqueue(5);

   //EXPECTATIONS
   EXPECT_TRUE(added);
}
TEST(SafeQueueTest, Enqueue_MaxItems_DoesNotAdd)
{
   //SETUP
   SafeQueue<int> underTest(1);
   underTest.Enqueue(1);

   //TEST
   bool added = underTest.Enqueue(2);

   //EXPECTATIONS
   EXPECT_FALSE(added);
}

TEST(SafeQueueTest, Enqueue_Move_Adds)
{
   //SETUP
   SafeQueue<std::unique_ptr<int>> underTest(1);

   //TEST
   bool added = underTest.Enqueue_Move(std::unique_ptr<int>(new int(2)));

   //EXPECTATIONS
   EXPECT_TRUE(added);
}
TEST(SafeQueueTest, Enqueue_Move_MaxItems_DoesNotAdd)
{
   //SETUP
   SafeQueue<std::unique_ptr<int>> underTest(1);
   underTest.Enqueue_Move(std::unique_ptr<int>(new int(3)));

   //TEST
   bool added = underTest.Enqueue_Move(std::unique_ptr<int>(new int(2)));

   //EXPECTATIONS
   EXPECT_FALSE(added);
}

TEST(SafeQueueTest, Dequeue_GetsItem)
{
   //SETUP
   int item = 23;
   SafeQueue<int> underTest;
   underTest.Enqueue(item);
   underTest.Enqueue(59);
   underTest.Enqueue(33);

   //TEST
   int removed;
   bool success = underTest.Dequeue(removed);

   //EXPECTATIONS
   EXPECT_EQ(item, removed);
   EXPECT_TRUE(success);
}
TEST(SafeQueueTest, Dequeue_RemovesItem)
{
   //SETUP
   int item = 23;
   SafeQueue<int> underTest;
   underTest.Enqueue(item);
   underTest.Enqueue(59);
   underTest.Enqueue(33);

   //TEST
   int removed;
   underTest.Dequeue(removed);

   //EXPECTATIONS
   EXPECT_EQ((int)underTest.size(), 2);
}
TEST(SafeQueueTest, Dequeue_TimesOut)
{
   //SETUP
   SafeQueue<int> underTest;

   //TEST
   int removed;
   bool success = underTest.Dequeue(removed, 500);

   //EXPECTATIONS
   EXPECT_FALSE(success);
}
TEST(SafeQueueTest, Stop_StopsDequeueWait)
{
   //SETUP
   SafeQueue<int> underTest;

   //TEST
   int removed;
   CountdownTimer timer;
   timer.StartTimer(100, [&underTest, &timer]() -> void
   {
      underTest.Stop(); 
   }, true);

   bool success = underTest.Dequeue(removed);

   //EXPECTATIONS
   EXPECT_FALSE(success);
}
TEST(SafeQueueTest, Restart_AllowsDequeueWait)
{
   //SETUP
   SafeQueue<int> underTest;
   int removed;
   underTest.Stop();
   underTest.Restart();
   CountdownTimer timer;
   timer.StartTimer(100, [&underTest, &timer]() -> void
   {
      underTest.Stop();
   }, true);

   //TEST
   bool success = underTest.Dequeue(removed);

   //EXPECTATIONS
   EXPECT_FALSE(success);
}

TEST(SafeQueueTest, TryDequeue_NoItems_ReturnsFalse)
{
   //SETUP
   SafeQueue<int> underTest;

   //TEST
   int removed = 0;
   auto success = underTest.TryDequeue(removed);

   //EXPECTATIONS
   EXPECT_FALSE(success);
}
TEST(SafeQueueTest, TryDequeue_ItemExists_ReturnsTrue)
{
   //SETUP
   int item = 23;
   SafeQueue<int> underTest;
   underTest.Enqueue(item);

   //TEST
   int removed = 0;
   auto success = underTest.TryDequeue(removed);

   //EXPECTATIONS
   EXPECT_TRUE(success);
   EXPECT_EQ(item, removed);
}
