#include <gtest/gtest.h>

#include "MessageUtils.h"

using namespace Matrix::MsgService;
using namespace Matrix::MsgService::CommonMessages;

//Test Fixture - use TEST_F when using a test fixture
class MessageUtilsTest : public testing::Test {
protected:
   virtual void SetUp() {
   }
   virtual void TearDown() {
   }
};

// Tests GetMsgTypeID
TEST(MessageUtilsTest, GetMsgTypeID) {
   //Setup
   Header hdr;
   hdr.set_msgtypeid(CommonMessages::MsgType::SUBSCRIBE);

   size_t size = hdr.ByteSizeLong();
   void* buffer = malloc(size);
   hdr.SerializeToArray(buffer, size);

   //Test
   auto msgType = MessageUtils::GetMsgTypeID(buffer, size);

   //Expectations
   EXPECT_EQ(msgType, MsgType::SUBSCRIBE);
   free(buffer);
}

// Tests GetMsgTypeID
TEST(MessageUtilsTest, ParseHeader) {
   //Setup
   Header msgRequest;
   msgRequest.set_msgtypeid(CommonMessages::MsgType::SUBSCRIBE);
   msgRequest.set_msgkey(5);
//   msgRequest.set_doorid(2);

   size_t size = msgRequest.ByteSizeLong();
   void* buffer = malloc(size);
   msgRequest.SerializeToArray(buffer, size);

   //Test
   Header hdr;
   auto success = MessageUtils::ParseHeader(hdr, buffer, size);

   //Expectations
   EXPECT_TRUE(success);
   EXPECT_EQ(hdr.msgtypeid(), MsgType::SUBSCRIBE);
   EXPECT_EQ(hdr.msgkey(), 5);
   free(buffer);
}

