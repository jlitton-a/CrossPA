using Google.Protobuf;
using Matrix.MsgService.CommonMessages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.MsgService.CommunicationUtils
{
   /// <summary>
   /// Interface for ClientContextInterface - Allows different parts of an application to communicate with the message service
   /// </summary>
   public interface IClientContextInterface : IDisposable
   {
      /// <summary>
      /// The context for this client interface
      /// </summary>
      IClientContext Context { get; }
      /// <summary>
      /// Is the service currently connected
      /// </summary>
      bool IsConnected { get; }
      /// <summary>
      /// Returns true if client is online
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <returns>online status of client</returns>
      ClientComm.OnlineStatus IsClientOnline(int clientType, int clientID);
      /// <summary>
      /// Return true if this msg needs to be Acked (e.g. it needs to be tracked and resent until acked)
      /// </summary>
      /// <param name="sentMsg">the message to check</param>
      /// <returns>true if the message needs to be acked and resent until acked</returns>
      bool NeedToAckMsg(Header sentMsg);
      /// <summary>
      /// Send an Ack for a message
      /// </summary>
      /// <param name="msgToAck">the message to be acked</param>
      void SendAckMessage(Header msgToAck);
      /// <summary>
      /// Send an acknowledgement that msgToNack was received but was not processed.
      /// reason and details may provide information about why it was not processed
      /// </summary>
      /// <param name="msgToNack">the message to be acked</param>
      /// <param name="reason">client defined reason identifier for the Nack</param>
      /// <param name="details">details for the Nack</param>
      void SendNackMessage(Header msgToNack, int reason = 0, string details = "");
      /// <summary>
      /// Sends a common message, returns the MsgKey used
      /// </summary>
      /// <param name="msgType">type of message</param>
      /// <param name="msgToSend">sub message to send</param>
      /// <param name="topic">the topic of this message</param>
      /// <param name="destClientType">specific client type to send it to, 0 to broadcast</param>
      /// <param name="destClientID">specific client ID to send it to, 0 for all</param>
      /// <param name="replyMsgKey">the key of the message for which this message is a reply, 0 if it is not a reply</param>
      /// <param name="storeMsg">true to store this message for resending on application restart</param>
      /// <param name="isArchived">true to set the isArchived flag</param>
      /// <returns>The msg that was sent</returns>
      Header SendCommonMessage(MsgType msgType, Google.Protobuf.IMessage msgToSend, int topic = 0
            , int destClientType = 0, int destClientID = 0
            , int replyMsgKey = 0, bool storeMsg = false, bool isArchived = false);
      /// <summary>
      /// Sends a common message and waits for a response. Returns the actual message sent
      /// </summary>
      /// <param name="receivedMsg">The message response received, null if it was not received before maxWaitTimeMS</param>
      /// <param name="msgType">type of message</param>
      /// <param name="msgToSend">sub message to send</param>
      /// <param name="topic">the topic of this message</param>
      /// <param name="destClientType">specific client type to send it to, 0 to broadcast</param>
      /// <param name="destClientID">specific client ID to send it to, 0 for all</param>
      /// <param name="replyMsgKey">the key of the message for which this message is a reply, 0 if it is not a reply</param>
      /// <param name="maxWaitTimeMS">Maximum time to wait before timing out</param>
      /// <returns>The msg that was sent</returns> 
      Header SendCommonMessageAndWait(out Header receivedMsg
            , MsgType msgType, Google.Protobuf.IMessage msgToSend, int topic
            , int destClientType, int destClientID
            , int replyMsgKey = 0, int maxWaitTimeMS = ClientComm.DEFAULT_WAITTIME);
   }
   /// <summary>
   /// Allows different parts of an application to communicate with the message service
   /// </summary>
   public class ClientContextInterface : IClientContextInterface
   {
      #region Constructor/Dispose
      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="context">the context for this client interface</param>
      public ClientContextInterface(IClientContext context)
      {
         Context = context;
      }

      private bool disposedValue = false; // To detect redundant calls
      /// <summary>
      /// Remove from ClientComm
      /// </summary>
      /// <param name="disposing"></param>
      protected virtual void Dispose(bool disposing)
      {
         if (!disposedValue)
         {
            if (disposing)
            {
               if(Context != null)
               {
                  Context.Dispose();
                  Context = null;
               }
            }

            disposedValue = true;
         }
      }
      /// <summary>
      /// Remove from ClientComm
      /// </summary>
      public void Dispose()
      {
         // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
         Dispose(true);
      }
      #endregion

      #region Properties
      /// <summary>
      /// The context for this client interface
      /// </summary>
      public IClientContext Context { get; private set; }
      /// <summary>
      /// Is the service currently connected
      /// </summary>
      public bool IsConnected { get { return (Context == null || Context.ClientMsgComm == null) ? false : Context.ClientMsgComm.IsConnected; } }
      #endregion

      #region Methods
      /// <summary>
      /// Returns true if client is online
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <returns>online status of client</returns>
      public ClientComm.OnlineStatus IsClientOnline(int clientType, int clientID)
      {
         return (Context == null || Context.ClientMsgComm == null) ? ClientComm.OnlineStatus.NotOnline : Context.ClientMsgComm.IsClientOnline(clientType, clientID);
      }
      /// <summary>
      /// Return true if this msg needs to be Acked (e.g. it needs to be tracked and resent until acked)
      /// </summary>
      /// <param name="sentMsg">the message to check</param>
      /// <returns>true if the message needs to be acked and resent until acked</returns>
      public bool NeedToAckMsg(Header sentMsg) { return (Context == null || Context.ClientMsgComm == null) ? false : Context.ClientMsgComm.NeedToAckMsg(sentMsg); }
      /// <summary>
      /// Send an Ack for a message
      /// </summary>
      /// <param name="msgToAck">the message to be acked</param>
      public void SendAckMessage(Header msgToAck)
      {
         if (Context != null && Context.ClientMsgComm != null)
            Context.ClientMsgComm.SendAckMessage(msgToAck);
      }
      /// <summary>
      /// Send an acknowledgement that msgToNack was received but was not processed.
      /// reason and details may provide information about why it was not processed
      /// </summary>
      /// <param name="msgToNack">the message to be acked</param>
      /// <param name="reason">client defined reason identifier for the Nack</param>
      /// <param name="details">details for the Nack</param>
      public void SendNackMessage(Header msgToNack, int reason = 0, string details = "")
      {
         if (Context != null && Context.ClientMsgComm != null)
            Context.ClientMsgComm.SendNackMessage(msgToNack, reason, details);
      }
      /// <summary>
      /// </summary>
      /// <param name="replyToMsg">the message for which to send a reply message</param>
      /// <param name="msgType">type of message</param>
      /// <param name="msgToSend">sub message to send</param>
      /// <param name="storeMsg">true to store this message for resending on application restart</param>
      /// <param name="isArchived">true to set the isArchived flag</param>
      /// <returns>The msg that was sent</returns>
      public Header SendReplyMessage(MsgType msgType, IMessage msgToSend, Header replyToMsg
            , bool storeMsg = false, bool isArchived = false)
      {
         if (Context != null && Context.ClientMsgComm != null && replyToMsg != null)
         {
            return Context.ClientMsgComm.SendCommonMessage(Context, msgType, msgToSend, 0
                  , replyToMsg.OrigClientType, replyToMsg.OrigClientID, replyToMsg.MsgKey, storeMsg, isArchived);
         }
         return null;
      }

      /// <summary>
      /// Sends a common message, returns the MsgKey used
      /// </summary>
      /// <param name="msgType">type of message</param>
      /// <param name="msgToSend">sub message to send</param>
      /// <param name="topic">the topic of this message</param>
      /// <param name="destClientType">specific client type to send it to, 0 to broadcast</param>
      /// <param name="destClientID">specific client ID to send it to, 0 for all</param>
      /// <param name="replyMsgKey">the key of the message for which this message is a reply, 0 if it is not a reply</param>
      /// <param name="storeMsg">true to store this message for resending on application restart</param>
      /// <param name="isArchived">true to set the isArchived flag</param>
      /// <returns>The msg that was sent</returns>
      public Header SendCommonMessage(MsgType msgType, IMessage msgToSend, int topic = 0
            , int destClientType = 0, int destClientID = 0
            , int replyMsgKey = 0, bool storeMsg = false, bool isArchived = false)
      {
         if (Context != null && Context.ClientMsgComm != null)
            return Context.ClientMsgComm.SendCommonMessage(Context, msgType, msgToSend, topic, destClientType, destClientID, replyMsgKey, storeMsg, isArchived);
         return null;
      }

      /// <summary>
      /// Sends a common message and waits for a response. Returns the actual message sent
      /// </summary>
      /// <param name="receivedMsg">The message response received, null if it was not received before maxWaitTimeMS</param>
      /// <param name="msgType">type of message</param>
      /// <param name="msgToSend">sub message to send</param>
      /// <param name="topic">the topic of this message</param>
      /// <param name="destClientType">specific client type to send it to, 0 to broadcast</param>
      /// <param name="destClientID">specific client ID to send it to, 0 for all</param>
      /// <param name="replyMsgKey">the key of the message for which this message is a reply, 0 if it is not a reply</param>
      /// <param name="maxWaitTimeMS">Maximum time to wait before timing out</param>
      /// <returns>The msg that was sent</returns> 
      public Header SendCommonMessageAndWait(out Header receivedMsg
            , MsgType msgType, Google.Protobuf.IMessage msgToSend, int topic
            , int destClientType, int destClientID
            , int replyMsgKey = 0, int maxWaitTimeMS = ClientComm.DEFAULT_WAITTIME)
      {
         if (Context != null && Context.ClientMsgComm != null)
            return Context.ClientMsgComm.SendCommonMessageAndWait(out receivedMsg, Context, msgType, msgToSend, topic, destClientType, destClientID, replyMsgKey, maxWaitTimeMS);
         receivedMsg = null;
         return null;
      }

      #endregion
   }
}
