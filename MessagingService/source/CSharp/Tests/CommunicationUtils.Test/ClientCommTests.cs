using System;
using System.Collections.Generic;
using Google.Protobuf;
using Matrix.MsgService.CommonMessages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

using CommonMessages = Matrix.MsgService.CommonMessages;

namespace Matrix.MsgService.CommunicationUtils.Test
{
   [TestClass]
   public class ClientCommTest
   {
      #region Mocks
      ISubscriberMessageLists _subscriberMsgListsMock = null;
      MsgService.CommunicationUtils.IConnectionHandler _connectionHandlerMock = null;
      Utilities.IThreadingTimer _serviceTimeoutTimerMock = null;
      Utilities.IThreadingTimer _resendMsgTimerMock = null;
      IMsgStore _msgStoreMock = null;

      private void CreateMocks()
      {
         _subscriberMsgListsMock = Substitute.For<ISubscriberMessageLists>();
         _connectionHandlerMock = Substitute.For<MsgService.CommunicationUtils.IConnectionHandler>();
         _serviceTimeoutTimerMock = Substitute.For<Utilities.IThreadingTimer>();
         _resendMsgTimerMock = Substitute.For<Utilities.IThreadingTimer>();
         _msgStoreMock = Substitute.For<IMsgStore>();
      }
      private ClientComm CreateTestItem(Logon logonMsg, 
            uint reconnectRetryTimeMS = 0,
            uint heartbeatTimeMS = 2000,
            uint serverTimeOutTimeMS = 4000, 
            uint resendMessagesTimeMS = 0)
      {
         return new Matrix.MsgService.CommunicationUtils.ClientComm("MyName"
            , _connectionHandlerMock, _subscriberMsgListsMock, _serviceTimeoutTimerMock, _resendMsgTimerMock
            , logonMsg, _msgStoreMock, reconnectRetryTimeMS, heartbeatTimeMS, serverTimeOutTimeMS, resendMessagesTimeMS);
      }
      #endregion

      #region Constructor
      [TestMethod]
      public void Constructor()
      {
         //Setup
         string name = "MyName";

         //Test
         var underTest = new Matrix.MsgService.CommunicationUtils.ClientComm(name, "localhost", 100, null, null, 0, 0, 0);

         //Checks
         Assert.AreEqual(name, underTest.Name, "Name");
         Assert.IsFalse(underTest.IsConnected, "IsConnected");
         Assert.AreEqual(ClientComm.SocketState.Disconnected, underTest.ClientSocketState, "SocketState");
      }
      #endregion

      #region Tests for Connect
      [TestMethod]
      public void Connect_NoHandler()
      {
         //Setup
         string name = "MyName";
         IConnectionHandler connectionHandler = null;

         var underTest = new Matrix.MsgService.CommunicationUtils.ClientComm(name, connectionHandler, null, null, 0, 0, 0);

         //Test
         bool success = underTest.Connect();

         //Checks
         Assert.IsFalse(success, "success");
         Assert.IsFalse(underTest.IsConnected, "IsConnected");
      }
      [TestMethod]
      public void Connect_HandlerConnectReturnsFalse()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return false; });
         var underTest = CreateTestItem(null);

         //Test
         bool success = underTest.Connect();

         //Checks
         Assert.IsFalse(success, "success");
         Assert.IsFalse(underTest.IsConnected, "IsConnected");
      }

      [TestMethod]
      public void Connect_IsConnected()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         var underTest = CreateTestItem(null);

         //Test
         bool success = underTest.Connect();

         //Checks
         Assert.IsTrue(success, "success");
         Assert.IsTrue(underTest.IsConnected, "IsConnected");
      }
      [TestMethod]
      public void Connect_ConnectionStatusChanged()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         var underTest = CreateTestItem(null);

         List<ClientComm.ConnectionStatusChangedEventArgs> connectionChangedArgs = new List<ClientComm.ConnectionStatusChangedEventArgs>();
         underTest.ConnectionStatusChanged += (sender, e)=> connectionChangedArgs.Add(e);

         //Test
         underTest.Connect();

         //Checks
         Assert.IsTrue(connectionChangedArgs[0].ClientSocketState == ClientComm.SocketState.Connecting, "Connecting");
         Assert.IsTrue(connectionChangedArgs[1].ClientSocketState == ClientComm.SocketState.Connected, "Connected");
      }
      [TestMethod]
      public void Connect_CallsBeginReading()
      {
         //Setup
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         var underTest = CreateTestItem(null);

         //Test
         underTest.Connect();

         //Checks
         _connectionHandlerMock.Received().BeginReading();
      }
      [TestMethod]
      public void Connect_WithLogon_CallsSendMessage()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });

         Header rxMsg = null;
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(y=>rxMsg=y), out ex).Returns(x =>
         {
            x[1] = null;
            return true;
         });
         var logon = new CommonMessages.Logon();

         var underTest = CreateTestItem(logon);

         //Test
         underTest.Connect();

         //Checks
         _connectionHandlerMock.Received().SendMessage(Arg.Any<Header>(), out ex);
         Assert.IsTrue(rxMsg.MsgTypeID == MsgType.Logon);
      }
      [TestMethod]
      public void Connect_SendsHeartbeat_AfterLogon()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         int sendMsgCount = 0;
         _connectionHandlerMock.When(x => x.SendHeartbeatMessage(out ex)).Do(x => sendMsgCount++);
         Logon logonMsg = new Logon();
         logonMsg.ClientType = 10;

         var underTest = CreateTestItem(logonMsg, 0, 50, 0);

         //Test
         underTest.Connect();
         Header msg = new Header();
         msg.MsgKey = 1;
         msg.MsgTypeID = MsgType.Ack;
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(msg, null));
         System.Threading.Thread.Sleep(300);

         //Checks
         Assert.IsTrue(sendMsgCount > 0, string.Format("sendMsgCount = {0}", sendMsgCount));
      }
      [TestMethod]
      public void Connect_RetriesConnect()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         int connectCount = 0;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; connectCount++; return false; }, x => { x[0] = null; connectCount++; return true; });
         var underTest = CreateTestItem(null, 100, 0, 0);

         //Test
         underTest.Connect();
         System.Threading.Thread.Sleep(300);

         //Checks
         Assert.IsTrue(connectCount == 2, string.Format("connectCount = {0}", connectCount));
      }
      [TestMethod]
      public void Connect_StartsTimers()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });

         var underTest = CreateTestItem(null, 0, 0, 100, 1000);

         //Test
         underTest.Connect();

         //Checks
         _serviceTimeoutTimerMock.ReceivedWithAnyArgs().Change((uint)0, (uint)0);
         _resendMsgTimerMock.ReceivedWithAnyArgs().Change((uint)0, (uint)0);
      }
      [TestMethod]
      public void Connect_Failure_DoesNotStartTimers()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return false; });

         var underTest = CreateTestItem(null, 0, 0, 100, 1000);

         //Test
         underTest.Connect();

         //Checks
         _serviceTimeoutTimerMock.DidNotReceiveWithAnyArgs().Change((uint)0, (uint)0);
         _resendMsgTimerMock.DidNotReceiveWithAnyArgs().Change((uint)0, (uint)0);
      }

      #endregion

      #region _serviceTimeoutTimerMock.Tick
      [TestMethod]
      public void ServiceTimeoutTimer_Tick_DoesNotRetryConnect()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return false; }, x => { x[0] = null; return true; });
         int connectCount = 0;
         _connectionHandlerMock.When(x => x.Connect(out ex)).Do(x => connectCount++);

         var underTest = CreateTestItem(null);
         underTest.Connect();

         //Test
         _serviceTimeoutTimerMock.Tick += Raise.EventWith(new Utilities.ThreadingTimer.TickEventArgs(null));

         //Checks
         Assert.IsTrue(connectCount == 1, string.Format("connectCount = {0}", connectCount));
      }
      [TestMethod]
      public void ServiceTimeoutTimer_Tick_ServerTimesOut()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         _connectionHandlerMock.CheckConnection().Returns(true);

         var underTest = CreateTestItem(null, 0, 0, 100);
         underTest.Connect();
         _connectionHandlerMock.LastMessageRx.Returns<DateTime>(DateTime.Now.Subtract(new TimeSpan(0, 0, 0, 0, 101)));

         //Test
         _serviceTimeoutTimerMock.Tick += Raise.EventWith(new Utilities.ThreadingTimer.TickEventArgs(null));

         //Checks
         Assert.IsFalse(underTest.IsConnected);
      }
      [TestMethod]
      public void ServiceTimeoutTimer_Tick_ServerDoesNotTimeout()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         _connectionHandlerMock.CheckConnection().Returns(true);
         _connectionHandlerMock.LastMessageRx.Returns<DateTime>(DateTime.Now);

         var underTest = CreateTestItem(null, 0, 0, 100);
         underTest.Connect();

         //Test
         _serviceTimeoutTimerMock.Tick += Raise.EventWith(new Utilities.ThreadingTimer.TickEventArgs(null));

         //Checks
         Assert.IsTrue(underTest.IsConnected);
      }
      #endregion

      #region _resendTimeoutTimerMock.Tick
      [TestMethod]
      public void ResendTimeoutTimer_NoSubscribers_DoesNotCallSendMessage()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; } );

         var logon = new CommonMessages.Logon();
         var underTest = CreateTestItem(logon, 0, 0, 0, 1000);
         underTest.Connect();
         Header logonAck = new Header();
         logonAck.MsgKey = 1;
         logonAck.MsgTypeID = MsgType.Ack;
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(logonAck, null));
         _connectionHandlerMock.ClearReceivedCalls();

         //Test
         _resendMsgTimerMock.Tick += Raise.EventWith(new Utilities.ThreadingTimer.TickEventArgs(null));

         //Checks
         _connectionHandlerMock.Received(0).SendMessage(Arg.Any<Header>(), out ex);
      }
      [TestMethod]
      public void ResendTimeoutTimer_SubscriberNotOnline_DoesNotCallSendMessage()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });

         var subMsgListMock = Substitute.For<ISubscriberMessages>();
         var list = new List<ISubscriberMessages>();
         list.Add(subMsgListMock);
         _subscriberMsgListsMock.GetEnumerator().Returns(list.GetEnumerator());

         subMsgListMock.IsOnline.Returns(false);
         var msgList = new List<Header>();
         Header msg = new Header();
         msg.MsgKey = 14;
         msg.MsgTypeID = MsgType.Custom;
         msg.OrigClientType = 3;
         msg.OrigClientID = 10;
         msgList.Add(msg);
         subMsgListMock.GetMessages(Arg.Any<uint>(), Arg.Any<DateTime?>()).Returns(msgList);

         var logon = new CommonMessages.Logon();
         var underTest = CreateTestItem(logon, 0, 0, 0, 1000);
         underTest.Connect();
         Header logonAck = new Header();
         logonAck.MsgKey = 1;
         logonAck.MsgTypeID = MsgType.Ack;
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(logonAck, null));
         _connectionHandlerMock.ClearReceivedCalls();

         //Test
         _resendMsgTimerMock.Tick += Raise.EventWith(new Utilities.ThreadingTimer.TickEventArgs(null));

         //Checks
         _connectionHandlerMock.Received(0).SendMessage(Arg.Any<Header>(), out ex);
      }
      [TestMethod]
      public void ResendTimeoutTimer_SubscriberOnline_NullMsgList_DoesNotCallSendMessage()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });

         var subMsgListMock = Substitute.For<ISubscriberMessages>();
         var list = new List<ISubscriberMessages>();
         list.Add(subMsgListMock);
         _subscriberMsgListsMock.GetEnumerator().Returns(list.GetEnumerator());

         subMsgListMock.IsOnline.Returns(true);
         subMsgListMock.GetMessages(Arg.Any<uint>(), Arg.Any<DateTime?>()).Returns((List<Header>)null);

         var logon = new CommonMessages.Logon();
         var underTest = CreateTestItem(logon, 0, 0, 0, 1000);
         underTest.Connect();
         Header logonAck = new Header();
         logonAck.MsgKey = 1;
         logonAck.MsgTypeID = MsgType.Ack;
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(logonAck, null));
         _connectionHandlerMock.ClearReceivedCalls();

         //Test
         _resendMsgTimerMock.Tick += Raise.EventWith(new Utilities.ThreadingTimer.TickEventArgs(null));

         //Checks
         _connectionHandlerMock.Received(0).SendMessage(Arg.Any<Header>(), out ex);
      }
      [TestMethod]
      public void ResendTimeoutTimer_SubscribersOnLine_CallSendMessage()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });

         var subMsgListMock = Substitute.For<ISubscriberMessages>();
         var list = new List<ISubscriberMessages>();
         list.Add(subMsgListMock);
         _subscriberMsgListsMock.GetEnumerator().Returns(list.GetEnumerator());

         subMsgListMock.IsOnline.Returns(true);
         var msgList = new List<Header>();
         Header msg = new Header();
         msg.MsgKey = 14;
         msg.MsgTypeID = MsgType.Custom;
         msg.OrigClientType = 3;
         msg.OrigClientID = 10;
         msgList.Add(msg);
         subMsgListMock.GetMessages(Arg.Any<uint>(), Arg.Any<DateTime?>()).Returns(msgList);

         var logon = new CommonMessages.Logon();
         var underTest = CreateTestItem(logon, 0, 0, 0, 1000);
         underTest.Connect();
         Header logonAck = new Header();
         logonAck.MsgKey = 1;
         logonAck.MsgTypeID = MsgType.Ack;
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(logonAck, null));
         _connectionHandlerMock.ClearReceivedCalls();

         //Test
         _resendMsgTimerMock.Tick += Raise.EventWith(new Utilities.ThreadingTimer.TickEventArgs(null));

         //Checks
         _connectionHandlerMock.Received(1).SendMessage(Arg.Any<Header>(), out ex);
      }
      [TestMethod]
      public void ResendTimeoutTimer_SubscribersOnLine_SetsIsArchivedFlag()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });

         var subMsgListMock = Substitute.For<ISubscriberMessages>();
         var list = new List<ISubscriberMessages>();
         list.Add(subMsgListMock);
         _subscriberMsgListsMock.GetEnumerator().Returns(list.GetEnumerator());

         subMsgListMock.IsOnline.Returns(true);
         var msgList = new List<Header>();
         Header msg = new Header();
         msg.MsgKey = 14;
         msg.MsgTypeID = MsgType.Custom;
         msg.OrigClientType = 3;
         msg.OrigClientID = 10;
         msgList.Add(msg);
         subMsgListMock.GetMessages(Arg.Any<uint>(), Arg.Any<DateTime?>()).Returns(msgList);
         Header msgSent = null;
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(x => msgSent = x), out ex);

         var logon = new CommonMessages.Logon();
         var underTest = CreateTestItem(logon, 0, 0, 0, 1000);
         underTest.Connect();
         Header logonAck = new Header();
         logonAck.MsgKey = 1;
         logonAck.MsgTypeID = MsgType.Ack;
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(logonAck, null));

         //Test
         _resendMsgTimerMock.Tick += Raise.EventWith(new Utilities.ThreadingTimer.TickEventArgs(null));

         //Checks
         Assert.IsTrue(msgSent.IsArchived);
      }
      #endregion

      #region Tests for Disconnect
      [TestMethod]
      public void Disconnect_Manual()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });

         var underTest = CreateTestItem(null, 0, 0, 0);
         bool success = underTest.Connect();
         Assert.IsTrue(underTest.IsConnected, "Start IsConnected");

         List<ClientComm.ConnectionStatusChangedEventArgs> connectionChangedArgs = new List<ClientComm.ConnectionStatusChangedEventArgs>();
         underTest.ConnectionStatusChanged += (sender, e) => connectionChangedArgs.Add(e);

         //Test
         underTest.Disconnect();

         //Checks
         Assert.IsFalse(underTest.IsConnected, "IsConnected");
         Assert.IsNotNull(connectionChangedArgs.Find(x => x.ClientSocketState == ClientComm.SocketState.Disconnected && x.Reason == ClientComm.DisconnectReason.Manual));
      }
      [TestMethod]
      public void Disconnect_ConnectionStatusChanged()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });

         var underTest = CreateTestItem(null);

         underTest.Connect();
         List<ClientComm.ConnectionStatusChangedEventArgs> connectionChangedArgs = new List<ClientComm.ConnectionStatusChangedEventArgs>();
         underTest.ConnectionStatusChanged += (sender, e) => connectionChangedArgs.Add(e);

         //Test
         underTest.Disconnect();

         //Checks
         Assert.IsTrue(connectionChangedArgs[0].ClientSocketState == ClientComm.SocketState.Disconnecting, "Disconnecting");
         Assert.IsTrue(connectionChangedArgs[1].ClientSocketState == ClientComm.SocketState.Disconnected, "Disconnected");
      }
      [TestMethod]
      public void Disconnect_DisablesTimers()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });

         var underTest = CreateTestItem(null, 0, 0, 100, 1000);

         underTest.Connect();

         //Test
         underTest.Disconnect();

         //Checks
         _serviceTimeoutTimerMock.Received().Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
         _resendMsgTimerMock.Received().Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
      }
      #endregion

      #region AddSubscribe
      [TestMethod]
      public void AddSubscribe_CallsSendMessage_OnRxLogonAck()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         Header rxMsg = null;
         int sendMsgCount = 0;
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(y =>
         {
            if (sendMsgCount == 1)
               rxMsg = y;
            sendMsgCount++;
         }), out ex).Returns(x =>
         {
            x[1] = null;
            return true;
         });
         var logon = new CommonMessages.Logon();
         var underTest = CreateTestItem(logon, 0, 0, 0);
         underTest.Connect();

         //Test
         underTest.AddSubscribe(1, 2, 3, true);
         Header msg = new Header();
         msg.MsgKey = 1;
         msg.MsgTypeID = MsgType.Ack;
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(msg, null));

         //Checks
         //Logon and Subscribe
         _connectionHandlerMock.Received(2).SendMessage(Arg.Any<Header>(), out ex);
         Assert.IsTrue(rxMsg.MsgTypeID == MsgType.Subscribe);
      }
      [TestMethod]
      public void AddSubscribe_CallsSendMessage_WhenLoggedOn()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         Header rxMsg = null;
         int sendMsgCount = 0;
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(y =>
         {
            if (sendMsgCount == 1)
               rxMsg = y;
            sendMsgCount++;
         }), out ex).Returns(x =>
         {
            x[1] = null;
            return true;
         });
         var logon = new CommonMessages.Logon();
         var underTest = CreateTestItem(logon, 0, 0, 0);

         underTest.Connect();
         Header logonAck = new Header();
         logonAck.MsgKey = 1;
         logonAck.MsgTypeID = MsgType.Ack;
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(logonAck, null));
         _connectionHandlerMock.ClearReceivedCalls();

         //Test
         underTest.AddSubscribe(1, 2, 3, true);

         //Checks
         //Logon and Subscribe
         _connectionHandlerMock.Received().SendMessage(Arg.Any<Header>(), out ex);
         Assert.IsTrue(rxMsg.MsgTypeID == MsgType.Subscribe);
      }

      #endregion

      #region AddClientContext and RemoveClientContext
      [TestMethod]
      public void AddClientContext_ReturnsContext()
      {
         //Setup
         CreateMocks();
         var underTest = CreateTestItem(null, 0, 0, 0);

         //Test
         var result = underTest.AddClientContext();

         //Expectations
         Assert.IsNotNull(result);
      }
      [TestMethod]
      public void RemoveClientContext_NullContext_ReturnsFalse()
      {
         //Setup
         CreateMocks();
         var underTest = CreateTestItem(null, 0, 0, 0);

         //Test
         var result = underTest.RemoveClientContext(null);

         //Expectations
         Assert.IsFalse(result);
      }
      [TestMethod]
      public void RemoveClientContext_ContextNotThere_ReturnsFalse()
      {
         //Setup
         CreateMocks();
         var underTest = CreateTestItem(null, 0, 0, 0);
         IClientContext clientContext = Substitute.For<IClientContext>();
         _subscriberMsgListsMock.RemoveContext(clientContext).Returns((List<Header>)null);

         //Test
         var result = underTest.RemoveClientContext(clientContext);

         //Expectations
         Assert.IsFalse(result);
      }
      [TestMethod]
      public void RemoveClientContext_RemovesFromSubscriberList()
      {
         //Setup
         CreateMocks();
         var underTest = CreateTestItem(null, 0, 0, 0);
         IClientContext clientContext = Substitute.For<IClientContext>();
         List<Header> list = new List<Header>();

         //Test
         var result = underTest.RemoveClientContext(clientContext);

         //Expectations
         _subscriberMsgListsMock.Received().RemoveContext(clientContext);
      }
      [TestMethod]
      public void RemoveClientContext_ContextThere_ReturnsTrue()
      {
         //Setup
         CreateMocks();
         var underTest = CreateTestItem(null, 0, 0, 0);
         IClientContext clientContext = Substitute.For<IClientContext>();
         List<Header> list = new List<Header>();
         _subscriberMsgListsMock.RemoveContext(clientContext).Returns(list);

         //Test
         var result = underTest.RemoveClientContext(clientContext);

         //Expectations
         Assert.IsTrue(result);
      }
      [TestMethod]
      public void RemoveClientContext_ContextThere_SetsWaits()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         var msgList = new List<Header>();
         Header msg = new Header();
         msg.MsgTypeID = MsgType.Custom;
         msg.MsgKey = 15;
         msgList.Add(msg);
         _subscriberMsgListsMock.RemoveContext(Arg.Any<IClientContext>()).Returns(msgList);

         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         var underTest = CreateTestItem(null, 0, 0, 0);
         underTest.Connect();
         var context = underTest.AddClientContext();
         Header receivedMsg = new Header();

         //Test
         //in order to test that the waiting acks are set, must call the function after the SendMessage
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(y =>
         {
            var result = underTest.RemoveClientContext(context);
         }), out ex).Returns(x =>
         {
            x[1] = null;
            return true;
         });
         underTest.SendCommonMessageAndWait(out receivedMsg, context, MsgType.Custom, null, 0, 1, 1, 5000);

         //Checks
         Assert.IsNull(receivedMsg);
      }
      #endregion

      #region SendAckMessage
      [TestMethod]
      public void SendAckMessage_NullMsg_DoesNothing()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         Header rxMsg = null;
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(y =>
         {
            rxMsg = y;
         }), out ex).Returns(x =>
         {
            x[1] = null;
            return true;
         });

         var underTest = CreateTestItem(null, 0, 0, 0);
         underTest.Connect();
         Header msg = null;

         //Test
         underTest.SendAckMessage(msg);

         //Checks
         _connectionHandlerMock.DidNotReceive().SendMessage(Arg.Any<Header>(), out ex);
      }
      [TestMethod]
      public void SendAckMessage_CallsSendMessage()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         Header rxMsg = null;
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(y =>
         {
            rxMsg = y;
         }), out ex).Returns(x =>
         {
            x[1] = null;
            return true;
         });

         var underTest = CreateTestItem(null, 0, 0, 0);
         underTest.Connect();
         Header msg = new Header();
         msg.OrigClientType = 5;
         msg.OrigClientID = 3;
         msg.MsgKey = 2;

         //Test
         underTest.SendAckMessage(msg);

         //Checks
         _connectionHandlerMock.Received(1).SendMessage(Arg.Any<Header>(), out ex);
         Assert.IsTrue(rxMsg.MsgTypeID == MsgType.Ack);
         Assert.IsTrue(rxMsg.DestClientType == msg.OrigClientType, "Correct ClientType");
         Assert.IsTrue(rxMsg.DestClientID == msg.OrigClientID, "Correct DestClientID");
         Assert.IsTrue(rxMsg.MsgKey == msg.MsgKey, "Correct Key");
      }
      #endregion

      #region SendNackMessage
      [TestMethod]
      public void SendNackMessage_NullMsg_DoesNothing()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         Header rxMsg = null;
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(y =>
         {
            rxMsg = y;
         }), out ex).Returns(x =>
         {
            x[1] = null;
            return true;
         });

         var underTest = CreateTestItem(null, 0, 0, 0);
         underTest.Connect();
         Header msg = null;

         //Test
         underTest.SendNackMessage(msg, 0, "");

         //Checks
         _connectionHandlerMock.DidNotReceive().SendMessage(Arg.Any<Header>(), out ex);
      }
      [TestMethod]
      public void SendNackMessage_CallsSendMessage()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         Header rxMsg = null;
         NackDetails nackDetails = null;
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(y =>
         {
            rxMsg = y;
            if (rxMsg.Msg != null)
               nackDetails = NackDetails.Parser.ParseFrom(rxMsg.Msg);

         }), out ex).Returns(x =>
         {
            x[1] = null;
            return true;
         });

         var underTest = CreateTestItem(null, 0, 0, 0);
         underTest.Connect();
         Header msg = new Header();
         msg.OrigClientType = 5;
         msg.OrigClientID = 3;
         msg.MsgKey = 2;

         //Test
         underTest.SendNackMessage(msg, 5, "xyz");

         //Checks
         _connectionHandlerMock.Received(1).SendMessage(Arg.Any<Header>(), out ex);
         Assert.AreEqual(MsgType.Nack, rxMsg.MsgTypeID);
         Assert.AreEqual(msg.OrigClientType, rxMsg.DestClientType, "Incorrect ClientType");
         Assert.AreEqual(msg.OrigClientID, rxMsg.DestClientID, "Incorrect DestClientID");
         Assert.AreEqual(msg.MsgKey, rxMsg.MsgKey, "Incorrect Key");
         Assert.AreEqual(5, nackDetails.Reason, "Incorrect reason");
         Assert.AreEqual("xyz", nackDetails.Details, "Incorrect Details");
      }
      #endregion

      #region SendCommonMessage
      [TestMethod]
      public void SendCommonMessage_CallsSendMessage_WithNullMsg()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         Header rxMsg = null;
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(y =>
         {
            rxMsg = y;
         }), out ex).Returns(x =>
         {
            x[1] = null;
            return true;
         });

         var underTest = CreateTestItem(null, 0, 0, 0);
         underTest.Connect();
         var msgType = MsgType.Custom;

         //Test
         underTest.SendCommonMessage(null, MsgType.Custom, null, 3);

         //Checks
         _connectionHandlerMock.Received(1).SendMessage(Arg.Any<Header>(), out ex);
         Assert.IsTrue(rxMsg.MsgTypeID == msgType);
         Assert.IsTrue(rxMsg.Msg.IsEmpty);
      }
      [TestMethod]
      public void SendCommonMessage_CallsSendMessage_WithNonNullMsg()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         Header rxMsg = null;
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(y =>
         {
            rxMsg = y;
         }), out ex).Returns(x =>
         {
            x[1] = null;
            return true;
         });

         var underTest = CreateTestItem(null, 0, 0, 0);
         underTest.Connect();
         var msgType = MsgType.Custom;
         

         //Test
         underTest.SendCommonMessage(null, MsgType.Custom, null, 3);

         //Checks
         _connectionHandlerMock.Received(1).SendMessage(Arg.Any<Header>(), out ex);
         Assert.IsTrue(rxMsg.MsgTypeID == msgType);
         Assert.IsNotNull(rxMsg.Msg);
      }
      [TestMethod]
      public void SendCommonMessage_Tracking_CallsAddSentMessage()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         Header rxMsg = null;
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(y =>
         {
            rxMsg = y;
         }), out ex).Returns(x =>
         {
            x[1] = null;
            return true;
         });

         var underTest = CreateTestItem(null, 0, 0, 0, 5000);
         underTest.Connect();
         var msgType = MsgType.Custom;

         //Test
         underTest.SendCommonMessage(null, msgType, null, 0, 15);

         //Checks
         _subscriberMsgListsMock.ReceivedWithAnyArgs().AddSentMessage(null, null);
      }
      [TestMethod]
      public void SendCommonMessage_NotTracking_DoesNotCallAddSentMessage()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         Header rxMsg = null;
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(y =>
         {
            rxMsg = y;
         }), out ex).Returns(x =>
         {
            x[1] = null;
            return true;
         });

         var underTest = CreateTestItem(null, 0, 0, 0, 5000);
         underTest.Connect();
         var msgType = MsgType.Custom;

         //Test
         underTest.SendCommonMessage(null, msgType, null, 3);

         //Checks
         _subscriberMsgListsMock.DidNotReceiveWithAnyArgs().AddSentMessage(null, null);
      }


      [TestMethod]
      public void SendCommonMessage_Tracking_ToDestClient_AddsAcks()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         Header rxMsg = null;
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(y =>
         {
            rxMsg = y;
         }), out ex).Returns(x =>
         {
            x[1] = null;
            return true;
         });
         var keyList = new List<int>();
         keyList.Add(15);
         _subscriberMsgListsMock.GetNeedToAckList(3, 4).Returns(keyList);

         var underTest = CreateTestItem(null, 0, 0, 0, 5000);
         underTest.Connect();
         var msgType = MsgType.Custom;

         //Test
         underTest.SendCommonMessage(null, msgType, null, 2, 3, 4);

         //Checks
         Assert.AreEqual(keyList.Count, rxMsg.AckKeys.Count);
         _subscriberMsgListsMock.Received().RemoveFromNeedToAckList(3, 4, keyList);
      }
      [TestMethod]
      public void ClientCommTest_SendCommonMessage_CallsStoreNewMessage()
      {
         //setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         Header rxMsg = null;
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(y =>
         {
            rxMsg = y;
         }), out ex).Returns(x =>
         {
            x[1] = null;
            return true;
         });
         Logon logonMsg = new Logon();
         var testItem = CreateTestItem(logonMsg);

         //test
         testItem.SendCommonMessage(null, MsgType.Custom, logonMsg, 0, 1, 0, 0, true);

         //expectations
         _msgStoreMock.ReceivedWithAnyArgs().StoreNewMessage(null, Arg.Any<DateTime>());
      }
      #endregion

      #region SendCommonMessageAndWait
      [TestMethod]
      public void SendCommonMessageAndWait_NoReceipt_ReturnsNull()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(y =>
         {
            //do not receive message
         }), out ex).Returns(x =>
         {
            x[1] = null;
            return true;
         });

         var underTest = CreateTestItem(null, 0, 0, 0);
         underTest.Connect();
         Header receivedMsg = new Header();

         //Test
         IClientContext context = null;
         var msgSent = underTest.SendCommonMessageAndWait(out receivedMsg, context, MsgType.Custom, null, 1, 1, 200);

         //Checks
         Assert.IsNull(receivedMsg);
      }
      [TestMethod]
      public void SendCommonMessageAndWait_NotThisAckNoAckKeys_ReturnsNull()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         Header replyMsg = new Header();
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(y =>
         {
            //receive a message with wrong ack
            replyMsg.MsgTypeID = MsgType.Ack;
            replyMsg.MsgKey = y.MsgKey + 15;
            var disconnectEx = new Exception();
            _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(replyMsg, disconnectEx));

         }), out ex).Returns(x =>
         {
            x[1] = null;
            return true;
         });

         var underTest = CreateTestItem(null, 0, 0, 0);
         underTest.Connect();
         Header receivedMsg = new Header();

         //Test
         IClientContext context = null;
         var msgSent = underTest.SendCommonMessageAndWait(out receivedMsg, context, MsgType.Custom, null, 1, 1, -1);

         //Checks
         Assert.IsNull(receivedMsg);
      }
      [TestMethod]
      public void SendCommonMessageAndWait_NotThisAckNotThisAckKeys_ReturnsNull()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         Header replyMsg = new Header();
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(y =>
         {
            //receive a message with wrong ack
            replyMsg.MsgTypeID = MsgType.Ack;
            replyMsg.MsgKey = y.MsgKey + 15;
            replyMsg.AckKeys.Add(y.MsgKey + 10);
            var disconnectEx = new Exception();
            _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(replyMsg, disconnectEx));

         }), out ex).Returns(x =>
         {
            x[1] = null;
            return true;
         });

         var underTest = CreateTestItem(null, 0, 0, 0);
         underTest.Connect();
         Header receivedMsg = new Header();

         //Test
         IClientContext context = null;
         var msgSent = underTest.SendCommonMessageAndWait(out receivedMsg, context, MsgType.Custom, null, 1, 1, -1);

         //Checks
         Assert.IsNull(receivedMsg);
      }
      [TestMethod]
      public void SendCommonMessageAndWait_AckForSentMsg_ReturnsMessage()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         Header replyMsg = new Header();
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(y =>
         {
            replyMsg.MsgTypeID = MsgType.Ack;
            replyMsg.MsgKey = y.MsgKey;
            var disconnectEx = new Exception();
            _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(replyMsg, disconnectEx));

         }), out ex).Returns(x =>
         {
            x[1] = null;
            return true;
         });

         var underTest = CreateTestItem(null, 0, 0, 0);
         underTest.Connect();
         Header receivedMsg = new Header();

         //Test
         IClientContext context = null;
         var msgSent = underTest.SendCommonMessageAndWait(out receivedMsg, context, MsgType.Custom, null, 1, 1, -1);

         //Checks
         Assert.AreEqual(receivedMsg, replyMsg);
      }
      [TestMethod]
      public void SendCommonMessageAndWait_InAckKeys_ReturnsMessage()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         Header replyMsg = new Header();
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(y =>
         {
            replyMsg.MsgTypeID = MsgType.Custom;
            replyMsg.MsgKey = 999;
            replyMsg.AckKeys.Add(y.MsgKey);
            var disconnectEx = new Exception();
            _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(replyMsg, disconnectEx));
         }), out ex).Returns(x =>
         {
            x[1] = null;
            return true;
         });

         var underTest = CreateTestItem(null, 0, 0, 0);
         underTest.Connect();
         Header receivedMsg = new Header();

         //Test
         IClientContext context = null;
         var msgSent = underTest.SendCommonMessageAndWait(out receivedMsg, context, MsgType.Custom, null, 1, 1, -1);

         //Checks
         Assert.AreEqual(receivedMsg, replyMsg);
      }
      [TestMethod]
      public void SendCommonMessageAndWait_AckForSentMsgAndInAckKeys_ReturnsMessage()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         Header replyMsg = new Header();
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(y =>
         {
            replyMsg.MsgTypeID = MsgType.Ack;
            replyMsg.MsgKey = y.MsgKey;
            replyMsg.AckKeys.Add(y.MsgKey);
            var disconnectEx = new Exception();
            _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(replyMsg, disconnectEx));
         }), out ex).Returns(x =>
         {
            x[1] = null;
            return true;
         });

         var underTest = CreateTestItem(null, 0, 0, 0);
         underTest.Connect();
         Header receivedMsg = new Header();

         //Test
         IClientContext context = null;
         var msgSent = underTest.SendCommonMessageAndWait(out receivedMsg, context, MsgType.Custom, null, 1, 1, -1);

         //Checks
         Assert.AreEqual(receivedMsg, replyMsg);
      }
      #endregion

      #region _connectionHandler_MessageReceived

      [TestMethod]
      public void MessageReceived_DisconnectError_ServerDisconnects()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         var underTest = CreateTestItem(null, 0, 0, 0);

         underTest.Connect();
         List<ClientComm.ConnectionStatusChangedEventArgs> connectionChangedArgs = new List<ClientComm.ConnectionStatusChangedEventArgs>();
         underTest.ConnectionStatusChanged += (sender, e) => connectionChangedArgs.Add(e);

         Header msg = new Header();
         msg.MsgKey = 1;
         msg.MsgTypeID = MsgType.Ack;
         var disconnectEx = new System.IO.IOException();

         //Test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(msg, disconnectEx));

         //Checks
         Assert.IsTrue(connectionChangedArgs[1].Reason == ClientComm.DisconnectReason.ServerDisconnected);
      }
      [TestMethod]
      public void MessageReceived_DisconnectError_Disconnects()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         var underTest = CreateTestItem(null, 0, 0, 0);

         underTest.Connect();
         List<ClientComm.ConnectionStatusChangedEventArgs> connectionChangedArgs = new List<ClientComm.ConnectionStatusChangedEventArgs>();
         underTest.ConnectionStatusChanged += (sender, e) => connectionChangedArgs.Add(e);

         Header msg = new Header();
         msg.MsgKey = 1;
         msg.MsgTypeID = MsgType.Ack;
         var disconnectEx = new Exception();

         //Test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(msg, disconnectEx));

         //Checks
         Assert.IsTrue(connectionChangedArgs[1].Reason == ClientComm.DisconnectReason.Exception);
      }
      [TestMethod]
      public void MessageReceived_NotTracking_DoesNotCallAddSubscription()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         var underTest = CreateTestItem(null, 0, 0, 0, 0);

         underTest.Connect();
         List<ClientComm.ConnectionStatusChangedEventArgs> connectionChangedArgs = new List<ClientComm.ConnectionStatusChangedEventArgs>();
         underTest.ConnectionStatusChanged += (sender, e) => connectionChangedArgs.Add(e);

         Header msg = new Header();
         msg.MsgKey = 1;
         msg.MsgTypeID = MsgType.Subscribe;
         msg.OrigClientType = 1;
         msg.OrigClientID = 2;
         var subMsg = new Subscribe();
         subMsg.Topic = 3;
         msg.Msg = subMsg.ToByteString();

         //Test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(msg, null));

         //Checks
         _subscriberMsgListsMock.DidNotReceiveWithAnyArgs().AddSubscription(0, 0);
      }
      [TestMethod]
      public void MessageReceived_Logoff_CallsSetClientOnLine()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         var underTest = CreateTestItem(null, 0, 0, 0, 5000);

         underTest.Connect();

         Header msg = new Header();
         msg.MsgKey = 1;
         msg.MsgTypeID = MsgType.Logoff;
         msg.OrigClientType = 1;
         msg.OrigClientID = 2;
         var subMsg = new Subscribe();
         subMsg.Topic = 3;
         msg.Msg = subMsg.ToByteString();

         //Test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(msg, null));

         //Checks
         _subscriberMsgListsMock.Received().SetClientOnLine(msg.OrigClientType, msg.OrigClientID, false);
      }
      [TestMethod]
      public void MessageReceived_AnyMessage_CallsSetClientOnLine()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         var underTest = CreateTestItem(null, 0, 0, 0, 5000);

         underTest.Connect();

         Header msg = new Header();
         msg.MsgKey = 1;
         msg.MsgTypeID = MsgType.Custom;
         msg.OrigClientType = 1;
         msg.OrigClientID = 2;
         var subMsg = new Subscribe();
         subMsg.Topic = 3;
         msg.Msg = subMsg.ToByteString();

         //Test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(msg, null));

         //Checks
         _subscriberMsgListsMock.Received().SetClientOnLine(msg.OrigClientType, msg.OrigClientID, true);
      }

      [TestMethod]
      public void MessageReceived_LogonAck_CallsOnLogonComplete()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });

         var logon = new CommonMessages.Logon();
         var underTest = CreateTestItem(logon, 0, 0, 0, 5000);
         bool logonCompleteCalled = false;
         underTest.LogonComplete += (obj, args) => { logonCompleteCalled = true; };

         underTest.Connect();

         Header msg = new Header();
         int msgKey = 1;
         msg.MsgKey = msgKey;
         msg.MsgTypeID = MsgType.Ack;
         var subMsg = new Subscribe();
         subMsg.Topic = 3;
         msg.Msg = subMsg.ToByteString();

         //Test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(msg, null));

         //Checks
         Assert.IsTrue(logonCompleteCalled);
      }
      [TestMethod]
      public void MessageReceived_LogonAck_DoesNotCallOnLogonComplete()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });

         var logon = new CommonMessages.Logon();
         var underTest = CreateTestItem(logon, 0, 0, 0, 5000);
         bool logonCompleteCalled = false;
         underTest.LogonComplete += (obj, args) => { logonCompleteCalled = true; };

         underTest.Connect();

         Header msg = new Header();
         int msgKey = 1;
         msg.MsgKey = msgKey;
         msg.MsgTypeID = MsgType.Ack;
         var subMsg = new Subscribe();
         subMsg.Topic = 3;
         msg.Msg = subMsg.ToByteString();

         //Test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(msg, null));

         //Checks
         Assert.IsTrue(logonCompleteCalled);
      }

      [TestMethod]
      public void MessageReceived_Ack_CallsRemoveSentMessage()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         var underTest = CreateTestItem(null, 0, 0, 0, 5000);

         underTest.Connect();

         Header msg = new Header();
         int msgKey = 156;
         msg.MsgKey = msgKey;
         msg.MsgTypeID = MsgType.Ack;
         msg.OrigClientType = 1;
         msg.OrigClientID = 2;
         var subMsg = new Subscribe();
         subMsg.Topic = 3;
         msg.Msg = subMsg.ToByteString();

         //Test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(msg, null));

         //Checks
         IClientContext context = null;
         _subscriberMsgListsMock.Received().RemoveSentMessage(1, 2, msgKey, out context);
      }
      [TestMethod]
      public void MessageReceived_AckKeys_CallsRemoveSentMessages()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         var underTest = CreateTestItem(null, 0, 0, 0, 5000);

         underTest.Connect();

         Header msg = new Header();
         int msgKey = 156;
         msg.MsgKey = msgKey;
         msg.MsgTypeID = MsgType.Custom;
         msg.OrigClientType = 1;
         msg.OrigClientID = 2;
         msg.AckKeys.Add(5);

         //Test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(msg, null));

         //Checks
         IClientContext context;
         _subscriberMsgListsMock.Received().RemoveSentMessage(1, 2, 5, out context);
      }
      [TestMethod]
      public void MessageReceived_WithAckKeys_CallsRemoveMessage()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         Logon logonMsg = new Logon();
         var underTest = CreateTestItem(logonMsg);

         Header msg = new Header();
         msg.MsgKey = 1;
         msg.MsgTypeID = MsgType.Logon;
         msg.OrigClientType = 1;
         msg.OrigClientID = 10;
         int ackKey = 5;
         msg.AckKeys.Add(ackKey);

         //Test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(msg, (Exception)null));

         //Checks
         _msgStoreMock.Received().RemoveMessage(ackKey);
      }
      [TestMethod]
      public void MessageReceived_AckMsg_CallsRemoveMessage()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         Logon logonMsg = new Logon();
         var underTest = CreateTestItem(logonMsg);

         Header msg = new Header();
         msg.MsgKey = 15;
         msg.MsgTypeID = MsgType.Ack;
         msg.OrigClientType = 1;
         msg.OrigClientID = 10;

         //Test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(msg, (Exception)null));

         //Checks
         _msgStoreMock.Received().RemoveMessage(msg.MsgKey);
      }
      [TestMethod]
      public void MessageReceived_ReplyMsgKey_CallsRemoveMessage()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         Logon logonMsg = new Logon();
         var underTest = CreateTestItem(logonMsg);

         Header msg = new Header();
         msg.MsgKey = 1;
         msg.MsgTypeID = MsgType.Custom;
         msg.ReplyMsgKey = 15;
         msg.OrigClientType = 1;
         msg.OrigClientID = 10;

         //Test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(msg, (Exception)null));

         //Checks
         _msgStoreMock.Received().RemoveMessage(msg.ReplyMsgKey);
      }
      [TestMethod]
      public void MessageReceived_AckMsg_CallsClientContextAckReceived()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         Logon logonMsg = new Logon();
         var underTest = CreateTestItem(logonMsg);
         var context = underTest.AddClientContext();

         Header contextMsgSent = null;
         context.AckReceived += (x) => { contextMsgSent = x; };

         Header sentMsg = new Header();
         sentMsg.MsgKey = 16;
         sentMsg.MsgTypeID = MsgType.Custom;
         sentMsg.OrigClientType = 1;
         sentMsg.OrigClientID = 10;

         IClientContext outContext = Substitute.For<IClientContext>();
         _subscriberMsgListsMock.RemoveSentMessage(0, 1, 15, out outContext)
            .ReturnsForAnyArgs(sentMsg)
            .AndDoes(x => { x[3] = context; });

         Header ackMsg = new Header();
         ackMsg.MsgKey = sentMsg.MsgKey;
         ackMsg.MsgTypeID = MsgType.Ack;
         ackMsg.OrigClientType = 1;
         ackMsg.OrigClientID = 10;

         //Test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(ackMsg, (Exception)null));

         //Checks
         Assert.AreEqual(sentMsg, contextMsgSent);
      }
      [TestMethod]
      public void MessageReceived_AckKeys_CallsClientContextAckReceived()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         Logon logonMsg = new Logon();
         var underTest = CreateTestItem(logonMsg);
         var context = underTest.AddClientContext();

         Header contextMsgSent = null;
         context.AckReceived += (x) => { contextMsgSent = x; };

         Header sentMsg = new Header();
         sentMsg.MsgKey = 16;
         sentMsg.MsgTypeID = MsgType.Custom;
         sentMsg.OrigClientType = 1;
         sentMsg.OrigClientID = 10;

         IClientContext outContext = Substitute.For<IClientContext>();
         _subscriberMsgListsMock.RemoveSentMessage(0, 1, 15, out outContext)
            .ReturnsForAnyArgs(sentMsg)
            .AndDoes(x => { x[3] = context; });

         Header ackMsg = new Header();
         ackMsg.MsgKey = 123;
         ackMsg.MsgTypeID = MsgType.Custom;
         ackMsg.AckKeys.Add(sentMsg.MsgKey);
         ackMsg.OrigClientType = 1;
         ackMsg.OrigClientID = 10;

         //Test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new Matrix.MsgService.CommunicationUtils.ConnectionHandler.MessageDetails(ackMsg, (Exception)null));

         //Checks
         Assert.AreEqual(sentMsg, contextMsgSent);
      }
      [TestMethod]
      public void MessageReceived_ReplyMsg_CallsClientContextMessageReceived()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         Logon logonMsg = new Logon();
         var underTest = CreateTestItem(logonMsg);
         var context = underTest.AddClientContext();

         Header contextMsgSent = null;
         Header contextMsgRx = null;
         context.MessageReceived += (x, y) => { contextMsgRx = x; contextMsgSent = y; };

         Header sentMsg = new Header();
         sentMsg.MsgKey = 16;
         sentMsg.MsgTypeID = MsgType.Custom;
         sentMsg.OrigClientType = 1;
         sentMsg.OrigClientID = 10;

         IClientContext outContext = Substitute.For<IClientContext>();
         _subscriberMsgListsMock.RemoveSentMessage(0, 1, 15, out outContext)
            .ReturnsForAnyArgs(sentMsg)
            .AndDoes(x => { x[3] = context; });

         Header replyMsg = new Header();
         replyMsg.MsgKey = 123;
         replyMsg.ReplyMsgKey = sentMsg.MsgKey;
         replyMsg.MsgTypeID = MsgType.Custom;
         replyMsg.OrigClientType = 1;
         replyMsg.OrigClientID = 10;

         //Test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new ConnectionHandler.MessageDetails(replyMsg, (Exception)null));

         //Checks
         Assert.AreEqual(replyMsg, contextMsgRx);
         Assert.AreEqual(sentMsg, contextMsgSent);
      }
      [TestMethod]
      public void MessageReceived_MessageWithNoContext_CallsNoClientContextMessageReceived()
      {
         //Setup
         CreateMocks();
         Exception ex = null;
         _connectionHandlerMock.Connect(out ex).Returns(x => { x[0] = null; return true; });
         Logon logonMsg = new Logon();
         var underTest = CreateTestItem(logonMsg);
         var context = underTest.AddClientContext();
         var noClientContext = underTest.NoClientContext;
         Header noClientContextMsgRx = null;
         noClientContext.MessageReceived += (x, y) => { noClientContextMsgRx = x; };

         Header contextMsgSent = null;
         Header contextMsgRx = null;
         context.MessageReceived += (x, y) => { contextMsgRx = x; contextMsgSent = y; };

         Header sentMsg = new Header();
         sentMsg.MsgKey = 16;
         sentMsg.MsgTypeID = MsgType.Custom;
         sentMsg.OrigClientType = 1;
         sentMsg.OrigClientID = 10;

         IClientContext outContext = Substitute.For<IClientContext>();
         _subscriberMsgListsMock.RemoveSentMessage(0, 1, 15, out outContext)
            .ReturnsForAnyArgs(sentMsg)
            .AndDoes(x => { x[3] = null; });

         Header noContextMsg = new Header();
         noContextMsg.MsgKey = 123;
         noContextMsg.ReplyMsgKey = sentMsg.MsgKey;
         noContextMsg.MsgTypeID = MsgType.Custom;
         noContextMsg.OrigClientType = 1;
         noContextMsg.OrigClientID = 10;

         //Test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new ConnectionHandler.MessageDetails(noContextMsg, (Exception)null));

         //Checks
         Assert.IsNull(contextMsgRx);
         Assert.IsNull(contextMsgSent);
         Assert.AreEqual(noContextMsg, noClientContextMsgRx, "MsgRx");
      }

      #endregion

      #region OnLogonComplete
      [TestMethod]
      public void ClientCommTest_OnLogonComplete_NoMsgStore_DoesNothing()
      {
         //setup
         CreateMocks();

         Header msg = new Header();
         msg.MsgKey = 1;
         msg.MsgTypeID = MsgType.Ack;
         _msgStoreMock = null;
         var underTest = CreateTestItem(null, 0, 0, 0);

         //test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new ConnectionHandler.MessageDetails(msg, (Exception)null));

         //expectations
         //doesn't blow up
      }
      [TestMethod]
      public void ClientCommTests_OnLogonComplete_MsgStore_CallsGetMessages()
      {
         //setup
         CreateMocks();

         Header msg = new Header();
         msg.MsgKey = 1;
         msg.MsgTypeID = MsgType.Ack;

         Logon logon = new Logon();

         var testItem = CreateTestItem(logon);

         //test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new ConnectionHandler.MessageDetails(msg, (Exception)null));

         //expectations
         _msgStoreMock.Received().GetMessages();
      }
      [TestMethod]
      public void ClientCommTests_OnLogonComplete_MsgStore_CallsAddMsgStoreRecord()
      {
         //setup
         CreateMocks();

         Header msg = new Header();
         msg.MsgKey = 1;
         msg.MsgTypeID = MsgType.Ack;

         var recordListMock = Substitute.For<CommunicationUtils.IMsgStoreRecordList>();
         var recordMock = Substitute.For<CommunicationUtils.IMsgStoreRecord>();
         recordMock.HdrMsg.Returns(new Header());
         var list = new List<CommunicationUtils.IMsgStoreRecord>();
         list.Add(recordMock);
         recordListMock.GetEnumerator().Returns(list.GetEnumerator());
         _msgStoreMock.GetMessages().Returns(recordListMock);

         Logon logon = new Logon();
         var testItem = CreateTestItem(logon);

         //test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new ConnectionHandler.MessageDetails(msg, (Exception)null));

         //expectations
         _msgStoreMock.ReceivedWithAnyArgs().AddMsgStoreRecord(null, 0);
      }
      [TestMethod]
      public void ClientCommTests_OnLogonComplete_SubscriberMsgList_NoTrack_DoesNotCallAddSentMessage()
      {
         //setup
         CreateMocks();

         Header msg = new Header();
         msg.MsgKey = 1;
         msg.MsgTypeID = MsgType.Ack;

         var recordListMock = Substitute.For<CommunicationUtils.IMsgStoreRecordList>();
         var recordMock = Substitute.For<CommunicationUtils.IMsgStoreRecord>();
         recordMock.HdrMsg.Returns(new Header());
         var list = new List<CommunicationUtils.IMsgStoreRecord>();
         list.Add(recordMock);
         recordListMock.GetEnumerator().Returns(list.GetEnumerator());
         _msgStoreMock.GetMessages().Returns(recordListMock);
         Header msgAdded =  null;
         _subscriberMsgListsMock.AddSentMessage(Arg.Do<Header>(x => msgAdded = x), null);

         Logon logon = new Logon();
         var testItem = CreateTestItem(logon, 0, 2000, 4000, 200);

         //test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new ConnectionHandler.MessageDetails(msg, (Exception)null));

         //expectations
         _subscriberMsgListsMock.DidNotReceiveWithAnyArgs().AddSentMessage(null, null);
      }
      [TestMethod]
      public void ClientCommTests_OnLogonComplete_SubscriberMsgList_Tracks_CallsAddSentMessage()
      {
         //setup
         CreateMocks();

         Header msg = new Header();
         msg.MsgKey = 1;
         msg.MsgTypeID = MsgType.Ack;

         var recordListMock = Substitute.For<CommunicationUtils.IMsgStoreRecordList>();
         var recordMock = Substitute.For<CommunicationUtils.IMsgStoreRecord>();
         Header storedMsg = new Header();
         storedMsg.MsgTypeID = MsgType.Custom;
         storedMsg.DestClientType = 2;
         recordMock.HdrMsg.Returns(storedMsg);
         var list = new List<CommunicationUtils.IMsgStoreRecord>();
         list.Add(recordMock);
         recordListMock.GetEnumerator().Returns(list.GetEnumerator());
         _msgStoreMock.GetMessages().Returns(recordListMock);
         Header msgAdded = null;
         _subscriberMsgListsMock.AddSentMessage(Arg.Do<Header>(x => msgAdded = x), null);

         Logon logon = new Logon();
         var testItem = CreateTestItem(logon, 0, 2000, 4000, 200);

         //test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new ConnectionHandler.MessageDetails(msg, (Exception)null));

         //expectations
         _subscriberMsgListsMock.ReceivedWithAnyArgs().AddSentMessage(null, null);
         Assert.IsTrue(msgAdded.IsArchived);
      }
      [TestMethod]
      public void ClientCommTests_OnLogonComplete_MsgStore_CallsSendsMessage()
      {
         //setup
         CreateMocks();

         Header msg = new Header();
         msg.MsgKey = 1;
         msg.MsgTypeID = MsgType.Ack;

         var recordListMock = Substitute.For<CommunicationUtils.IMsgStoreRecordList>();
         var recordMock = Substitute.For<CommunicationUtils.IMsgStoreRecord>();
         recordMock.HdrMsg.Returns(new Header());
         var list = new List<CommunicationUtils.IMsgStoreRecord>();
         list.Add(recordMock);
         recordListMock.GetEnumerator().Returns(list.GetEnumerator());
         _msgStoreMock.GetMessages().Returns(recordListMock);
         Exception ex;
         Header sentMsg = null;
         _connectionHandlerMock.SendMessage(Arg.Do<Header>(x => sentMsg = x), out ex);

         Logon logon = new Logon();
         var testItem = CreateTestItem(logon);

         //test
         _connectionHandlerMock.MessageReceived += Raise.EventWith(new ConnectionHandler.MessageDetails(msg, (Exception)null));

         //expectations
         _connectionHandlerMock.ReceivedWithAnyArgs().SendMessage(null, out ex);
         Assert.IsTrue(sentMsg.IsArchived);
      }
      #endregion

   }
}
