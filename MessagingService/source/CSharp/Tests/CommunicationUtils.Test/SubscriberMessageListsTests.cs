using System;
using System.Collections.Generic;
using Matrix.MsgService.CommonMessages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

using CommonMessages = Matrix.MsgService.CommonMessages;

namespace Matrix.MsgService.CommunicationUtils.Test
{
   [TestClass]
   public class SubscriberMessageListsTests
   {
      #region AddToNeedToAckList
      [TestMethod]
      public void SubscriberMessageListsTests_AddToNeedToAckList_ClientNotThere_Adds()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         Header msg = new Header();
         int clientType = 1;
         int clientID = 2;
         msg.DestClientType = clientType;
         msg.DestClientID = clientID;
         msg.MsgKey = 3;

         //Test
         underTest.AddToNeedToAckList(msg);

         //Checks
         var result = underTest.GetNeedToAckList(clientType, clientID);
         Assert.AreEqual(1, result.Count);
         Assert.AreEqual(msg.MsgKey, result[0]);
      }
      [TestMethod]
      public void SubscriberMessageListsTests_AddToNeedToAckList_ClientThere_AddsKey()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         int clientType = 1;
         int clientID = 2;
         underTest.AddSubscription(clientType, clientID);
         bool isSubscribed = underTest.IsSubscribed(1, 2);
         Assert.IsTrue(isSubscribed, "Is subscribed before");

         Header msg = new Header();
         msg.MsgKey = 5;
         msg.DestClientType = clientType;
         msg.DestClientID = clientID;

         //Test
         underTest.AddToNeedToAckList(msg);

         //Checks
         var result = underTest.GetNeedToAckList(clientType, clientID);
         Assert.AreEqual(1, result.Count);
         Assert.AreEqual(msg.MsgKey, result[0]);
      }
      #endregion

      #region AddSubscription
      [TestMethod]
      public void SubscriberMessageListsTests_AddSubscription_WasNotThere_Adds()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();

         //Test
         bool added = underTest.AddSubscription(1, 2);

         //Checks
         Assert.IsTrue(added, "Was added");
         Assert.IsTrue(underTest.IsSubscribed(1, 2), "Is subscribed");
      }
      [TestMethod]
      public void SubscriberMessageListsTests_AddSubscription_WasAlreadyThere_DoesNotAdd()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         underTest.AddSubscription(1, 2);
         bool isSubscribed = underTest.IsSubscribed(1, 2);
         Assert.IsTrue(isSubscribed, "Is subscribed before");

         //Test
         bool added = underTest.AddSubscription(1, 2);

         //Checks
         Assert.IsFalse(added, "Return value");
         Assert.IsTrue(underTest.IsSubscribed(1, 2), "Is subscribed after");
      }
      #endregion

      #region AddSentMessage
      [TestMethod]
      public void SubscriberMessageListsTests_AddSentMessage_NoDestClient_ReturnsFalse()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         Header msg = new Header();
         msg.MsgKey = 1;

         //Test
         bool added = underTest.AddSentMessage(msg);

         //Checks
         Assert.IsFalse(added, "Return value");
      }
      [TestMethod]
      public void SubscriberMessageListsTests_AddSentMessage_NotThere_Adds()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         Header msg = new Header();
         msg.DestClientType = 15;
         msg.MsgKey = 3;

         //Test
         bool added = underTest.AddSentMessage(msg);

         //Checks
         Assert.IsTrue(added, "Return value");
         Assert.IsTrue(underTest.IsSubscribed(15,0));
      }
      [TestMethod]
      public void SubscriberMessageListsTests_AddSentMessage_There_ReturnsTrue()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         underTest.AddSubscription(1, 2);
         Header msg = new Header();
         msg.MsgKey = 3;
         msg.DestClientType = 1;
         msg.DestClientID = 2;

         //Test
         bool added = underTest.AddSentMessage(msg);

         //Checks
         Assert.IsTrue(added, "Return value");
      }
      [TestMethod]
      public void SubscriberMessageListsTests_AddSentMessage_MsgThere_ReturnsFalse()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         underTest.AddSubscription(1, 2);
         Header msg = new Header();
         msg.MsgKey = 3;
         msg.DestClientType = 1;
         msg.DestClientID = 2;
         underTest.AddSentMessage(msg);

         //Test
         bool added = underTest.AddSentMessage(msg);

         //Checks
         Assert.IsFalse(added, "Return value");
      }
      #endregion

      #region GetMessages
      [TestMethod]
      public void SubscriberMessageListsTests_GetMessages_NotThere_ReturnsNull()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();

         //Test
         var list = underTest.GetMessages(1, 2, 3);

         //Checks
         Assert.IsNull(list, "Return value");
      }
      public void SubscriberMessageListsTests_GetMessages_NoMessages_ReturnsEmptyList()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         underTest.AddSubscription(1, 2);

         //Test
         var list = underTest.GetMessages(1, 2, 100);

         //Checks
         Assert.IsNull(list, "Return value");
      }
      public void SubscriberMessageListsTests_GetMessages_NoOlderMessages_ReturnsEmptyList()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         underTest.AddSubscription(1, 2);
         var msg = new Header();
         msg.MsgKey = 1;
         underTest.AddSentMessage(msg);

         //Test
         var list = underTest.GetMessages(1, 2, 1000);

         //Checks
         Assert.AreEqual(0, list.Count, "Empty list");
      }
      public void SubscriberMessageListsTests_GetMessages_HasMessages_ReturnsList()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         underTest.AddSubscription(1, 2);
         var msg = new Header();
         msg.MsgKey = 1;
         msg.Topic = 3;
         underTest.AddSentMessage(msg);
         System.Threading.Thread.Sleep(30);

         //Test
         var list = underTest.GetMessages(1, 2, 1);

         //Checks
         Assert.AreEqual(1, list.Count, "Empty list");
      }
      #endregion

      #region RemoveFromNeedToAckList
      [TestMethod]
      public void SubscriberMessageListsTests_RemoveFromNeedToAckList_NotThere_DoesNothing()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         int clientType = 1;
         int clientID = 2;
         underTest.AddSubscription(clientType, clientID);
         bool isSubscribed = underTest.IsSubscribed(1, 2);
         Assert.IsTrue(isSubscribed, "Is subscribed before");
         Header msg = new Header();
         msg.MsgKey = 5;
         msg.DestClientID = clientID;
         msg.DestClientType = clientType;
         underTest.AddToNeedToAckList(msg);

         var keyList = new List<int>();
         keyList.Add(msg.MsgKey);

         //Test
         underTest.RemoveFromNeedToAckList(clientType + 1, clientID, keyList);

         //Checks
         var result = underTest.GetNeedToAckList(clientType, clientID);
         Assert.AreEqual(1, result.Count);
      }
      [TestMethod]
      public void SubscriberMessageListsTests_RemoveFromNeedToAckList_There_WrongKey_DoesNothing()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         int clientType = 1;
         int clientID = 2;
         underTest.AddSubscription(clientType, clientID);
         bool isSubscribed = underTest.IsSubscribed(1, 2);
         Assert.IsTrue(isSubscribed, "Is subscribed before");
         Header msg = new Header();
         msg.MsgKey = 5;
         msg.DestClientID = clientID;
         msg.DestClientType = clientType;
         underTest.AddToNeedToAckList(msg);

         var keyList = new List<int>();
         keyList.Add(msg.MsgKey + 1);

         //Test
         underTest.RemoveFromNeedToAckList(clientType, clientID, keyList);

         //Checks
         var result = underTest.GetNeedToAckList(clientType, clientID);
         Assert.AreEqual(1, result.Count);
      }
      [TestMethod]
      public void SubscriberMessageListsTests_RemoveFromNeedToAckList_RemovesKey()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         int clientType = 1;
         int clientID = 2;
         underTest.AddSubscription(clientType, clientID);
         bool isSubscribed = underTest.IsSubscribed(1, 2);
         Assert.IsTrue(isSubscribed, "Is subscribed before");
         Header msg = new Header();
         msg.MsgKey = 5;
         msg.DestClientID = clientID;
         msg.DestClientType = clientType;
         underTest.AddToNeedToAckList(msg);

         var keyList = new List<int>();
         keyList.Add(msg.MsgKey);

         //Test
         underTest.RemoveFromNeedToAckList(clientType, clientID, keyList);

         //Checks
         var result = underTest.GetNeedToAckList(clientType, clientID);
         Assert.AreEqual(0, result.Count);
      }
      #endregion

      #region RemoveSentMessage
      [TestMethod]
      public void SubscriberMessageListsTests_RemoveSentMessage_NotSubscribed_ReturnsFalse()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         int msgKey = 1;

         //Test
         bool removed = underTest.RemoveSentMessage(1, 2, msgKey);

         //Checks
         Assert.IsFalse(removed, "Return value");
      }
      [TestMethod]
      public void SubscriberMessageListsTests_RemoveSentMessage_KeyNotThere_ReturnsFalse()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         int msgKey = 1;
         Header msg = new Header();
         msg.MsgKey = msgKey;
         msg.DestClientType = 3;
         msg.DestClientID = 5;
         underTest.AddSubscription(msg.DestClientType, msg.DestClientID);

         //Test
         bool removed = underTest.RemoveSentMessage(msg.DestClientType, msg.DestClientID, msgKey);

         //Checks
         Assert.IsFalse(removed, "Return value");
      }
      [TestMethod]
      public void SubscriberMessageListsTests_RemoveSentMessage_ReturnsTrue()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         int msgKey = 1;
         Header msg = new Header();
         msg.MsgKey = msgKey;
         msg.DestClientType = 3;
         msg.DestClientID = 6;
         underTest.AddSentMessage(msg);

         //Test
         bool removed = underTest.RemoveSentMessage(msg.DestClientType, msg.DestClientID, msgKey);

         //Checks
         Assert.IsTrue(removed, "Return value");
      }
      #endregion

      #region RemoveSentMessages
      [TestMethod]
      public void SubscriberMessageListsTests_RemoveSentMessages_NotSubscribed_ReturnsFalse()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         var ackKeys = new Google.Protobuf.Collections.RepeatedField<int>();
         ackKeys.Add(1);

         //Test
         bool removed = underTest.RemoveSentMessages(1, 2, ackKeys);

         //Checks
         Assert.IsFalse(removed, "Return value");
      }
      [TestMethod]
      public void SubscriberMessageListsTests_RemoveSentMessages_NullKeys_ReturnsFalse()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         underTest.AddSubscription(1, 2);
         Google.Protobuf.Collections.RepeatedField<int> ackKeys = null;

         //Test
         bool removed = underTest.RemoveSentMessages(1, 2, ackKeys);

         //Checks
         Assert.IsFalse(removed, "Return value");
      }
      [TestMethod]
      public void SubscriberMessageListsTests_RemoveSentMessages_NotInKeys_ReturnsFalse()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         underTest.AddSubscription(1, 2);
         var ackKeys = new Google.Protobuf.Collections.RepeatedField<int>();
         ackKeys.Add(1);

         //Test
         bool removed = underTest.RemoveSentMessages(1, 2, ackKeys);

         //Checks
         Assert.IsFalse(removed, "Return value");
      }
      [TestMethod]
      public void SubscriberMessageListsTests_RemoveSentMessages_InKeys_ReturnsTrue()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         Header msg = new Header();
         msg.MsgKey = 3;
         msg.DestClientType = 15;
         msg.DestClientID = 5;
         underTest.AddSentMessage(msg);
         var ackKeys = new Google.Protobuf.Collections.RepeatedField<int>();
         ackKeys.Add(msg.MsgKey);

         //Test
         bool removed = underTest.RemoveSentMessages(msg.DestClientType, msg.DestClientID, ackKeys);

         //Checks
         Assert.IsTrue(removed, "Return value");
      }
      #endregion

      #region RemoveSubscription
      [TestMethod]
      public void SubscriberMessageListsTests_RemoveSubscription_SubscriberNotThere_ReturnsFalse()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();

         //Test
         bool removed = underTest.RemoveSubscription(1, 2);

         //Checks
         Assert.IsFalse(removed, "Return value");
      }
      [TestMethod]
      public void SubscriberMessageListsTests_RemoveSubscription_SubscriberThere_RemovesSubscriber()
      {
         //Setup
         var underTest = new Matrix.MsgService.CommunicationUtils.SubscriberMessageLists();
         underTest.AddSubscription(1, 2);
         Assert.IsTrue(underTest.IsSubscribed(1, 2), "Is subscribed before");

         //Test
         bool removed = underTest.RemoveSubscription(1, 2);

         //Checks
         Assert.IsTrue(removed, "Return value");
         Assert.IsFalse(underTest.IsSubscribed(1, 2), "Is subscribed after");
      }
      #endregion

   }
}
