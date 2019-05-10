#include <gtest/gtest.h>
#include <gmock/gmock.h>

#include "SubscriptionHandler.h"
#include "ClientMsgHandlerMock.h"
#include "SubscriptionHandler.h"
#include "Message.h"

namespace CommonMessages = Matrix::MsgService::CommonMessages;
using namespace Matrix::MsgService::MessageThreads;
using namespace testing;

//Test Fixture - use TEST_F when using a test fixture
class SubscriptionHandlerTest : public testing::Test {
protected:
   std::shared_ptr<SubscriptionHandler> pUnderTest;

   virtual void SetUp() {
      pUnderTest = std::make_shared<SubscriptionHandler>();
   }
   virtual void TearDown() {
      pUnderTest = nullptr;
   }

   std::shared_ptr<NiceMock<ClientMsgHandlerMock>> CreateMockClientMsgHandler(int clientType = 1, int clientID = 100)
   {
      auto pClient = std::make_shared<NiceMock<ClientMsgHandlerMock>>();
      ON_CALL(*pClient, SendMsg(_)).WillByDefault(Return(true));
      ON_CALL(*pClient, GetClientType()).WillByDefault(Return(clientType));
      ON_CALL(*pClient, GetClientID()).WillByDefault(Return(clientID));
      return pClient;
   }
};

/////////////////////////////////////////////
// TESTS
/////////////////////////////////////////////

// Tests GetMsgTypeID
TEST_F(SubscriptionHandlerTest, AddSubscription_Adds) {
   //Setup
   CommonMessages::Subscribe subscribeMsg;
   auto pClient = CreateMockClientMsgHandler();

   //Test
   pUnderTest->AddSubscription(pClient, subscribeMsg);

   //Expectations
   EXPECT_EQ(2, pClient.use_count());
   pClient = nullptr;
}
TEST_F(SubscriptionHandlerTest, AddSubscription_DoesNotAddTwice) {
   //Setup
   CommonMessages::Subscribe subscribeMsg;
   auto pClient = CreateMockClientMsgHandler();
   pUnderTest->AddSubscription(pClient, subscribeMsg);

   //Test
   pUnderTest->AddSubscription(pClient, subscribeMsg);

   //Expectations
   EXPECT_EQ(2, pClient.use_count());
   pClient = nullptr;
}
TEST_F(SubscriptionHandlerTest, AddSubscription_SecondMsgAdded) {
   //Setup
   CommonMessages::Subscribe subscribeMsg;
   auto pClient = CreateMockClientMsgHandler();
   pUnderTest->AddSubscription(pClient, subscribeMsg);
   subscribeMsg.set_clienttype(1);
   pUnderTest->AddSubscription(pClient, subscribeMsg);

   //Test
   pUnderTest->AddSubscription(pClient, subscribeMsg);

   //Expectations
   EXPECT_EQ(3, pClient.use_count());
   pClient = nullptr;
}

TEST_F(SubscriptionHandlerTest, RemoveSubscription_Removes) {
   //Setup
   CommonMessages::Subscribe subscribeMsg;
   auto pClient = CreateMockClientMsgHandler();
   pUnderTest->AddSubscription(pClient, subscribeMsg);
   auto pClient2 = CreateMockClientMsgHandler();
   pUnderTest->AddSubscription(pClient2, subscribeMsg);

   //Test
   pUnderTest->RemoveSubscription(pClient.get(), subscribeMsg);

   //Expectations
   EXPECT_EQ(1, pClient.use_count());
   EXPECT_EQ(2, pClient2.use_count());
   pClient = nullptr;
}

TEST_F(SubscriptionHandlerTest, RemoveSubscription_NotThere) {
   //Setup
   CommonMessages::Subscribe subscribeMsg;
   auto pClient = CreateMockClientMsgHandler();
   auto pClient2 = CreateMockClientMsgHandler();
   pUnderTest->AddSubscription(pClient2, subscribeMsg);

   //Test
   pUnderTest->RemoveSubscription(pClient.get(), subscribeMsg);

   //Expectations
   EXPECT_EQ(1, pClient.use_count());
   EXPECT_EQ(2, pClient2.use_count());
   pClient = nullptr;
}

TEST_F(SubscriptionHandlerTest, RemoveSubscriptionFor_Removes) {
   //Setup
   CommonMessages::Subscribe subscribeMsg;
   auto pClient = CreateMockClientMsgHandler();
   pUnderTest->AddSubscription(pClient, subscribeMsg);
   subscribeMsg.set_clienttype(1);
   pUnderTest->AddSubscription(pClient, subscribeMsg);
   EXPECT_EQ(3, pClient.use_count());
   auto pClient2 = CreateMockClientMsgHandler();
   pUnderTest->AddSubscription(pClient2, subscribeMsg);

   //Test
   pUnderTest->RemoveSubscriptionsFor(pClient.get());

   //Expectations
   EXPECT_EQ(1, pClient.use_count());
   EXPECT_EQ(2, pClient2.use_count());
   pClient = nullptr;
}
TEST_F(SubscriptionHandlerTest, RemoveSubscriptionFor_NotThere) {
   //Setup
   CommonMessages::Subscribe subscribeMsg;
   auto pClient = CreateMockClientMsgHandler();
   auto pClient2 = CreateMockClientMsgHandler();
   pUnderTest->AddSubscription(pClient2, subscribeMsg);

   //Test
   pUnderTest->RemoveSubscriptionsFor(pClient.get());

   //Expectations
   EXPECT_EQ(1, pClient.use_count());
   EXPECT_EQ(2, pClient2.use_count());
   pClient = nullptr;
}
TEST_F(SubscriptionHandlerTest, ClearAll_Clears) {
   //Setup
   CommonMessages::Subscribe subscribeMsg;
   auto pClient = CreateMockClientMsgHandler();
   pUnderTest->AddSubscription(pClient, subscribeMsg);
   EXPECT_EQ(2, pClient.use_count());
   auto pClient2 = CreateMockClientMsgHandler();
   pUnderTest->AddSubscription(pClient2, subscribeMsg);
   EXPECT_EQ(2, pClient2.use_count());

   //Test
   pUnderTest->ClearAll();

   //Expectations
   EXPECT_EQ(1, pClient.use_count());
   EXPECT_EQ(1, pClient2.use_count());
   pClient = nullptr;
}
TEST_F(SubscriptionHandlerTest, SendToSubscribers_NoClients) {
   //Setup
   CommonMessages::Header msg;
   msg.set_msgtypeid(CommonMessages::MsgType::SUBSCRIBE);
   auto pClient = CreateMockClientMsgHandler();

   //Test
   pUnderTest->SendToSubscribers(pClient.get(), msg);

   //Expectations
   pClient = nullptr;
}
TEST_F(SubscriptionHandlerTest, SendToSubscribers_WrongClient_DoesNotSend) {
   //Setup
   CommonMessages::Subscribe subscribeMsg;
   auto pClient = CreateMockClientMsgHandler();
   int clientType = 1;
   auto topic = CommonMessages::MsgType::LOGON;
   subscribeMsg.set_clienttype(clientType);
   subscribeMsg.set_topic(topic);
   pUnderTest->AddSubscription(pClient, subscribeMsg);

   CommonMessages::Header msg;
   msg.set_msgtypeid(topic);
   auto pSender = CreateMockClientMsgHandler(clientType + 1);

   //Mock Expectations
   EXPECT_CALL(*pSender, SendMsg(_)).Times(0);

   //Test
   pUnderTest->SendToSubscribers(pSender.get(), msg);

   pClient = nullptr;
   pSender = nullptr;
}
TEST_F(SubscriptionHandlerTest, SendToSubscribers_WrongTopic_DoesNotSend) {
   //Setup
   CommonMessages::Subscribe subscribeMsg;
   int clientType = 1;
   auto pClient = CreateMockClientMsgHandler(clientType);
   auto topic = 2;
   subscribeMsg.set_clienttype(clientType);
   subscribeMsg.set_topic(topic);
   pUnderTest->AddSubscription(pClient, subscribeMsg);

   CommonMessages::Header msg;
   msg.set_msgtypeid(CommonMessages::MsgType::LOGON);
   msg.set_topic(topic + 1);
   auto pSender = CreateMockClientMsgHandler(clientType);

   //Mock Expectations
   EXPECT_CALL(*pSender, GetClientType()).Times(1);
   EXPECT_CALL(*pClient, SendMsg(_)).Times(0);

   //Test
   pUnderTest->SendToSubscribers(pSender.get(), msg);

   //Cleanup
   pUnderTest = nullptr;
   pClient = nullptr;
   pSender = nullptr;
}
TEST_F(SubscriptionHandlerTest, SendToSubscribers_WrongClientID_DoesNotSend) {
   //Setup
   CommonMessages::Subscribe subscribeMsg;
   int clientType = 1;
   int clientID = 100;
   auto pClient = CreateMockClientMsgHandler(clientType, clientID);
   auto topic = CommonMessages::MsgType::LOGON;
   subscribeMsg.set_clienttype(clientType);
   subscribeMsg.set_topic(topic);
   subscribeMsg.set_clientid(clientID + 1);
   pUnderTest->AddSubscription(pClient, subscribeMsg);

   CommonMessages::Header msg;
   msg.set_msgtypeid(topic);
   auto pSender = CreateMockClientMsgHandler(clientType);

   //Mock Expectations
   EXPECT_CALL(*pSender, GetClientType()).Times(1);
   EXPECT_CALL(*pSender, SendMsg(_)).Times(0);

   //Test
   pUnderTest->SendToSubscribers(pSender.get(), msg);

   //Expectations
   pClient = nullptr;
   pSender = nullptr;
}
TEST_F(SubscriptionHandlerTest, SendToSubscribers_DoesNotSendToSelf) {
   //Setup
   CommonMessages::Subscribe subscribeMsg;
   int clientType = 1;
   int clientID = 100;
   auto topic = CommonMessages::MsgType::LOGON;
   subscribeMsg.set_clienttype(clientType);
   subscribeMsg.set_topic(topic);
   subscribeMsg.set_clientid(clientID);

   CommonMessages::Header msg;
   msg.set_msgtypeid(topic);
   auto pSender = CreateMockClientMsgHandler(clientType, clientID);

   //Mock Expectations
   EXPECT_CALL(*pSender, GetClientType()).Times(1);
   EXPECT_CALL(*pSender, SendMsg(_)).Times(0);

   //Test
   pUnderTest->SendToSubscribers(pSender.get(), msg);

   //Cleanup
   pUnderTest = nullptr;
   pSender = nullptr;
}
TEST_F(SubscriptionHandlerTest, SendToSubscribers_Sends) {
   //Setup
   CommonMessages::Subscribe subscribeMsg;
   int clientType = 1;
   auto pClient = CreateMockClientMsgHandler(clientType);
   auto topic = 2;
   subscribeMsg.set_clienttype(clientType);
   subscribeMsg.set_topic(topic);
   pUnderTest->AddSubscription(pClient, subscribeMsg);

   CommonMessages::Header msg;
   msg.set_msgtypeid(CommonMessages::MsgType::LOGON);
   msg.set_topic(topic);
   auto pSender = CreateMockClientMsgHandler(clientType);

   //Mock Expectations
   EXPECT_CALL(*pSender, GetClientType()).Times(1);
   EXPECT_CALL(*pClient, SendMsg(_)).Times(1);

   //Test
   pUnderTest->SendToSubscribers(pSender.get(), msg);

   //Cleanup
   pUnderTest = nullptr;
   pClient = nullptr;
   pSender = nullptr;
}
TEST_F(SubscriptionHandlerTest, GetSubscribers_ReturnsList) {
   //Setup
   CommonMessages::Subscribe subscribeMsg;
   int clientType = 1;
   auto pClient = CreateMockClientMsgHandler(clientType + 1);
   auto topic = 2;
   subscribeMsg.set_clienttype(clientType);
   subscribeMsg.set_topic(topic);
   pUnderTest->AddSubscription(pClient, subscribeMsg);

   //Mock Expectations

   //Test
   auto list = pUnderTest->GetSubscribersTo(clientType, 0);

   //Expectations
   EXPECT_EQ(1, (int)list.size());

   //Cleanup
   pUnderTest = nullptr;
   pClient = nullptr;
}
