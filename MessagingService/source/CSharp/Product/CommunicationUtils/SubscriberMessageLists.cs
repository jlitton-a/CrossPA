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
      /// <param name="senderContext">the context of the client that sent the message</param>
      /// <returns>true if the message was added</returns>
      bool AddSentMessage(Header msg, IClientContext senderContext);
      /// <summary>
      /// Removes each message with a key in ackKeys from the subscriber
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <param name="ackKeys">List of keys that have been acked</param>
      /// <returns>true if the message was removed</returns>
      bool RemoveSentMessages(int clientType, int clientID, IEnumerable<int> ackKeys);
      /// <summary>
      /// Remove a message with msgKey from the subscriber
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <param name="msgKey">The key of the message that has been acked</param>
      /// <param name="context">set to the context of the client that originally sent the message</param>
      /// <returns>true if the message was removed</returns>
      Header RemoveSentMessage(int clientType, int clientID, int msgKey, out IClientContext context);
      /// <summary>
      /// Removes all messages that were sent by context with contextID
      /// </summary>
      /// <param name="context">the context for which to remove messages</param>
      /// <returns>a list of the messages removed</returns>
      List<Header> RemoveContext(IClientContext context);
      /// <summary>
      /// Add a context 
      /// </summary>
      /// <param name="context">the context to add</param>
      /// <returns>true if it was added, false if it was already there</returns>
      bool AddContext(IClientContext context);

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
      /// <summary>
      /// Clear out everything
      /// </summary>
      void Clear();
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
         /// <returns>true if message was added, false if it was updated</returns>
         public bool AddOrUpdateMessage(Header msg)
         {
            MsgInfo foundMsg;
            if (_msgList.TryGetValue(msg.MsgKey, out foundMsg))
            {
               foundMsg.Msg = msg;
               foundMsg.TimeSent = DateTime.Now;
               return false;
            }
            else
            {
               _msgList.TryAdd(msg.MsgKey, new MsgInfo(msg, DateTime.Now));
               return true;
            }
         }
         /// <summary>
         /// Remove messages with msgKey in msgKeys list
         /// </summary>
         /// <param name="msgKeys">msg keys to remove</param>
         /// <returns>true if at least one message was removed</returns>
         public bool RemoveMessages(IEnumerable<int> msgKeys)
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
         /// <returns>the message removed, null if there was no message</returns>
         public Header RemoveMessage(int msgKey)
         {
            MsgInfo msgInfo;
            if (_msgList.TryRemove(msgKey, out msgInfo) && msgInfo != null)
               return msgInfo.Msg;
            return null;
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
         /// Gets a list of all messages for this subscriber
         /// </summary>
         /// <returns>list of messages</returns>
         public List<Header> GetMessages()
         {
            List<Header> list = new List<Header>();
            var msgList = _msgList.Values.ToList();
            foreach(var item in msgList)
            {
               list.Add(item.Msg);
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
         /// <param name="msgKeys">the list of msgKeys to remove</param>
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

      class SentMessagesClientContexts
      {
         /// <summary>
         /// ClientContext for sent messages - Key is MsgKey
         /// </summary>
         ConcurrentDictionary<int, IClientContext> _messagesByMsgKey = new ConcurrentDictionary<int, IClientContext>();
         /// <summary>
         /// List of sent messages for client Context- Key is ContextID
         /// </summary>
         ConcurrentDictionary<IClientContext, ConcurrentDictionary<int,Header>> _messagesByContext = new ConcurrentDictionary<IClientContext, ConcurrentDictionary<int, Header>>();
         public SentMessagesClientContexts()
         { }
         public void AddMessage(Header msg, IClientContext senderContext)
         {
            if (senderContext != null)
            {
               _messagesByMsgKey.AddOrUpdate(msg.MsgKey, senderContext, (key, oldValue) => senderContext);
               var list = _messagesByContext.GetOrAdd(senderContext,
                     (contextID) => { return new ConcurrentDictionary<int, Header>(); });
               list.AddOrUpdate(msg.MsgKey, msg, (msgKey, oldMsg) => msg);
            }
         }
         /// <summary>
         /// Remove a message from the lists
         /// </summary>
         /// <param name="msgKey">key of the message to remove</param>
         /// <param name="msg">will be set to the message removed</param>
         /// <param name="context">will be set to the context that sent the message</param>
         /// <returns>true </returns>
         public bool RemoveMessage(int msgKey, out Header msg, out IClientContext context)
         {
            msg = null;
            if (_messagesByMsgKey.TryRemove(msgKey, out context))
            {
               if (context != null)
               {
                  ConcurrentDictionary<int, Header> list;
                  if (_messagesByContext.TryGetValue(context, out list))
                  {
                     list.TryRemove(msgKey, out msg);
                  }
                  return true;
               }
            }
            return false;
         }
         public void RemoveMessages(IEnumerable<int> msgKeys)
         {
            Header msg;
            IClientContext context;
            foreach (var msgKey in msgKeys)
            {
               RemoveMessage(msgKey, out msg, out context);
            }
         }
         public bool AddContext(IClientContext context)
         {
            bool added = false;
            _messagesByContext.GetOrAdd(context,
                  (contextID) => { added = true; return new ConcurrentDictionary<int, Header>(); });
            return added;
         }
         public List<Header> RemoveContext(IClientContext context)
         {
            if (context != null)
            {
               ConcurrentDictionary<int, Header> list;
               if (_messagesByContext.TryRemove(context, out list))
               {
                  foreach (var msgKey in list.Keys)
                  {
                     IClientContext outContext;
                     _messagesByMsgKey.TryRemove(msgKey, out outContext);
                  }
                  return list.Values.ToList();
               }
            }
            return null;
         }
         public void Clear()
         {
            _messagesByContext.Clear();
            _messagesByMsgKey.Clear();
            var list = _messagesByContext.Keys.ToList();
            foreach (var context in list)
            {
               context.Dispose();
            }
         }
      }
      #endregion

      #region Fields
      ConcurrentDictionary<SubscriberMessageKey, SubscriberMessages> _messageLists;
      SentMessagesClientContexts _sentMessagesByContext = new SentMessagesClientContexts();
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
         if(_messageLists.TryRemove(key, out subscriberMsgs))
         {
            var list = subscriberMsgs.GetMessages();
            //TODO: we should be checking for wait messages for any that are removed
            foreach (var msg in list)
            {
               IClientContext context;
               Header outMsg;
               _sentMessagesByContext.RemoveMessage(msg.MsgKey, out outMsg, out context);
            }
            return true;
         }
         return false;
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
      /// <param name="senderContext">the context of the client that sent the message</param>
      /// <returns>true if the message was added</returns>
      public bool AddSentMessage(Header msg, IClientContext senderContext)
      {
         bool added = false;
         if (msg.DestClientType > 0)
         {
            //add message to the subscriber
            var key = new SubscriberMessageKey(msg.DestClientType, msg.DestClientID);
            _messageLists.AddOrUpdate(key,
                  (msgKey) =>
                        {
                           var msgList = new SubscriberMessages(key);
                           added = msgList.AddOrUpdateMessage(msg);
                           return msgList;
                        },
                  (msgKey, existing) =>
                        {
                           added = existing.AddOrUpdateMessage(msg);
                           return existing;
                        });
            _sentMessagesByContext.AddMessage(msg, senderContext);
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
      public bool RemoveSentMessages(int clientType, int clientID, IEnumerable<int> msgKeys)
      {
         var key = new SubscriberMessageKey(clientType, clientID);
         SubscriberMessages msgList;
         if(_messageLists.TryGetValue(key, out msgList))
         {
            return msgList.RemoveMessages(msgKeys);
         }
         _sentMessagesByContext.RemoveMessages(msgKeys.ToList());
         return false;
      }
      /// <summary>
      /// Remove a message with msgKey from the subscriber
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <param name="msgKey">The msgKeys of the message that has been acked</param>
      /// <param name="context">set to the context of the client that originally sent the message</param>
      /// <returns>true if the message was removed</returns>
      public Header RemoveSentMessage(int clientType, int clientID, int msgKey, out IClientContext context)
      {
         Header msg = null;
         _sentMessagesByContext.RemoveMessage(msgKey, out msg, out context);
         var key = new SubscriberMessageKey(clientType, clientID);
         SubscriberMessages msgList;
         if (_messageLists.TryGetValue(key, out msgList))
         {
            return msgList.RemoveMessage(msgKey);
         }
         return msg;
      }
      /// <summary>
      /// Add a context 
      /// </summary>
      /// <param name="context">the context to add</param>
      /// <returns>true if it was added, false if it was already there</returns>
      public bool AddContext(IClientContext context)
      {
         return _sentMessagesByContext.AddContext(context);
      }
      /// <summary>
      /// Removes all messages that were sent by context with contextID
      /// </summary>
      /// <param name="context">the context for which to remove messages</param>
      /// <returns>a list of the messages removed</returns>
      public List<Header> RemoveContext(IClientContext context)
      {
         var msgList = _sentMessagesByContext.RemoveContext(context);
         if (msgList != null && msgList.Count() > 0)
         {
            foreach (var msg in msgList)
            {
               if (msg != null)
               {
                  var key = new SubscriberMessageKey(msg.DestClientType, msg.DestClientID);
                  SubscriberMessages messages;
                  if (_messageLists.TryGetValue(key, out messages))
                  {
                     messages.RemoveMessage(msg.MsgKey);
                  }
               }
            }
         }
         return msgList;
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
      /// <param name="msgKeyList">keys to remove</param>
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
      /// <summary>
      /// Clear everything and disposes of contexts
      /// </summary>
      public void Clear()
      {
         _sentMessagesByContext.Clear();
         _messageLists.Clear();
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
