using Matrix.Utilities.DateTimeFunctions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommonMessages = Matrix.MsgService.CommonMessages;

namespace Matrix.MsgService.CommunicationUtils
{
   /// <summary>
   /// Interface for MsgStore - Class to store messages in the Frontier database that need to be sent after a restart
   /// </summary>
   public interface IMsgStore
   {
      /// <summary>
      /// Retrieve the list of stored messages
      /// </summary>
      /// <returns>the list of stored messages</returns>
      IMsgStoreRecordList GetMessages();
      /// <summary>
      /// Adds msgStoreRecord to the list
      /// </summary>
      /// <param name="msgStoreRecord">record to add</param>
      /// <param name="msgKey">key to assign</param>
      void AddMsgStoreRecord(IMsgStoreRecord msgStoreRecord, int msgKey);
      /// <summary>
      /// Save the message to the database
      /// </summary>
      /// <param name="msg">the Message to store</param>
      /// <param name="msgDate">Date of the Message</param>
      /// <returns>the ID of the stored record</returns>
      int StoreNewMessage(CommonMessages::Header msg, DateTime msgDate);
      /// <summary>
      /// Remove the message with msgKey from the store
      /// </summary>
      /// <param name="msgKey">msg key to remove</param>
      /// <returns>true if it was in the list, false if it wasn't</returns>
      bool RemoveMessage(int msgKey);
   }
   /// <summary>
   /// Interface for MsgStoreRecord - Message info
   /// </summary>
   public interface IMsgStoreRecord
   {
      /// <summary>
      /// ID of this record in storage
      /// </summary>
      int ID { get; }
      /// <summary>
      /// the Header of the msgStoreRecord
      /// </summary>
      CommonMessages.Header HdrMsg { get; }
      /// <summary>
      /// Date of the message
      /// </summary>
      Utilities.DateTimeFunctions.SmartDate MessageDate { get; }
   }
   /// <summary>
   /// Interface for IMsgStoreRecordList - list of stored Messages 
   /// </summary>
   public interface IMsgStoreRecordList : IEnumerable<IMsgStoreRecord>
   {
      /// <summary>
      /// provide an indexer for IMsgStoreRecord items in the list
      /// </summary>
      /// <param name="index">index of the item</param>
      /// <returns>the item at index</returns>
      IMsgStoreRecord this[int index] { get; }
      /// <summary>
      /// Returns the number of items in the list
      /// </summary>
      int Count { get; }
   }
   /// <summary>
   /// interface for MsgStoreRecordListFactory - Factory for creating IMsgStoreRecordList
   /// </summary>
   public interface IMsgStoreRecordListFactory
   {
      /// <summary>
      /// Client Type
      /// </summary>
      int ClientType { get; }
      /// <summary>
      /// Client ID
      /// </summary>
      int ClientID { get; }


      /// <summary>
      /// Gets a list of msgStoreRecords for controller
      /// </summary>
      /// <returns>a list of all msgStoreRecords for the client with clientType and ID</returns>
      IMsgStoreRecordList GetList();
      /// <summary>
      /// Add msg to the database list
      /// </summary>
      /// <param name="msg">message to add</param>
      /// <param name="msgDate">the date of the message</param>
      /// <returns>the record of newly stored record</returns>
      IMsgStoreRecord StoreMessage(CommonMessages.Header msg, DateTime msgDate);
      /// <summary>
      /// Remove record from the database list
      /// </summary>
      /// <param name="id">id of record to remove</param>
      void RemoveMessage(int id);
   }

   /// <summary>
   /// Class to store messages in the Frontier database that need to be sent after a restart
   /// </summary>
   public class MsgStore : IMsgStore
   {
      #region Fields
      IMsgStoreRecordListFactory _msgStoreRecordListFactory;
      /// <summary>
      /// stores the messages by msgKey
      /// </summary>
      Dictionary<int, IMsgStoreRecord> _keyDictionary;
      /// <summary>
      /// stores the messages by ID
      /// </summary>
      Dictionary<int, IMsgStoreRecord> _idDictionary;
      #endregion

      #region Constructors
      /// <summary>
      /// Internal Constructor - for use by unit tests
      /// </summary>
      /// <param name="clientType">type of client for the messages</param>
      /// <param name="clientID">ID of client for the messages</param>
      /// <param name="msgStoreRecordListFactory"></param>
      public MsgStore(int clientType, int clientID, IMsgStoreRecordListFactory msgStoreRecordListFactory)
      {
         ClientType = clientType;
         ClientID = clientID;
         _msgStoreRecordListFactory = msgStoreRecordListFactory;
         _keyDictionary = new Dictionary<int, IMsgStoreRecord>();
         _idDictionary = new Dictionary<int, IMsgStoreRecord>();
      }
      #endregion

      #region Public Properties/Methods
      /// <summary>
      /// Type of client for this store
      /// </summary>
      public int ClientType { get; private set; }
      /// <summary>
      /// ID of client for this store
      /// </summary>
      public int ClientID { get; private set; }

      /// <summary>
      /// Retrieve the list of stored messages
      /// </summary>
      /// <returns></returns>
      public IMsgStoreRecordList GetMessages()
      {
         return _msgStoreRecordListFactory.GetList();
      }
      /// <summary>
      /// Adds msgStoreRecord to the list
      /// </summary>
      /// <param name="msgStoreRecord">record to add</param>
      /// <param name="msgKey">key to assign</param>
      public void AddMsgStoreRecord(IMsgStoreRecord msgStoreRecord, int msgKey)
      {
         _keyDictionary[msgKey] = msgStoreRecord;
         _idDictionary[msgStoreRecord.ID] = msgStoreRecord;
      }
      /// <summary>
      /// Save the message to the database
      /// </summary>
      /// <param name="msg">the Message to store</param>
      /// <param name="msgDate">the Message date</param>
      /// <returns>the ID of the stored record</returns>
      public int StoreNewMessage(CommonMessages::Header msg, DateTime msgDate)
      {
         var msgStoreRecord = _msgStoreRecordListFactory.StoreMessage(msg, msgDate);
         _keyDictionary[msg.MsgKey] = msgStoreRecord;
         _idDictionary[msgStoreRecord.ID] = msgStoreRecord;
         return msgStoreRecord.ID;
      }
      /// <summary>
      /// Remove the message with msgKey from the store
      /// </summary>
      /// <param name="msgKey">msg key to remove</param>
      /// <returns>true if it was in the list, false if it wasn't</returns>
      public bool RemoveMessage(int msgKey)
      {
         IMsgStoreRecord msgStoreRecord;
         if (_keyDictionary.TryGetValue(msgKey, out msgStoreRecord))
         {
            _idDictionary.Remove(msgStoreRecord.ID);
            _keyDictionary.Remove(msgKey);
            _msgStoreRecordListFactory.RemoveMessage(msgStoreRecord.ID);
            return true;
         }
         return false;
      }
      #endregion


   }
}
