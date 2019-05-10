using System;
using System.Linq;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Timers;
using Google.Protobuf;
using Matrix.Utilities.DateTimeFunctions;

namespace Matrix.MsgService.CommunicationUtils.Test
{
   /// <summary>
   /// Tests for MsgStore
   /// </summary>
   [TestClass]
   public class MsgStoreTests
   {
      IMsgStoreRecordListFactory _msgStoreRecordListFactory;
      void CreateMocks()
      {
         _msgStoreRecordListFactory = Substitute.For<IMsgStoreRecordListFactory>();
      }

      #region GetMessages
      [TestMethod]
      public void MsgStore_GetMessages_CallsGetList()
      {
         //setup
         CreateMocks();
         var testItem = new MsgStore((int)CommonMessages.ClientTypes.AssaAbloy, 0, _msgStoreRecordListFactory);

         //test
         var list = testItem.GetMessages();

         //expectations
         _msgStoreRecordListFactory.Received().GetList();

         //cleanup
      }
      #endregion

      #region AddMsgStoreRecord
      [TestMethod]
      public void MsgStore_AddMsgStoreRecord_works()
      {
         //setup
         CreateMocks();
         var msgStoreRecord = Substitute.For<IMsgStoreRecord>();
         msgStoreRecord.ID.Returns(3);
         msgStoreRecord.MessageDate.Returns(new SmartDate(DateTime.Now));
         CommonMessages.Header msg = new CommonMessages.Header();
         msgStoreRecord.HdrMsg.Returns(msg);

         var testItem = new MsgStore((int)CommonMessages.ClientTypes.AssaAbloy, 0, _msgStoreRecordListFactory);

         //test
         testItem.AddMsgStoreRecord(msgStoreRecord, 32);

         //expectations
         //doesn't blow up?

         //cleanup
      }
      #endregion

      #region StoreNewMessage
      [TestMethod]
      public void MsgStore_StoreNewMessage_CallsStoreMessage()
      {
         //setup
         CreateMocks();
         CommonMessages.Header msg = new CommonMessages.Header();
         msg.MsgTypeID = CommonMessages.MsgType.Subscribe;
         DateTime msgDate = DateTime.Now;

         var testItem = new MsgStore((int)CommonMessages.ClientTypes.AssaAbloy, 0, _msgStoreRecordListFactory);

         //test
         var list = testItem.StoreNewMessage(msg, msgDate);

         //expectations
         _msgStoreRecordListFactory.Received().StoreMessage(msg, msgDate);

         //cleanup
      }
      [TestMethod]
      public void MsgStore_StoreNewMessage_ReturnsID()
      {
         //setup
         CreateMocks();
         CommonMessages.Header msg = new CommonMessages.Header();
         msg.MsgTypeID = CommonMessages.MsgType.Subscribe;
         CommonMessages.Subscribe subMsg = new CommonMessages.Subscribe();
         msg.Msg = (subMsg as IMessage).ToByteString();
         DateTime msgDate = DateTime.Now;

         int expectID = 3;
         var msgStoreRecord = Substitute.For<IMsgStoreRecord>();
         msgStoreRecord.ID.Returns(expectID);

         _msgStoreRecordListFactory.StoreMessage(msg, msgDate).Returns(msgStoreRecord);

         var testItem = new MsgStore((int)CommonMessages.ClientTypes.AssaAbloy, 0, _msgStoreRecordListFactory);

         //test
         var id = testItem.StoreNewMessage(msg, msgDate);

         //expectations
         Assert.AreEqual(expectID, id);

         //cleanup
      }
      #endregion

      #region RemoveMessage
      [TestMethod]
      public void MsgStore_RemoveMessage_NoKey_ReturnsFalse()
      {
         //setup
         CreateMocks();

         var msgStoreRecord = Substitute.For<IMsgStoreRecord>();
         msgStoreRecord.ID.Returns(3);
         msgStoreRecord.MessageDate.Returns(new SmartDate(DateTime.Now));
         CommonMessages.Header msg = new CommonMessages.Header();
         msgStoreRecord.HdrMsg.Returns(msg);

         int msgKey = 6;
         var testItem = new MsgStore((int)CommonMessages.ClientTypes.AssaAbloy, 0, _msgStoreRecordListFactory);
         testItem.AddMsgStoreRecord(msgStoreRecord, msgKey);

         //test
         var wasRemoved = testItem.RemoveMessage(msgKey + 15);

         //expectations
         Assert.IsFalse(wasRemoved);
         _msgStoreRecordListFactory.DidNotReceiveWithAnyArgs().RemoveMessage(0);

         //cleanup
      }
      [TestMethod]
      public void MsgStore_RemoveMessage_KeyThere_ReturnsTrue()
      {
         //setup
         CreateMocks();
         var msgStoreRecord = Substitute.For<IMsgStoreRecord>();
         msgStoreRecord.ID.Returns(3);
         msgStoreRecord.MessageDate.Returns(new SmartDate(DateTime.Now));
         CommonMessages.Header msg = new CommonMessages.Header();
         msgStoreRecord.HdrMsg.Returns(msg);

         int msgKey = 6;
         var testItem = new MsgStore((int)CommonMessages.ClientTypes.AssaAbloy, 0, _msgStoreRecordListFactory);
         testItem.AddMsgStoreRecord(msgStoreRecord, msgKey);

         //test
         var wasRemoved = testItem.RemoveMessage(msgKey);

         //expectations
         Assert.IsTrue(wasRemoved);

         //cleanup
      }
      [TestMethod]
      public void MsgStore_RemoveMessage_KeyAddedWithAddMsgStoreRecord_CallsRemoveMessage()
      {
         //setup
         CreateMocks();
         var msgStoreRecord = Substitute.For<IMsgStoreRecord>();
         int id = 3;
         msgStoreRecord.ID.Returns(id);
         msgStoreRecord.MessageDate.Returns(new SmartDate(DateTime.Now));
         CommonMessages.Header msg = new CommonMessages.Header();
         msgStoreRecord.HdrMsg.Returns(msg);

         int msgKey = 6;
         var testItem = new MsgStore((int)CommonMessages.ClientTypes.AssaAbloy, 0, _msgStoreRecordListFactory);

         //test
         testItem.AddMsgStoreRecord(msgStoreRecord, msgKey);
         var wasRemoved = testItem.RemoveMessage(msgKey);

         //expectations
         _msgStoreRecordListFactory.Received().RemoveMessage(id);

         //cleanup
      }
      [TestMethod]
      public void MsgStore_RemoveMessage_KeyAddedWithStoreNewMessage_CallsRemoveMessage()
      {
         //setup
         CreateMocks();
         int msgKey = 6;
         var msg = new CommonMessages.Header();
         msg.MsgTypeID = CommonMessages.MsgType.Subscribe;
         msg.MsgTypeID = CommonMessages.MsgType.Subscribe;
         CommonMessages.Subscribe subMsg = new CommonMessages.Subscribe();
         msg.Msg = (subMsg as IMessage).ToByteString();
         msg.MsgKey = msgKey;
         DateTime msgDate = DateTime.Now;

         var msgStoreRecord = Substitute.For<IMsgStoreRecord>();
         int id = 15;
         msgStoreRecord.ID.Returns(id);
         msgStoreRecord.MessageDate.Returns(new SmartDate(DateTime.Now));
         msgStoreRecord.HdrMsg.Returns(msg);

         var testItem = new MsgStore((int)CommonMessages.ClientTypes.AssaAbloy, 0, _msgStoreRecordListFactory);
         _msgStoreRecordListFactory.StoreMessage(msg, msgDate).Returns(msgStoreRecord);
         
         //test
         testItem.StoreNewMessage(msg, msgDate);
         var wasRemoved = testItem.RemoveMessage(msgKey);

         //expectations
         _msgStoreRecordListFactory.Received().RemoveMessage(id);

         //cleanup
      }
      #endregion
   }
}
