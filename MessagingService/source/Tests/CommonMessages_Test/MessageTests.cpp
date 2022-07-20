#include <gtest/gtest.h>

#include "MessageUtils.h"
#include "Message.h"

using namespace Matrix::MsgService;
using namespace Matrix::MsgService::CommonMessages;

//Test Fixture - use TEST_F when using a test fixture
class MessageTest : public testing::Test {
protected:
   virtual void SetUp() {
   }
   virtual void TearDown() {
   }
};

// Tests CreateEmptyMsg
TEST(MessageTest, CreateEmptyMsg) {
   //Setup

   //Test
   Message msg;
   auto size = msg.CreateEmptyMsg();

   //Expectations
   EXPECT_EQ(HDR_SIZE, (int)size);
}

// Tests PackMsg
TEST(MessageTest, PackMsg) {
   //Setup
   Header myMsg;
   myMsg.set_msgkey(5);
   Message msg;
    
   //Test
   int totalSize = msg.PackMsg(myMsg);

   //Expectations
   EXPECT_GT(totalSize, 0);
}

// Tests Decode
TEST(MessageTest, Decode) {
   //Setup
   Header myMsg;
   int key = 5;
   myMsg.set_msgkey(key);
   Message msg;
   auto totalSize = msg.PackMsg(myMsg);
   ASSERT_GT(totalSize, 0);

   //Test
   msg.GetMsg().set_msgkey(0);
   int msgSize = msg.Decode();

   //Expectations
   EXPECT_GT(msgSize, 0);
   EXPECT_EQ(msg.GetMsg().msgkey(), key);
}
// Tests Decode
TEST(MessageTest, DecodeWithParam) {
   //Setup
   Header myMsg;
   myMsg.set_msgkey(5);
   Message msg;
   auto totalSize = msg.PackMsg(myMsg);
   ASSERT_GT(totalSize, 0);

   //Test
   Header decodedMsg;
   int msgSize = msg.Decode(&decodedMsg);

   //Expectations
   EXPECT_GT(msgSize, 0);
   EXPECT_EQ(decodedMsg.msgkey(), myMsg.msgkey());
}
// Tests Decode of empty message
TEST(MessageTest, DecodeEmptyMessage) {
   //Setup
   Message msg;
   auto size = msg.CreateEmptyMsg();
   ASSERT_EQ((int)size, HDR_SIZE);

   //Test
   Header decodedMsg;
   int msgSize = msg.Decode(&decodedMsg);

   //Expectations
   EXPECT_EQ(msgSize, 0);
}
