using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using Matrix.MsgService.CommonMessages;

namespace Matrix.MsgService.CommunicationUtils
{
   /// <summary>
   /// Used for ease of unit testing
   /// </summary>
   public interface IConnectionHandler : IDisposable
   {
      #region Properties
      /// <summary>
      /// Host name of the server to connect
      /// </summary>
      string HostName { get; }
      /// <summary>
      /// Port of the server to connect
      /// </summary>
      int Port { get; }
      /// <summary>
      /// The tcp client Stream when it is connected
      /// </summary>
      NetworkStream Stream { get; }
      /// <summary>
      /// Last message received from the Messaging service
      /// </summary>
      DateTime LastMessageRx { get; }
      /// <summary>
      /// Last message sent to the Messaging service
      /// </summary>
      DateTime LastMessageSent { get; }
      #endregion
      /// <summary>
      /// True if it is currently connected
      /// </summary>
      bool IsConnected { get; }
      /// <summary>
      /// Connect to hostname/port
      /// </summary>
      /// <param name="ex">the exception when the connection fails</param>
      /// <returns>true if connection is successful</returns>
      bool Connect(out Exception ex);
      /// <summary>
      /// Checks that the connection is still valid
      /// </summary>
      /// <returns>true if it is still connecteed</returns>
      bool CheckConnection();
      /// <summary>
      /// Disconnects from the messaging service
      /// </summary>
      void Disconnect();
      /// <summary>
      /// Send heartbeat msg to the server
      /// </summary>
      /// <returns>true if it was able to send the message</returns>
      bool SendHeartbeatMessage(out Exception ex);
      /// <summary>
      /// Send msg to the server
      /// </summary>
      /// <param name="msg">the message to send</param>
      /// <returns>true if it was able to send the message</returns>
      bool SendMessage(Header msg, out Exception ex);
		/// <summary>
		/// Starts the repeating process of reading incoming messages
		/// </summary>
		void BeginReading();
      /// <summary>
      /// Event occurs when a message is received
      /// </summary>
      event EventHandler<ConnectionHandler.MessageDetails> MessageReceived;
   }

   /// <summary>
   /// Basic class for connecting to and communicating with the Messaging Service
   /// </summary>
   public class ConnectionHandler : IConnectionHandler, IDisposable
   {
      #region Fields
      private TcpClient _tcpClient = null;
      byte[] _heartbeatMsg = null;
      RxMsgBuffer _rxMsgBuffer = null;
      #endregion

      #region Properties
      /// <summary>
      /// Host name of the server to connect
      /// </summary>
      public string HostName { get; private set; }
      /// <summary>
      /// Port of the server to connect
      /// </summary>
      public int Port { get; private set; }
      /// <summary>
      /// The tcp client Stream when it is connected
      /// </summary>
      public NetworkStream Stream { get; private set; }
      /// <summary>
      /// Last message received from the Messaging service
      /// </summary>
      public DateTime LastMessageRx { get; private set; }
      /// <summary>
      /// Last message sent to the Messaging service
      /// </summary>
      public DateTime LastMessageSent { get; private set; }
      #endregion

      #region Constructors/Destructors
      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="hostname">host name for the connection</param>
      /// <param name="port">the host's port for the connection</param>
      public ConnectionHandler(string hostname, int port)
      {
         HostName = hostname;
         Port = port;
      }
      /// <summary>
      /// Disconnect and dispose
      /// </summary>
      public void Dispose()
      {
         Disconnect();
      }
      #endregion

      #region Connection
      /// <summary>
      /// True if it is currently connected
      /// </summary>
      public bool IsConnected { get { return _tcpClient != null && Stream != null && _tcpClient.Connected; } }
      /// <summary>
      /// Connect to hostname/port
      /// </summary>
      /// <param name="ex">the exception when the connection fails</param>
      /// <returns>true if connection is successful</returns>
      public bool Connect(out Exception ex)
      {
         ex = null;
         if (HostName == string.Empty)
         {
            ex = new Exception("HostName has not been set.");
            return false;
         }

         try
         {
            if (IsConnected)
               Disconnect();

            var tcpClient = new TcpClient(HostName, Port);
            _tcpClient = tcpClient;
            Stream = _tcpClient.GetStream();
            LastMessageSent = DateTime.Now;
            LastMessageRx = DateTime.Now;
            return true;
         }
         catch (Exception excpt)
         {
            ex = excpt;
         }
         return false;
      }
      /// <summary>
      /// Checks that the connection is still valid
      /// </summary>
      /// <returns>true if it is still connecteed</returns>
      public bool CheckConnection()
      {
         if (IsConnected)
         {
            _tcpClient.Client.Blocking = false;
            SocketError se;
            var cnt = _tcpClient.Client.Receive(new byte[1], 0, 0, SocketFlags.Peek, out se);
            _tcpClient.Client.Blocking = true;
            if (_tcpClient.Client.Connected && (se == SocketError.WouldBlock || se == SocketError.Success))
            {
               return true;
            }
         }
         return false;
      }
      /// <summary>
      /// Disconnects from the messaging service
      /// </summary>
      public void Disconnect()
      {
         var tcpClient = _tcpClient;
         Stream = null;
         _tcpClient = null;
         if (tcpClient != null)
         {
            tcpClient.Close();
            tcpClient.Dispose();
         }
      }
      #endregion

      const int HDR_SIZE = 4;

      /// <summary>
      /// Class returned when a message is received
      /// </summary>
      public class MessageDetails : EventArgs
      {
         /// <summary>
         /// Any error that occurred during the receipt of the message 
         /// </summary>
         public Exception Error { get; private set; }
         /// <summary>
         /// The message received
         /// </summary>
         public Header Message { get; private set; }
         /// <summary>
         /// Constructor
         /// </summary>
         /// <param name="msg">the message</param>
         /// <param name="ex">the error</param>
         public MessageDetails(Header msg, Exception ex = null)
         {
            Message = msg;
            Error = ex;
         }
      }
      #region Reading
      private class RxMsgBuffer
      {
         public Exception Error { get; internal set; }
         public Header Message { get; private set; }
         public byte[] _dataBuffer;
         public byte[] _sizeBuffer;
         public int _bytesReceived;
         public RxMsgBuffer()
         {
            _sizeBuffer = new byte[HDR_SIZE];
         }
         public bool HaveSize { get { return _dataBuffer != null; } }
         public int GetSize(int numberOfBytesRead)
         {
            Error = null;
            Message = null;
            int length = -1;
            _bytesReceived += numberOfBytesRead;
            if (_bytesReceived >= _sizeBuffer.Length)
            {
               // We've gotten the length buffer
               try
               {
						length = System.BitConverter.ToInt32(_sizeBuffer, 0);

                  // Sanity check for length < 0
                  //  This check will catch 50% of transmission errors that make it past both the IP and Ethernet checksums
                  //if (!cod.IsAtEnd && length >= 0)
                  // Zero-length packets are allowed as keepalives
                  if (length == 0)
                  {
                     _bytesReceived = 0;
                  }
                  else if (length > 0)
                  {
                     // Create the data buffer and start reading into it
                     _dataBuffer = new byte[length];
                     _bytesReceived = 0;
                  }
					}
               catch(Exception ex)
               {
                  Error = ex;
                  return -1;
               }
            }
            return length;
         }

			public Header GetMessage(int numberOfBytesRead)
			{
				Message = null;
				try
				{
					_bytesReceived += numberOfBytesRead;
					if (_bytesReceived == _dataBuffer.Length)
					{
						// We've gotten an entire packet
						Message = Header.Parser.ParseFrom(_dataBuffer);

						// Start reading the length buffer again
						_dataBuffer = null;
						_bytesReceived = 0;
					}
				}
				catch (Exception ex)
				{
					Error = ex;
				}
				return Message;
			}
		}

		/// <summary>
		/// Starts the repeating process of reading incoming messages
		/// </summary>
		public void BeginReading()
      {
         _rxMsgBuffer = new RxMsgBuffer();
         if (IsConnected )
         {
            ContinueReading(_rxMsgBuffer);
         }
      }

      private void ContinueReading(RxMsgBuffer rxMsgBuffer)
      {
         if (IsConnected && rxMsgBuffer != null)
         {
            if (!rxMsgBuffer.HaveSize)
            {
               Stream.BeginRead(
                     rxMsgBuffer._sizeBuffer, rxMsgBuffer._bytesReceived, rxMsgBuffer._sizeBuffer.Length - rxMsgBuffer._bytesReceived,
                     new AsyncCallback(ReadComplete), rxMsgBuffer);
            }
            else
               Stream.BeginRead(
                     rxMsgBuffer._dataBuffer, rxMsgBuffer._bytesReceived, rxMsgBuffer._dataBuffer.Length - rxMsgBuffer._bytesReceived,
                     new AsyncCallback(ReadComplete), rxMsgBuffer);
         }
      }
      private void ReadComplete(IAsyncResult ar)
      {
         if (Stream != null)
         {
            LastMessageRx = DateTime.Now;
            var rxMsgBuffer = ar.AsyncState as RxMsgBuffer;
            try
            {
               var numberOfBytesRead = Stream.EndRead(ar);

               if (numberOfBytesRead == 0)
               {
                  // disconnected??
                  //Raise MessageArrived??
                  return;
               }
               if (!rxMsgBuffer.HaveSize)
               {
                  rxMsgBuffer.GetSize(numberOfBytesRead);
               }
               else
               {
                  rxMsgBuffer.GetMessage(numberOfBytesRead);
               }
            }
            catch (Exception ex)
            {
               rxMsgBuffer.Error = ex;
               return;
            }
            //The message may get overwritten in ContinueReading
            //so get copy of the message before calling
            Header msg = null;
            Exception err = rxMsgBuffer.Error;
            if (rxMsgBuffer.Message != null)
               msg = new Header(rxMsgBuffer.Message);
            ContinueReading(rxMsgBuffer);
            OnMessageReceived(msg, err);
         }
      }

      #region MessageReceived event
      /// <summary>
      /// Event occurs when a message is received
      /// </summary>
      public event EventHandler<MessageDetails> MessageReceived;
      private void OnMessageReceived(Header msg, Exception err)
      {
         MessageReceived?.Invoke(this, new MessageDetails(msg, err));
      }
      #endregion
      #endregion

      #region Writing
		/// <summary>
		/// Send msg to the server
		/// </summary>
		/// <param name="msg">the message to send</param>
		/// <returns>true if it was able to send the message</returns>
		public bool SendMessage(Header msg, out Exception ex)
		{
			if (IsConnected)
			{
				using (var ms = new System.IO.MemoryStream())
				{
					msg.WriteTo(ms);
					var buffer = ms.ToArray();
					var length = buffer.Length;

					if (length > 0)
					{
						var lenBytes = System.BitConverter.GetBytes(length);
						return SendBuffer(lenBytes.Concat(buffer).ToArray(), out ex);
					}
				}
			}
			ex = null;
			return false;
		}
		/// <summary>
		/// Send heartbeat msg to the server
		/// </summary>
		/// <returns>true if it was able to send the message</returns>
		public bool SendHeartbeatMessage(out Exception ex)
      {
         if (IsConnected)
         {
            if (_heartbeatMsg == null)
            {
               _heartbeatMsg = new byte[HDR_SIZE];
               var cos = new CodedOutputStream(_heartbeatMsg);
               cos.WriteFixed32((uint)0);
               cos.Dispose();
            }
            return SendBuffer(_heartbeatMsg, out ex);
         }
         ex = null;
         return false;
      }
      private bool SendBuffer(byte[] buffer, out Exception ex)
      {
         bool sent = false;
         ex = null;
         int size = buffer.Length;
         if (size > 0)
         {
            try
            {
               if (Stream.CanWrite)
               {
                  LastMessageSent = DateTime.Now;
                  Stream.Write(buffer, 0, buffer.Length);
                  sent = true;
               }
            }
            catch (Exception excpn)
            {
               ex = excpn;
            }
         }
         return sent;
      }
      #endregion

   }
}
