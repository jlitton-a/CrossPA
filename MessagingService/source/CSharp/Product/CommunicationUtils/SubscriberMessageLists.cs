using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Matrix.MsgService.CommonMessages;

namespace Matrix.MsgService.CommunicationUtils
{
   /// <summary>
   /// Interface for SubscriberMessageLists - Tracks clients that are subscribed and the messages that were sent to them
   /// </summary>
   internal interface ISubscriberMessageLists : IEnumerable<ISubscriberMessages>
   {
      /// <summary>
      /// Adds a subscription to the lists
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <returns>true if it was added, false if it was already there</returns>
      bool AddSubscription(int clientType, int clientID);
      /// <summary>
      /// Removes a subscription from the lists
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <returns>true if it was removed, false if it was not there</returns>
      bool RemoveSubscription(int clientType, int clientID);
      /// <summary>
      /// Add a message to each subscribers list to wait for an ack
      /// </summary>
      /// <param name="msg">message to add</param>
      /// <returns>true if the message was added</returns>
      bool AddSentMessage(Header msg);
      /// <summary>
      /// Removes each message with a key in ackKeys from the subscriber
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <param name="ackKeys">List of keys that have been acked</param>
      /// <returns>true if the message was removed</returns>
      bool RemoveSentMessages(int clientType, int clientID, RepeatedField<int> ackKeys);
      /// <summary>
      /// Remove a message with msgKey from the subscriber
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <param name="msgKey">The key of the message that has been acked</param>
      /// <returns>true if the message was removed</returns>
      bool RemoveSentMessage(int clientType, int clientID, int msgKey);

      /// <summary>
      /// Adds msg.msgKey to the list of msgKeys that need to be acked
      /// for each subscribing client
      /// </summary>
      /// <param name="msg">the message to add to the list of keys that need to be acked</param>
      void AddToNeedToAckList(Header msg);
      /// <summary>
      /// Removes msg keys from the list of msg keys that need to be acked
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <param name="msgKeyList">msg keys to remove</param>
      void RemoveFromNeedToAckList(int clientType, int clientID, List<int> msgKeyList);
      /// <summary>
      /// Gets the list of msg keys that need to be acked
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <returns>list of msg keys that need to be acked</returns>
      List<int> GetNeedToAckList(int clientType, int clientID);
      /// <summary>
      /// Sets/clears the online flag for this client
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <param name="isOnline">true to set online, false to set offline</param>
      void SetClientOnLine(int clientType, int clientID, bool isOnline);
      /// <summary>
      /// returns the online flag for this client
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      bool IsClientOnline(int clientType, int clientID);
   }

   internal interface ISubscriberMessages
   {
      /// <summary>
      /// Gets a list of messages with TimeSent older than numMS
      /// </summary>
      /// <param name="numMS">the number of milliseconds</param>
      /// <param name="updateSentDate">if not null, update the date sent for the messages</param>
      /// <returns>list of messages with TimeSent older than numMS</returns>
      List<Header> GetMessages(uint numMS, DateTime? updateSentDate = null);
      SubscriberMessageLists.SubscriberMessageKey Key { get; }
      bool IsOnline { get; }

   }
   /// <summary>
   /// Tracks clients that are subscribed and the messages that were sent to them
   /// </summary>
   internal class SubscriberMessageLists : ISubscriberMessageLists
   {
      #region Sub-Classes
      public struct SubscriberMessageKey
      {
         public SubscriberMessageKey(int clientType, int clientID)
         {
            ClientType = clientType;
            ClientID = clientID;
         }

         public int ClientType { get; private set; }
         public int ClientID { get; private set; }
         public override bool Equals(object obj)
         {
            if (!(obj is SubscriberMessageKey))
               return false;
            var src = (SubscriberMessageKey)obj;
            return ClientType == src.ClientType && ClientID == src.ClientID;
         }
         public override int GetHashCode()
         {
            return ClientID ^ ClientType;
         }
      }
      class SubscriberMessages : ISubscriberMessages
      {
         /// <summary>
         /// Info about messages stored
         /// </summary>
         class MsgInfo
         {
            public Header Msg { get; set; }
            public DateTime TimeSent { get; set; }
            public MsgInfo(Header msg, DateTime timeSent)
            {
               Msg = msg;
               TimeSent = timeSent;
            }
         }
         #region Fields
         ConcurrentDictionary<int, MsgInfo> _msgList;
         SubscriberMessageKey _key;
         List<int> _msgKeysToAck;
         #endregion

         #region Constructors
         /// <summary>
         /// Creates the subscriber messages
         /// </summary>
         /// <param name="key"></param>
         public SubscriberMessages(SubscriberMessageKey key)
         {
            _key = key;
            _msgList = new ConcurrentDictionary<int, MsgInfo>();
            _msgKeysToAck = new List<int>();
            IsOnline = false;
         }
         #endregion

         #region Properties
         public SubscriberMessageKey Key { get { return _key; } }
         public bool IsOnline { get; set; }
         #endregion

         #region public methods
         /// <summary>
         /// Adds a message to the Sent list
         /// </summary>
         /// <param name="msg">message to add</param>
         /// <returns>true if message was added or updated</returns>
         public bool AddOrUpdateMessage(Header msg)
         {
            MsgInfo foundMsg;
            if (_msgList.TryGetValue(msg.MsgKey, out foundMsg))
            {
               foundMsg.Msg = msg;
               foundMsg.TimeSent = DateTime.Now;
            }
            else
               _msgList.TryAdd(msg.MsgKey, new MsgInfo(msg, DateTime.Now));
            return true;
         }
         /// <summary>
         /// Remove messages with msgKey in msgKeys list
         /// </summary>
         /// <param name="msgKeys">msg keys to remove</param>
         /// <returns>true if at least one message was removed</returns>
         public bool RemoveMessages(RepeatedField<int> msgKeys)
         {
            bool removed = false;
            if (msgKeys != null)
            {
               foreach (var msgKey in msgKeys)
               {
                  MsgInfo msgInfo;
                  return _msgList.TryRemove(msgKey, out msgInfo);
               }
            }
            return removed;
         }
         /// <summary>
         /// Remove messages with msgKey
         /// </summary>
         /// <param name="msgKey">msg key to remove</param>
         /// <returns>true if at least one message was removed</returns>
         public bool RemoveMessage(int msgKey)
         {
            MsgInfo msgInfo;
            return _msgList.TryRemove(msgKey, out msgInfo);
         }
         /// <summary>
         /// Gets a list of messages with TimeSent older than numMS
         /// </summary>
         /// <param name="numMS">the number of milliseconds</param>
         /// <param name="updateSentDate">if not null, update the date sent for the messages</param>
         /// <returns>list of messages with TimeSent older than numMS</returns>
         public List<Header> GetMessages(uint numMS, DateTime? updateSentDate = null)
         {
            List<Header> list = new List<Header>();
            DateTime dateTime = DateTime.Now.Subtract( new TimeSpan(0,0,0,0,(int)numMS));
            foreach(var msgInfo in _msgList.Values)
            {
               if (msgInfo.TimeSent < dateTime)
               {
                  list.Add(msgInfo.Msg);
                  if(updateSentDate != null)
                     msgInfo.TimeSent = (DateTime)updateSentDate;
               }
            }
            return list;
         }
         /// <summary>
         /// Add msgKey to the list of msgKeys that need to be acked
         /// </summary>
         /// <param name="msgKey">the msgKey to add</param>
         public void AddToNeedToAckList(int msgKey)
         {
            _msgKeysToAck.Add(msgKey);
         }
         /// <summary>
         /// Remove msgKeys from the list of msgKeys received (no longer need to be acked)
         /// </summary>
         /// <param name="msgKey">the msgKey to remove</param>
         public void RemoveNeedToAckKeys(List<int> msgKeys)
         {
            if (msgKeys != null)
            {
               foreach (var key in msgKeys)
               {
                  _msgKeysToAck.Remove(key);
               }
            }
         }
         /// <summary>
         /// Returns the list of msgKeys that need to be acked
         /// </summary>
         public List<int> GetNeedToAckKeys()
         {
            return new List<int>(_msgKeysToAck);
         }

         #endregion

      }

      #endregion

      #region Fields
      ConcurrentDictionary<SubscriberMessageKey, SubscriberMessages> _messageLists;
      #endregion

      #region Constructors
      /// <summary>
      /// Default constructor
      /// </summary>
      public SubscriberMessageLists()
      {
         _messageLists = new ConcurrentDictionary<SubscriberMessageKey, SubscriberMessages>();
      }
      #endregion

      #region ISubscriberMessageLists implementation
      /// <summary>
      /// Sets/clears the online flag for this client
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <param name="isOnline">true to set online, false to set offline</param>
      public void SetClientOnLine(int clientType, int clientID, bool isOnline)
      {
         var key = new SubscriberMessageKey(clientType, clientID);
         SubscriberMessages msgList;
         if (_messageLists.TryGetValue(key, out msgList))
         {
            msgList.IsOnline = isOnline;
         }
      }
      /// <summary>
      /// returns the online flag for this client
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      public bool IsClientOnline(int clientType, int clientID)
      {
         var key = new SubscriberMessageKey(clientType, clientID);
         SubscriberMessages msgList;
         if (_messageLists.TryGetValue(key, out msgList))
         {
            return msgList.IsOnline;
         }
         return false;
      }
      /// <summary>
      /// Adds a subscription to the lists
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <returns>true if it was added, false if it was already there</returns>
      public bool AddSubscription(int clientType, int clientID)
      {
         var key = new SubscriberMessageKey(clientType, clientID);
         return _messageLists.TryAdd(key, new SubscriberMessages(key));
      }
      /// <summary>
      /// Removes a subscription from the lists
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <returns>true if it was removed, false if it was not there</returns>
      public bool RemoveSubscription(int clientType, int clientID)
      {
         var key = new SubscriberMessageKey(clientType, clientID);
         SubscriberMessages subscriberMsgs;
         return _messageLists.TryRemove(key, out subscriberMsgs);
      }
      /// <summary>
      /// Is the client currently subscriped
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <returns>true if the client is subscribed</returns>
      public bool IsSubscribed(int clientType, int clientID)
      {
         var key = new SubscriberMessageKey(clientType, clientID);
         return _messageLists.ContainsKey(key);
      }
      /// <summary>
      /// Add a message to each subscribers list to wait for an ack
      /// NOTE: only adds if the message is for a specific client
      /// </summary>
      /// <param name="msg">message to add</param>
      /// <returns>true if the message was added</returns>
      public bool AddSentMessage(Header msg)
      {
         bool added = false;
         if (msg.DestClientType > 0)
         {
            var key = new SubscriberMessageKey(msg.DestClientType, msg.DestClientID);
            _messageLists.AddOrUpdate(key, 
                  (msgKey) =>
                        {
                           var msgList = new SubscriberMessages(key);
                           msgList.AddOrUpdateMessage(msg);
                           added = true;
                           return msgList;
                        },
                  (msgKey, existing) => 
                        {
                           existing.AddOrUpdateMessage(msg);
                           added = true;
                           return existing;
                        });
         }
         return added;
      }
      /// <summary>
      /// Removes each message with a key in ackKeys from the subscriber
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <param name="msgKeys">List of msgKeys that have been acked</param>
      /// <returns>true if the message was removed</returns>
      public bool RemoveSentMessages(int clientType, int clientID, RepeatedField<int> msgKeys)
      {
         var key = new SubscriberMessageKey(clientType, clientID);
         SubscriberMessages msgList;
         if(_messageLists.TryGetValue(key, out msgList))
         {
            return msgList.RemoveMessages(msgKeys);
         }
         return false;
      }
      /// <summary>
      /// Remove a message with msgKey from the subscriber
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <param name="msgKey">The msgKeys of the message that has been acked</param>
      /// <returns>true if the message was removed</returns>
      public bool RemoveSentMessage(int clientType, int clientID, int msgKey)
      {
         var key = new SubscriberMessageKey(clientType, clientID);
         SubscriberMessages msgList;
         if (_messageLists.TryGetValue(key, out msgList))
         {
            return msgList.RemoveMessage(msgKey);
         }
         return false;
      }
      /// <summary>
      /// Gets a list of messages that were last sent more than numMS ago
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <param name="numMS">Number of milliseconds since they were last sent</param>
      /// <returns>list of messages that were last sent more than numMS ago</returns>
      public List<Header> GetMessages(int clientType, int clientID, uint numMS)
      {
         var key = new SubscriberMessageKey(clientType, clientID);
         SubscriberMessages msgList;
         if (_messageLists.TryGetValue(key, out msgList))
         {
            return msgList.GetMessages(numMS);
         }
         return null;
      }
      /// <summary>
      /// Adds msg.msgKey to the list of msgKeys that need to be acked
      /// for each subscribing client
      /// </summary>
      /// <param name="msg">the message to add to the list of keys that need to be acked</param>
      public void AddToNeedToAckList(Header msg)
      {
         //if it is for a specific client, add to that list
         if (msg.DestClientType > 0)
         {
            var key = new SubscriberMessageKey(msg.DestClientType, msg.DestClientID);

            _messageLists.AddOrUpdate(key,
                  (msgKey) =>
                  {
                     var msgList = new SubscriberMessages(key);
                     msgList.AddToNeedToAckList(msg.MsgKey);
                     return msgList;
                  },
                  (msgKey, existing) =>
                  {
                     existing.AddToNeedToAckList(msg.MsgKey);
                     return existing;
                  });
         }
      }
      /// <summary>
      /// Removes keys from the list of keys that need to be acked
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <param name="ackKeyList">keys to remove</param>
      public void RemoveFromNeedToAckList(int clientType, int clientID, List<int> msgKeyList)
      {
         var key = new SubscriberMessageKey(clientType, clientID);
         SubscriberMessages msgList;
         if (_messageLists.TryGetValue(key, out msgList))
         {
            msgList.IsOnline = true;
            msgList.RemoveNeedToAckKeys(msgKeyList);
         }
      }
      /// <summary>
      /// Gets the list of keys that need to be acked
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <returns>list of keys that need to be acked</returns>
      public List<int> GetNeedToAckList(int clientType, int clientID)
      {
         var key = new SubscriberMessageKey(clientType, clientID);
         SubscriberMessages msgList;
         if (_messageLists.TryGetValue(key, out msgList))
         {
            return msgList.GetNeedToAckKeys();
         }
         return null;
      }

      public IEnumerator<ISubscriberMessages> GetEnumerator()
      {
         return _messageLists.Values.GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return _messageLists.Values.GetEnumerator();
      }

      #endregion
   }
}
