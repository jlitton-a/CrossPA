#include <gtest/gtest.h>
#include <gmock/gmock.h>

#include "SafeQueue.h"

#include "ContextHandler.h"
#include "CommHandlerSignals.h"
#include "ClientComm.h"
#include "Message.h"
#include "MessageUtils.h"
#include "SubscriberMessageListsMock.h"

//disable Inherits Via Dominance warning
#ifdef _WIN32
#pragma warning( push )
#pragma warning( disable: 4250)
#endif

using namespace Matrix::Common;
using namespace Matrix::MsgService::CommunicationUtils;
using namespace testing;

//Class used to bypass SendMsg processing so other functions can be more easily tested
class ClientCommDerived : public ClientComm
{
public:
   int _lastMsgKey = 0;
   std::vector<CommonMessages::Header> _messagesSent;
   ClientCommDerived(std::shared_ptr<ContextHandler> pContextHandler, std::shared_ptr<ISubscriberMessageLists> pSubscriberMsgListsMock
         , bool trackSentMessages, std::string ipAddress = "localhost", int retryTime = 0)
      : ClientComm(pContextHandler, ipAddress, "100", 1, 3, retryTime, "MyName", nullptr, trackSentMessages, pSubscriberMsgListsMock)
   {
   }
   ~ClientCommDerived() {}
   //override to not actually try to disconnect
   virtual void Disconnect(DisconnectReason) override {}
   //override to not actually try to send it on the wire
   virtual bool SendMsg(CommonMessages::Header& msg) override
   {
      _lastMsgKey = msg.msgkey();
      _messagesSent.push_back(msg);
      return true;
   }
   //override to make it public so we can test it
   void HandleMessageReceived(std::unique_ptr<CommonMessages::Header> pMsg) override
   {
      ClientComm::HandleMessageReceived(std::move(pMsg));
   }
};

//Test Fixture - use TEST_F when using a test fixture
class ClientCommTest : public testing::Test {
protected:
   std::shared_ptr<ContextHandler> _pContextHandler = nullptr;
   std::shared_ptr<NiceMock<SubscriberMessageListsMock>> _pSubscriberMessageLists = nullptr;

   virtual void SetUp()
   {
      _pSubscriberMessageLists = std::make_shared<NiceMock<SubscriberMessageListsMock>>();
#ifdef USING_SSL
      boost::asio::ssl::context sslContext(boost::asio::ssl::context::sslv23);
      _pContextHandler = std::make_shared<ContextHandler>(sslContext);
#else
      _pContextHandler = std::make_shared<ContextHandler>();
#endif
      _pContextHandler->StartThread();
   }
   virtual void TearDown() 
   {
      Shutdown();
   }
public:
   void Shutdown()
   {
      if (_pContextHandler)
      {
         _pContextHandler->ShutDown();
         _pContextHandler->WaitForShutdown(10, 10);
         _pContextHandler = nullptr;
      }
   }

   std::shared_ptr<ClientCommDerived> CreateClientComm(bool trackSentMessages = false, std::string ipAddress = "localhost", int retryTime = 0)
   {
      return std::make_shared<ClientCommDerived>(_pContextHandler, _pSubscriberMessageLists, trackSentMessages, ipAddress, retryTime);
   }
};
class CallbackClass
{
public:
   bool _statusChangeCalled = false;
   bool _connectChangeCalled = false;
   bool _isConnected = false;
   struct Info
   {
      SocketState oldState;
      SocketState newState;
      DisconnectReason reason;
   };
   SafeQueue<Info> _statusChangeList;
   CallbackClass()
   {
   }

   void HandleStatusChange(SocketState oldState, SocketState newState, DisconnectReason reason)
   {
      Info info;
      info.oldState = oldState;
      info.newState = newState;
      info.reason = reason;
      _statusChangeCalled = true;
      _statusChangeList.Enqueue(info);
   }
   void HandleConnectChange(bool isConnected)
   {
      _connectChangeCalled = true;
      _isConnected = isConnected;
   }
};

//Tests SendAckMessage
TEST_F(ClientCommTest, SendAckMessage_CallsSendMsg) {
   //Setup
   auto pClientComm = CreateClientComm();
   Matrix::MsgService::CommonMessages::Header msgToAck;
   msgToAck.set_origclienttype(1);
   msgToAck.set_origclientid(2);
   msgToAck.set_msgkey(3);

   //Test
   pClientComm->SendAckMessage(msgToAck);

   //Expectations
   EXPECT_EQ((int)pClientComm->_messagesSent.size(), 1);
   EXPECT_EQ(pClientComm->_messagesSent[0].msgtypeid(), Matrix::MsgService::CommonMessages::MsgType::ACK);
   EXPECT_EQ(pClientComm->_messagesSent[0].destclienttype(), 1);
   EXPECT_EQ(pClientComm->_messagesSent[0].destclientid(), 2);
   EXPECT_EQ(pClientComm->_messagesSent[0].msgkey(), 3);

   //Cleanup
   pClientComm = nullptr;
}
//Tests SendNackMessage
TEST_F(ClientCommTest, SendNackMessage_CallsSendMsg) {
   //Setup
   auto pClientComm = CreateClientComm();
   Matrix::MsgService::CommonMessages::Header msgToNack;
   msgToNack.set_origclienttype(1);
   msgToNack.set_origclientid(2);
   msgToNack.set_msgkey(3);

   //Test
   pClientComm->SendNackMessage(msgToNack, 1, "xyz");

   //Expectations
   EXPECT_EQ((int)pClientComm->_messagesSent.size(), 1);
   EXPECT_EQ(pClientComm->_messagesSent[0].msgtypeid(), Matrix::MsgService::CommonMessages::MsgType::NACK);
   EXPECT_EQ(pClientComm->_messagesSent[0].destclienttype(), 1);
   EXPECT_EQ(pClientComm->_messagesSent[0].destclientid(), 2);
   EXPECT_EQ(pClientComm->_messagesSent[0].msgkey(), 3);

   //Cleanup
   pClientComm = nullptr;
}

//Tests SendCommonMsg
TEST_F(ClientCommTest, SendCommonMsg_CallsSendMsg) {
   //Setup
   auto pClientComm = CreateClientComm();
   Matrix::MsgService::CommonMessages::Logon msgToSend;

   //Test
   pClientComm->SendCommonMsg(Matrix::MsgService::CommonMessages::MsgType::LOGON, &msgToSend);

   //Expectations
   EXPECT_EQ( (int)pClientComm->_messagesSent.size(), 1);
   EXPECT_EQ(pClientComm->_messagesSent[0].msgtypeid(), Matrix::MsgService::CommonMessages::MsgType::LOGON);

   //Cleanup
   pClientComm = nullptr;
}
//Tests SendCommonMsg
TEST_F(ClientCommTest, SendCommonMsg_NoSubMsg_CallsSendMsg) {
   //Setup
   auto pClientComm = CreateClientComm();

   //Test
   pClientComm->SendCommonMsg(Matrix::MsgService::CommonMessages::MsgType::LOGON, nullptr);

   //Expectations
   EXPECT_EQ((int)pClientComm->_messagesSent.size(), 1);
   EXPECT_EQ(pClientComm->_messagesSent[0].msgtypeid(), Matrix::MsgService::CommonMessages::MsgType::LOGON);

   //Cleanup
   pClientComm = nullptr;
}
//Tests SendCommonMsg
TEST_F(ClientCommTest, SendCommonMsg_WithClientType_CallsAddSentMessage) {
   //Setup
   auto pClientComm = CreateClientComm(true);
   Matrix::MsgService::CommonMessages::Logon msgToSend;

   //Mock Expectations
   EXPECT_CALL(*_pSubscriberMessageLists, AddSentMessage(_)).Times(1);
   EXPECT_CALL(*_pSubscriberMessageLists, GetNeedToAckList(1, 5)).Times(1);

   //Test
   pClientComm->SendCommonMsg(Matrix::MsgService::CommonMessages::MsgType::CUSTOM, &msgToSend, 0, 1, 5);

   //Expectations
    
   //Cleanup
   pClientComm = nullptr;
}
//Tests SendCommonMsg
TEST_F(ClientCommTest, SendCommonMsg_WithoutClientType_DoesNotCallsAddSentMessage) {
   //Setup
   auto pClientComm = CreateClientComm(true);
   Matrix::MsgService::CommonMessages::Logon msgToSend;

   //Mock Expectations
   EXPECT_CALL(*_pSubscriberMessageLists, AddSentMessage(_)).Times(0);
   EXPECT_CALL(*_pSubscriberMessageLists, GetNeedToAckList(_, _)).Times(0);

   //Test
   pClientComm->SendCommonMsg(Matrix::MsgService::CommonMessages::MsgType::CUSTOM, &msgToSend, 0, 0, 0);

   //Expectations

   //Cleanup
   pClientComm = nullptr;
}
//Tests SendCommonMsg
TEST_F(ClientCommTest, SendCommonMsg_WithClientType_CallsRemoveFromNeedsToAckList) {
   //Setup
   auto pClientComm = CreateClientComm(true);
   Matrix::MsgService::CommonMessages::Logon msgToSend;

   //Mock Expectations
   EXPECT_CALL(*_pSubscriberMessageLists, RemoveFromNeedToAckList(1, 5, _)).Times(1);

   //Test
   pClientComm->SendCommonMsg(Matrix::MsgService::CommonMessages::MsgType::CUSTOM, &msgToSend, 0, 1, 5);

   //Expectations

   //Cleanup
   pClientComm = nullptr;
}
//Tests SendCommonMsg
TEST_F(ClientCommTest, SendCommonMsg_WithoutClientType_DoesNotCallRemoveFromNeedsToAckList) {
   //Setup
   auto pClientComm = CreateClientComm(true);
   Matrix::MsgService::CommonMessages::Logon msgToSend;

   //Mock Expectations
   EXPECT_CALL(*_pSubscriberMessageLists, RemoveFromNeedToAckList(_, _, _)).Times(0);

   //Test
   pClientComm->SendCommonMsg(Matrix::MsgService::CommonMessages::MsgType::CUSTOM, &msgToSend, 0, 0, 0);

   //Expectations

   //Cleanup
   pClientComm = nullptr;
}
///Tests Subscribe
TEST_F(ClientCommTest, Subscribe_AddsToList) {
   //Setup
   auto pUnderTest = CreateClientComm();

   //Test
   bool changed = pUnderTest->Subscribe(Matrix::MsgService::CommonMessages::ClientTypes::AssaAbloy);

   //Expectations
   EXPECT_TRUE(changed);

   //Cleanup
   pUnderTest = nullptr;
}
TEST_F(ClientCommTest, Subscribe_Second_DoesNotAdd) {
   //Setup
   auto pUnderTest = CreateClientComm();

   pUnderTest->Subscribe(Matrix::MsgService::CommonMessages::ClientTypes::AssaAbloy);

   //Test
   bool changed = pUnderTest->Subscribe(Matrix::MsgService::CommonMessages::ClientTypes::AssaAbloy);

   //Expectations
   EXPECT_FALSE(changed);

   //Cleanup
   pUnderTest = nullptr;
}
TEST_F(ClientCommTest, Subscribe_Unsubscribe_NotInList) {
   //Setup
   auto pUnderTest = CreateClientComm();

   pUnderTest->Subscribe(Matrix::MsgService::CommonMessages::ClientTypes::AssaAbloy, 0, false);

   //Test
   bool changed = pUnderTest->Subscribe(Matrix::MsgService::CommonMessages::ClientTypes::AssaAbloy, 0, false);

   //Expectations
   EXPECT_FALSE(changed);

   //Cleanup
   pUnderTest = nullptr;
}
TEST_F(ClientCommTest, Subscribe_Unsubscribe_InList_Removes) {
   //Setup
   auto pUnderTest = CreateClientComm();
   pUnderTest->Subscribe(Matrix::MsgService::CommonMessages::ClientTypes::AssaAbloy, 0, true);

   //Mock Expectations

   //Test
   bool changed = pUnderTest->Subscribe(Matrix::MsgService::CommonMessages::ClientTypes::AssaAbloy, 0, false);

   //Expectations
   EXPECT_TRUE(changed);

   //Cleanup
   pUnderTest = nullptr;
}
TEST_F(ClientCommTest, HandleMessageReceived_Logoff_CallsSetClientOnLine) {
   //Setup
   auto pUnderTest = CreateClientComm(true);
   auto pMsg = std::unique_ptr<CommonMessages::Header>(new CommonMessages::Header());
   pMsg->set_msgtypeid(CommonMessages::MsgType::LOGOFF);
   pMsg->set_origclienttype(1);

   //Mock Expectations
   EXPECT_CALL(*_pSubscriberMessageLists, SetClientOnLine(1, 0, false)).Times(1);

   //Test
   pUnderTest->HandleMessageReceived(std::move(pMsg));

   //Expectations

   //Cleanup
   pUnderTest = nullptr;
}
TEST_F(ClientCommTest, HandleMessageReceived_AnyMsg_CallsSetClientOnLine) {
   //Setup
   auto pUnderTest = CreateClientComm(true);
   auto pMsg = std::unique_ptr<CommonMessages::Header>(new CommonMessages::Header());
   pMsg->set_msgtypeid(CommonMessages::MsgType::LOGON);
   pMsg->set_origclienttype(1);

   //Mock Expectations
   EXPECT_CALL(*_pSubscriberMessageLists, SetClientOnLine(1, 0, true)).Times(1);

   //Test
   pUnderTest->HandleMessageReceived(std::move(pMsg));

   //Expectations

   //Cleanup
   pUnderTest = nullptr;
}
TEST_F(ClientCommTest, HandleMessageReceived_AnyMsg_CallsRemoveSentMessages) {
   //Setup
   auto pUnderTest = CreateClientComm(true);
   auto pMsg = std::unique_ptr<CommonMessages::Header>(new CommonMessages::Header());
   pMsg->set_msgtypeid(CommonMessages::MsgType::LOGON);
   pMsg->set_origclienttype(1);

   //Mock Expectations
   EXPECT_CALL(*_pSubscriberMessageLists, RemoveSentMessages(1, 0, _)).Times(1);

   //Test
   pUnderTest->HandleMessageReceived(std::move(pMsg));

   //Expectations

   //Cleanup
   pUnderTest = nullptr;
}
TEST_F(ClientCommTest, HandleMessageReceived_Ack_CallsRemoveSentMessage) {
   //Setup
   auto pUnderTest = CreateClientComm(true);
   auto pMsg = std::unique_ptr<CommonMessages::Header>(new CommonMessages::Header());
   pMsg->set_msgtypeid(CommonMessages::MsgType::ACK);
   pMsg->set_origclienttype(1);
   pMsg->set_msgkey(5);

   //Mock Expectations
   EXPECT_CALL(*_pSubscriberMessageLists, RemoveSentMessage(1, 0, 5)).Times(1);

   //Test
   pUnderTest->HandleMessageReceived(std::move(pMsg));

   //Expectations

   //Cleanup
   pUnderTest = nullptr;
}

TEST_F(ClientCommTest, SendCommonMsgAndWait_NoReceipt_ReturnsNull) {
   //Setup
   auto pClientComm = CreateClientComm();

   //Test
    auto pRxMsg = pClientComm->SendCommonMsgAndWait(Matrix::MsgService::CommonMessages::MsgType::LOGON, nullptr, 0, 1, 1, 1000);

   //Expectations
   EXPECT_EQ(pRxMsg, nullptr);

   //Cleanup
   pClientComm = nullptr;
}
TEST_F(ClientCommTest, SendCommonMsgAndWait_NotThisAckNoAckKeys_ReturnsNull) {
   //Setup
   auto pClientComm = CreateClientComm();

   Matrix::Common::CountdownTimer rxCaller;
   rxCaller.StartTimer(500, [&pClientComm] {
      auto pMsg = std::unique_ptr<CommonMessages::Header>(new CommonMessages::Header());
      pMsg->set_msgtypeid(CommonMessages::MsgType::ACK);
      pMsg->set_origclienttype(1);
      pMsg->set_msgkey(99);
      pClientComm->HandleMessageReceived(std::move(pMsg)); }, true);

   //Test
   auto pRxMsg = pClientComm->SendCommonMsgAndWait(Matrix::MsgService::CommonMessages::MsgType::CUSTOM, nullptr, 0, 1, 1, 1000);

   //Expectations
   EXPECT_EQ(pRxMsg, nullptr);

   //Cleanup
   pClientComm = nullptr;
}
TEST_F(ClientCommTest, SendCommonMsgAndWait_AckForSentMsg_ReturnsMessage) {
   //Setup
   auto pClientComm = CreateClientComm();

   Matrix::Common::CountdownTimer rxCaller;
   rxCaller.StartTimer(500, [&pClientComm] {
      auto pMsg = std::unique_ptr<CommonMessages::Header>(new CommonMessages::Header());
      pMsg->set_msgtypeid(CommonMessages::MsgType::ACK);
      pMsg->set_origclienttype(1);
      pMsg->set_msgkey(pClientComm->_lastMsgKey);
      pClientComm->HandleMessageReceived(std::move(pMsg)); }, true);

   //Test
   auto pRxMsg = pClientComm->SendCommonMsgAndWait(Matrix::MsgService::CommonMessages::MsgType::CUSTOM, nullptr, 0, 1, 1, 1000);

   //Expectations
   EXPECT_NE(pRxMsg, nullptr);

   //Cleanup
   pClientComm = nullptr;
}
TEST_F(ClientCommTest, SendCommonMsgAndWait_InAckKeys_ReturnsMessage) {
   //Setup
   auto pClientComm = CreateClientComm();

   Matrix::Common::CountdownTimer rxCaller;
   rxCaller.StartTimer(500, [&pClientComm] {
      auto pMsg = std::unique_ptr<CommonMessages::Header>(new CommonMessages::Header());
      pMsg->set_msgtypeid(CommonMessages::MsgType::CUSTOM);
      pMsg->set_origclienttype(1);
      pMsg->set_msgkey(pClientComm->_lastMsgKey + 1);
      pMsg->add_ackkeys(pClientComm->_lastMsgKey);
      pClientComm->HandleMessageReceived(std::move(pMsg)); }, true);

   //Test
   auto pRxMsg = pClientComm->SendCommonMsgAndWait(Matrix::MsgService::CommonMessages::MsgType::CUSTOM, nullptr, 0, 1, 1, 1500);

   //Expectations
   EXPECT_NE(pRxMsg, nullptr);

   //Cleanup
   pClientComm = nullptr;
}
#ifdef _WIN32
#pragma warning( pop )
#endif
