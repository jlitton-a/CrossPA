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
   /// Delegate used for MessageReceived action
   /// </summary>
   /// <param name="rxMessage">the message that was received</param>
   /// <param name="forSentMsg">the message for which rxMessage was a reply</param>
   public delegate void MsgRxDelegate(Header rxMessage, Header forSentMsg);

   /// <summary>
   /// Interface for ClientContext - The context to distinguish different parts of an application that communicates with the message service
   /// </summary>
   public interface IClientContext : IDisposable
   {
      /// <summary>
      /// Function that will be called when an Ack is received
      /// the parameter will be the message for which the ack was received
      /// </summary>
      Action<Header> AckReceived { get; set; }
      /// <summary>
      /// Function that will be called when a message is received
      /// The first parameter will be the message received, 
      /// if the message is a reply, the second will be the message for which it is a reply
      /// </summary>
      MsgRxDelegate MessageReceived { get; set; }
      /// <summary>
      /// unique ID for this Context
      /// </summary>
      Guid ContextID { get; }
      /// <summary>
      /// Provides access to ClientComm for this context
      /// </summary>
      IClientComm ClientMsgComm { get; }
   }
   /// <summary>
   /// The context to distinguish different parts of an application that communicates with the message service
   /// </summary>
   internal class ClientContext : IClientContext
   {
      #region Types, classes, etc.
      #endregion

      #region fields
      #endregion

      #region Constructors/Destructors
      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="contextID">ID for this context</param>
      /// <param name="clientComm">the ClientComm for this context</param>
      internal ClientContext(Guid contextID, IClientComm clientComm)
      {
         ContextID = contextID;
         ClientMsgComm = clientComm;
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
               if(ClientMsgComm != null)
               {
                  ClientMsgComm.RemoveClientContext(this);
                  ClientMsgComm = null;
               }
            }

            disposedValue = true;
         }
      }
      /// <summary>
      /// Dispose
      /// </summary>
      public void Dispose()
      {
         // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
         Dispose(true);
      }
      #endregion

      #region events
      /// <summary>
      /// Function that will be called when an Ack is received
      /// the parameter will be the message for which the ack was received
      /// </summary>
      public Action<Header> AckReceived { get; set; }
      /// <summary>
      /// Function that will be called when a message is received
      /// The first parameter will be the message received, the second will be the message for which it is a a reply or ack
      /// </summary>
      public MsgRxDelegate MessageReceived { get; set; }
      #endregion

      #region Properties
      /// <summary>
      /// unique ID for this Context
      /// </summary>
      public Guid ContextID { get; private set; }
      /// <summary>
      /// Provides access to ClientComm for this context
      /// </summary>
      public IClientComm ClientMsgComm { get; private set; }

      #endregion

   }
}
