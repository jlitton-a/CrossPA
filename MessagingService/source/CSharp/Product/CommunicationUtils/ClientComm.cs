using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Google.Protobuf;
using System.Threading.Tasks;
using Matrix.MsgService.CommonMessages;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Matrix.MsgService.CommunicationUtils.Test")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace Matrix.MsgService.CommunicationUtils
{
   /// <summary>
   /// Used for ease of unit testing
   /// </summary>
   public interface IClientComm : IDisposable
   {
      /// <summary>
      /// Event occurs when the Connection status changes
      /// </summary>
      event EventHandler<ClientComm.ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
      /// <summary>
      /// Event occurs when IsConnected changes
      /// </summary>
      event EventHandler<bool> ConnectChanged;
      /// <summary>
      /// Event occurs when a message is received
      /// </summary>
      event EventHandler<Header> MessageReceived;

      /// <summary>
      /// True if socket state is connected
      /// </summary>
      bool IsConnected { get; }
      /// <summary>
      /// Client Context for messages that do not have a context
      /// </summary>
      IClientContext NoClientContext{ get; }
      /// <summary>
      /// The connected state of the socket.
      /// </summary>
      ClientComm.SocketState ClientSocketState { get; }
      /// <summary>
      /// Change the connection parameters.  Reconnects if it was previously connected
      /// </summary>
      /// <param name="hostname">Host name or ip address</param>
      /// <param name="port">port on which to communicate</param>
      /// <param name="logonMsg">logon msg to automatically logon and send subscriptions</param>
      /// <returns></returns>
      bool ChangeConnection(string hostname, int port, Logon logonMsg = null);
      /// <summary>
      /// Connect to the server and send logonMsg and subscriptions once connected
      /// </summary>
      bool Connect();
      /// <summary>
      /// Disconnect from the server
      /// </summary>
      void Disconnect();
      /// <summary>
      /// Add a subscription.  These will be automatically sent when logon message is sent and ACKed
      /// </summary>
      /// <param name="clientType">type of client to which to subscribe</param>
      /// <param name="clientID">ID of client to which to subscribe</param>
      /// <param name="topic">topic to which to subscribe</param>
      /// <param name="subscribe">true to subscribe, false to unsubscribe</param>
      /// <returns></returns>
      bool AddSubscribe(int clientType, int clientID, int topic, bool subscribe);
      /// <summary>
      /// Returns true if client is online
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <returns>online status of client</returns>
      ClientComm.OnlineStatus IsClientOnline(int clientType, int clientID);
      /// <summary>
      /// Add a new client context for sending and receiving messages
      /// </summary>
      /// <returns>the client context added</returns>
      IClientContext AddClientContext();
      /// <summary>
      /// Remove a client context-Removes all pending sent messages that have not yet been acked
      /// </summary>
      /// <param name="clientContext">the client context to remove</param>
      /// <returns>true if it was in the list</returns>
      bool RemoveClientContext(IClientContext clientContext);

      /// <summary>
      /// Send an acknowledgement that msgToAck was received and processed successfully
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
      void SendNackMessage(Header msgToNack, int reason=0, string details="");
      /// <summary>
      /// Sends a common message, returns the MsgKey used
      /// </summary>
      /// <param name="clientContext">The client context that is sending the message</param>
      /// <param name="msgType">type of message</param>
      /// <param name="msgToSend">sub message to send</param>
      /// <param name="topic">the topic of this message</param>
      /// <param name="destClientType">specific client type to send it to, 0 to broadcast</param>
      /// <param name="destClientID">specific client ID to send it to, 0 for all</param>
      /// <param name="replyMsgKey">the key of the message for which this message is a reply, 0 if it is not a reply</param>
      /// <param name="storeMsg">true to store this message for resending on application restart</param>
      /// <param name="isArchived">true to set the isArchived flag</param>
      /// <returns>The msg that was sent</returns>
      Header SendCommonMessage(IClientContext clientContext
            , MsgType msgType, Google.Protobuf.IMessage msgToSend, int topic = 0
            , int destClientType = 0, int destClientID = 0
            , int replyMsgKey = 0, bool storeMsg = false, bool isArchived = false);
      /// <summary>
      /// Sends a common message and waits for a response. Returns the actual message sent
      /// </summary>
      /// <param name="receivedMsg">The message response received, null if it was not received before maxWaitTimeMS</param>
      /// <param name="clientContext">The client context that is sending the message</param>
      /// <param name="msgType">type of message</param>
      /// <param name="msgToSend">sub message to send</param>
      /// <param name="topic">the topic of this message</param>
      /// <param name="destClientType">specific client type to send it to, 0 to broadcast</param>
      /// <param name="destClientID">specific client ID to send it to, 0 for all</param>
      /// <param name="replyMsgKey">the key of the message for which this message is a reply, 0 if it is not a reply</param>
      /// <param name="maxWaitTimeMS">Maximum time to wait before timing out</param>
      /// <returns>The msg that was sent</returns> 
      Header SendCommonMessageAndWait(out Header receivedMsg, IClientContext clientContext
            , MsgType msgType, Google.Protobuf.IMessage msgToSend, int topic
            , int destClientType, int destClientID
            , int replyMsgKey = 0
            , int maxWaitTimeMS = ClientComm.DEFAULT_WAITTIME);
      /// <summary>
      /// Return true if this msg needs to be Acked (e.g. it needs to be tracked and resent until acked)
      /// </summary>
      /// <param name="sentMsg">the message to check</param>
      /// <returns>true if the message needs to be acked and resent until acked</returns>
      bool NeedToAckMsg(Header sentMsg);
   }
   /// <summary>
   /// Class for connecting to and communicating with the Messaging Service.  
   /// Will send a periodic heartbeat and will automatically attempt to reconnect when connection is lost.
   /// If a logon msg is passed in, it will automatically send it and any subscriptions once connected.
   /// </summary>
   public class ClientComm : IClientComm
	{
		#region enums, types and const
      /// <summary>
      /// Status of connection
      /// </summary>
      public enum OnlineStatus
      {
         /// <summary>
         /// Don't know the status
         /// </summary>
         Unknown,
         /// <summary>
         /// it is online
         /// </summary>
         IsOnline,
         /// <summary>
         /// it is not online
         /// </summary>
         NotOnline
      }
		/// <summary>
		/// The connected state of the socket.
		/// </summary>
		public enum SocketState
		{
			/// <summary>
			/// The socket is disconnected; we are not trying to connect.
			/// </summary>
			Disconnected,
			/// <summary>
			/// The socket is attempting to connect.
			/// </summary>
			Connecting,
			/// <summary>
			/// The socket is connected.
			/// </summary>
			Connected,
			/// <summary>
			/// The socket is attempting to disconnect.
			/// </summary>
			Disconnecting,
			/// <summary>
			/// The socket is disconnected. An attempt will be made to retry connection
			/// </summary>
			RetryConnect,
		}
		/// <summary>
		/// The reason why the socket is disconnected
		/// </summary>
		public enum DisconnectReason
		{
			/// <summary>
			/// Server is connected or starting to connect
			/// </summary>
			None,
			/// <summary>
			/// It was manually disconnected
			/// </summary>
			Manual,
			/// <summary>
			/// Initial attempt to connect failed
			/// </summary>
			CouldNotConnect,
			/// <summary>
			/// Server connection was lost
			/// </summary>
			ServerDisconnected,
			/// <summary>
			/// Server is connected but it is not responding
			/// </summary>
			ServerNotResponding,
			/// <summary>
			/// An exception occurred during read/write
			/// </summary>
			Exception,
		}
		/// <summary>
		/// Class to provide an ability to execute a task after a delay, and to cancel the taks
		/// NOTE: see https://stackoverflow.com/questions/6960520/when-to-dispose-cancellationtokensource
		/// for information about disposing and CancellationTokenSource
		/// </summary>
		public class PauseAndExecuter : IDisposable
		{
			private CancellationTokenSource _cancelReconnect = null;

			/// <summary>
			/// Standard constructor
			/// </summary>
			public PauseAndExecuter()
			{
			}
			/// <summary>
			/// Dispose 
			/// </summary>
			public void Dispose()
			{
				if (_cancelReconnect != null)
				{
					_cancelReconnect.Dispose();
					_cancelReconnect = null;
				}
			}
			/// <summary>
			/// Cancel the execution, if it was started
			/// </summary>
			public void Cancel()
			{
				if (_cancelReconnect != null)
				{
					_cancelReconnect.Cancel();
					_cancelReconnect = null;
				}
			}
			/// <summary>
			/// Execute action after a delay of timeoutInMilliseconds
			/// </summary>
			/// <param name="action"></param>
			/// <param name="timeoutInMilliseconds"></param>
			/// <returns>the task</returns>
			public async Task ExecuteAsync(Action<CancellationToken> action, int timeoutInMilliseconds)
			{
				_cancelReconnect = new CancellationTokenSource();
				var cancelToken = _cancelReconnect.Token;
				await Task.Delay(timeoutInMilliseconds, cancelToken);
				action(cancelToken);
			}
		}
      /// <summary>
      /// Used to signal between SendCommonMessageAndWait and OnMessageReceive
      /// </summary>
      private class WaitResponse
      {
         public ManualResetEvent AckEvent { get; private set; }
         public Header MsgReceived { get; set; }
         public WaitResponse()
         {
            AckEvent = new ManualResetEvent(false);
         }
      }

      /// <summary>
      /// Default port if it is not specified
      /// </summary>
      public const int DEFAULT_PORT = 8888;
      /// <summary>
      /// Default maximum time to wait before timing out
      /// </summary>
      public const int DEFAULT_WAITTIME = 5000;
		#endregion

		#region fields
		private object _lock;
		IConnectionHandler _connectionHandler = null;
		private Header _logonMsg;
		int _msgKey;
		HashSet<Subscribe> _subscriptions;
      bool _needLoadMsgStore ;
      IMsgStore _msgStore;
      private volatile bool _isLoggedOn = false;
		/// <summary>
		/// The timer used to send keep alive messages to the server
		/// </summary>
		private Timer _heartbeatTimer;
		/// <summary>
		/// Number of milliseconds between when keep alive messages should be sent
		/// </summary>
		private uint _heartbeatTimeMS;
		/// <summary>
		/// The timer used to determine if we have heard anything from the Server in the timeout time
		/// </summary>
      private Utilities.IThreadingTimer _serverTimeOutTimer = null;
		/// <summary>
		/// Number of milliseconds to consider the server has timed out
		/// </summary>
		private uint _serverTimeOutTimeMS;
		/// <summary>
		/// Number of milliseconds between connect retries when connection is lost
		/// </summary>
		private uint _reconnectRetryTimeMS;
		volatile bool _stopped;
		PauseAndExecuter _reconnectRetryTask = null;

      /// <summary>
      /// holds reset events and received message for responding to SendCommonMessageAndWait
      /// </summary>
      private System.Collections.Concurrent.ConcurrentDictionary<int, WaitResponse> _ackRxEvents = new System.Collections.Concurrent.ConcurrentDictionary<int, WaitResponse>();

      ISubscriberMessageLists _subscriberMsgList = null;
      /// <summary>
      /// The timer used to resend messages that have not yet been acked
      /// </summary>
      private Utilities.IThreadingTimer _resendMsgTimer = null;
      uint _resendMessagesTimeMS;
      volatile bool _resendPaused = false;
      #endregion

      #region Constructors/Destructors
      /// <summary>
      /// Constructor for IP address and host
      /// </summary>
      /// <param name="name">name to use for logging</param>
      /// <param name="hostname">host name/IP Address for connection to the message service</param>
      /// <param name="port">port to use for connection to the message service</param>
      /// <param name="logonMsg">The Logon message to send after connecting</param>
      /// <param name="msgStore">Used to store messages that have not yet been acked for reapplying on restart</param>
      /// <param name="reconnectRetryTimeMS">Number of milliseconds between connect retries when connection is lost; 0 to not retry</param>
      /// <param name="heartbeatTimeMS">Number of milliseconds between when keep alive messages should be sent; 0 to not send heartbeats</param>
      /// <param name="serverTimeOutTimeMS">Number of milliseconds to consider the server has timed out; 0 to not check</param>
      /// <param name="resendMessagesTimeMS">Number of milliseconds between resending messages that have not been acked</param>
      public ClientComm(string name,
				string hostname, int port = DEFAULT_PORT,
				Logon logonMsg = null,
            IMsgStore msgStore = null,
				uint reconnectRetryTimeMS = 0,
				uint heartbeatTimeMS = 2000,
				uint serverTimeOutTimeMS = 4000,
            uint resendMessagesTimeMS = 2000
            )
         : this(name, new ConnectionHandler(hostname, port), null, null, null, logonMsg, msgStore, reconnectRetryTimeMS, heartbeatTimeMS, serverTimeOutTimeMS, resendMessagesTimeMS)
      {}
      /// <summary>
      /// Constructor for ConnectionHandler
      /// </summary>
      /// <param name="name">name to use for logging</param>
      /// <param name="connectionHandler">The ConnectionHandler to use; NOTE: it will be disposed when done</param>
      /// <param name="logonMsg">The Logon message to send after connecting</param>
      /// <param name="msgStore">Used to store messages that have not yet been acked for reapplying on restart</param>
      /// <param name="reconnectRetryTimeMS">Number of milliseconds between connect retries when connection is lost; 0 to not retry</param>
      /// <param name="heartbeatTimeMS">Number of milliseconds between when keep alive messages should be sent; 0 to not send heartbeats</param>
      /// <param name="serverTimeOutTimeMS">Number of milliseconds to consider the server has timed out; 0 to not check</param>
      /// <param name="resendMessagesTimeMS">Number of milliseconds between resending messages that have not been acked</param>
      public ClientComm(string name,
				IConnectionHandler connectionHandler,
				Logon logonMsg = null,
            IMsgStore msgStore = null,
            uint reconnectRetryTimeMS = 0,
				uint heartbeatTimeMS = 2000,
				uint serverTimeOutTimeMS = 4000,
            uint resendMessagesTimeMS = 2000
            )
         : this(name, connectionHandler, null, null, null, logonMsg, msgStore, reconnectRetryTimeMS, heartbeatTimeMS, serverTimeOutTimeMS, resendMessagesTimeMS)
      {}
      /// <summary>
      /// Constructor for ConnectionHandler
      /// </summary>
      /// <param name="name">name to use for logging</param>
      /// <param name="connectionHandler">The ConnectionHandler to use; NOTE: it will be disposed when done</param>
      /// <param name="subscriberMsgList">used to track sent messages when trackSentMessages is true</param>
      /// <param name="serverTimeOutTimer">Timer to use for server timeouts (for unit testing)</param>
      /// <param name="resendMsgTimer">Timer to use for resending messages (for unit testing)</param>
      /// <param name="logonMsg">The Logon message to send after connecting</param>
      /// <param name="msgStore">Used to store messages that have not yet been acked for reapplying on restart</param>
      /// <param name="reconnectRetryTimeMS">Number of milliseconds between connect retries when connection is lost; 0 to not retry</param>
      /// <param name="heartbeatTimeMS">Number of milliseconds between when keep alive messages should be sent; 0 to not send heartbeats</param>
      /// <param name="serverTimeOutTimeMS">Number of milliseconds to consider the server has timed out; 0 to not check</param>
      /// <param name="resendMessagesTimeMS">Number of milliseconds between resending messages that have not been acked</param>
      internal ClientComm(string name, 
            IConnectionHandler connectionHandler,
            ISubscriberMessageLists subscriberMsgList,
            Utilities.IThreadingTimer serverTimeOutTimer,
            Utilities.IThreadingTimer resendMsgTimer,
            Logon logonMsg = null,
            IMsgStore msgStore = null,
            uint reconnectRetryTimeMS = 0,
            uint heartbeatTimeMS = 1000,
            uint serverTimeOutTimeMS = 4000,
            uint resendMessagesTimeMS = 2000
            ) 
		{
         _resendMessagesTimeMS = resendMessagesTimeMS;
         _subscriberMsgList = subscriberMsgList ?? new SubscriberMessageLists();
         if (resendMessagesTimeMS > 0)
         {
            _resendMsgTimer = resendMsgTimer ?? new Matrix.Utilities.ThreadingTimer();
            _resendMsgTimer.Tick += _resendMsgTimer_Tick;
         }
         ClientSocketState = SocketState.Disconnected;
			Name = name;
			_msgKey = 0;
			_subscriptions = new HashSet<Subscribe>();
			if (logonMsg != null)
			{
				_logonMsg = new Header();
				_logonMsg.MsgKey = Interlocked.Increment(ref _msgKey);
				_logonMsg.MsgTypeID = MsgType.Logon;
            _logonMsg.Msg = ((IMessage)logonMsg).ToByteString();
			}
			else
				_logonMsg = null;
			_connectionHandler = connectionHandler;
			if (_connectionHandler != null)
				_connectionHandler.MessageReceived += _connectionHandler_MessageReceived;

			_lock = new object();

			_serverTimeOutTimeMS = serverTimeOutTimeMS;
			if (_serverTimeOutTimeMS > 0)
         {
            _serverTimeOutTimer = serverTimeOutTimer ?? new Utilities.ThreadingTimer();
            _serverTimeOutTimer.Tick += _serverTimeoutTimer_Tick;
         }

			_heartbeatTimeMS = heartbeatTimeMS;
			if (_heartbeatTimeMS > 0)
				_heartbeatTimer = new Timer(new TimerCallback(HeartbeatTimer_Timer));

			_stopped = false;
			_reconnectRetryTimeMS = reconnectRetryTimeMS;
         _needLoadMsgStore = true;
         _msgStore = msgStore;

         NoClientContext = AddClientContext();
      }

      /// <summary>
      /// Disconnect and dispose timers and tasks
      /// </summary>
      public void Dispose()
		{
         if (_subscriberMsgList != null)
         {
            _subscriberMsgList.Clear();
         }

         if (ClientSocketState == SocketState.Connected)
				Disconnect();

			if (_heartbeatTimer != null)
				_heartbeatTimer.Dispose();
			if (_serverTimeOutTimer != null)
         {
				_serverTimeOutTimer.Dispose();
            _serverTimeOutTimer = null;
         }
         if (_resendMsgTimer != null)
         {
            _resendMsgTimer.Dispose();
            _resendMsgTimer = null;
         }
         if (_reconnectRetryTask != null)
			{
				_reconnectRetryTask.Dispose();
				_reconnectRetryTask = null;
			}
		}
		#endregion

      #region ConnectionStatusChanged event
		/// <summary>
		/// Arguments for the ConnectionStatusChanged event
		/// </summary>
		public class ConnectionStatusChangedEventArgs
		{
         /// <summary>
         /// Constructor
         /// </summary>
         /// <param name="state">the current connection state of the socket</param>
         /// <param name="reason">When the state is disconnected, the reason</param>
         /// <param name="details">When the state is disconnected, the details</param>
         public ConnectionStatusChangedEventArgs(SocketState state, DisconnectReason reason, string details)
			{
				ClientSocketState = state;
				Reason = reason;
				Details = details;
			}
			/// <summary>
			/// Current connection state
			/// </summary>
			public SocketState ClientSocketState { get; private set; }
			/// <summary>
			/// If it is disconnected, reason why it is disconnected
			/// </summary>
			public DisconnectReason Reason { get; private set; }
			/// <summary>
			/// Additional details
			/// </summary>
			public string Details { get; private set; }
		}
		/// <summary>
		/// Event occurs when the Connection status changes
		/// </summary>
		public event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
		private void OnConnectionStatusChanged(SocketState state, DisconnectReason reason, string details = "")
		{
			ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(state, reason, details));
		}
		#endregion

		#region ConnectChanged event
		/// <summary>
		/// Event occurs when IsConnected changes
		/// </summary>
		public event EventHandler<bool> ConnectChanged;
      /// <summary>
      /// call to raise the ConnectChanged event
      /// </summary>
      /// <param name="isConnected"></param>
		protected virtual void OnConnectChanged(bool isConnected)
		{
			ConnectChanged?.Invoke(this, isConnected);
		}
		#endregion

		#region LogonComplete event
		/// <summary>
		/// Event occurs after logon
		/// </summary>
		public event EventHandler LogonComplete;
      /// <summary>
      /// Call to raise the LogonComplete event
      /// </summary>
      protected virtual void OnLogonComplete()
      {
         //only do this once on startup
         //load messages that need to be sent, send them, then add them to the store
         //to be removed by an ack
         IMsgStoreRecordList msgStoreList = null;
//         lock (_lock)
         {
            if (_needLoadMsgStore && _msgStore != null)
            {
               _needLoadMsgStore = false;
               msgStoreList = _msgStore.GetMessages();
            }
         } 
         if (msgStoreList != null)
         {
            _resendPaused = true;
            foreach (var msgStoreRecord in msgStoreList)
            {
               if (msgStoreRecord.HdrMsg != null)
               {
                  msgStoreRecord.HdrMsg.MsgKey = Interlocked.Increment(ref _msgKey);
                  msgStoreRecord.HdrMsg.IsArchived = true;
//                  lock (_lock)
                  {
                     _msgStore.AddMsgStoreRecord(msgStoreRecord, msgStoreRecord.HdrMsg.MsgKey);
                  }
                  if (_subscriberMsgList != null && NeedToAckMsg(msgStoreRecord.HdrMsg))
                  {
                     _subscriberMsgList.AddSentMessage(msgStoreRecord.HdrMsg, null);
                  }
                  SendMessage(msgStoreRecord.HdrMsg);
               }
            }
            _resendPaused = false;
         }
         LogonComplete?.Invoke(this, new EventArgs());
		}
      #endregion

      #region Subscribed event
      /// <summary>
      /// Arguments for the Subscribed event
      /// </summary>
      public class SubscribedEventArgs : EventArgs
		{
         /// <summary>
         /// the type of client subscribing
         /// </summary>
			public CommonMessages.ClientTypes ClientType { get; set; }
         /// <summary>
         /// The topic that the client is subscribing to
         /// </summary>
			public int Topic { get; set; }
		}
	
		/// <summary>
		/// Event occurs after subscribe
		/// </summary>
		public event EventHandler<SubscribedEventArgs> Subscribed;
      /// <summary>
      /// Call to raise the Subscribed event
      /// </summary>
      /// <param name="args"></param>
		protected virtual void OnSubscribed(SubscribedEventArgs args)
		{
			Subscribed?.Invoke(this, args);
		}
		#endregion

		#region MessageReceived event
		/// <summary>
		/// Event occurs when a message is received
		/// </summary>
		public event EventHandler<Header> MessageReceived;
      /// <summary>
      /// Call to raise the MessageReceived event
      /// </summary>
      /// <param name="hdr"></param>
      protected virtual void OnMessageReceived(Header hdr)
      {
         MessageReceived?.Invoke(this, hdr);
      }
      #endregion

      #region Properties
      /// <summary>
      /// Name of the client - mostly used for logging
      /// </summary>
      public string Name { get; private set; }
      /// <summary>
      /// true when connected
      /// </summary>
      public bool IsConnected
      {
         get
         {
            return ClientSocketState == SocketState.Connected;
         }
      }
      /// <summary>
      /// The connected state of the socket. If this is SocketState.Disconnected, then ClientSocket is null.
      /// </summary>
      public SocketState ClientSocketState
      {
         get;
         private set;
      }

      /// <summary>
      /// Client Context for messages that do not have a context
      /// </summary>
      public IClientContext NoClientContext { get; private set; }
      #endregion

      #region Connect/Disconnect Methods
      /// <summary>
      /// Change the connection parameters.  Reconnects if it was previously connected
      /// </summary>
      /// <param name="hostname">Host name or ip address</param>
      /// <param name="port">port on which to communicate</param>
      /// <param name="logonMsg">logon msg to automatically logon and send subscriptions</param>
      /// <returns></returns>
      public bool ChangeConnection(string hostname, int port,
            Logon logonMsg = null)
      {
         bool wasConnected = IsConnected;
         Disconnect();
         lock (_lock)
         {
            _stopped = false;
            _isLoggedOn = false;
            if (_connectionHandler != null)
            {
               _connectionHandler.MessageReceived -= _connectionHandler_MessageReceived;
               _connectionHandler.Dispose();
               _connectionHandler = null;
            }
            _connectionHandler = new ConnectionHandler(hostname, port);
            if (_connectionHandler != null)
               _connectionHandler.MessageReceived += _connectionHandler_MessageReceived;

            if (logonMsg != null)
            {
               _logonMsg = new Header();
               _logonMsg.MsgKey = Interlocked.Increment(ref _msgKey);
               _logonMsg.MsgTypeID = MsgType.Logon;
               _logonMsg.Msg = ((IMessage)logonMsg).ToByteString();
            }
            else
               _logonMsg = null;
         }
         if (wasConnected)
            return ConnectInternal();
         else
            return true;
      }
      /// <summary>
      /// Connect to the server and send logonMsg and subscriptions once connected
      /// </summary>
      public bool Connect()
      {
         try
         {
            Disconnect();
            lock (_lock)
            {
               _stopped = false;
            }
            return ConnectInternal();
         }
         catch(Exception ex)
         {
            Matrix.Utilities.Diagnostics.MessageLogger.ErrorHandler("Error connecting to messaging service.", ex);
         }
         Disconnect(DisconnectReason.CouldNotConnect, "Error connecting");
         return false;
      }

      /// <summary>
      /// Connect to the server using the previously set hostname and port.
      /// Returns false if hostname is empty or it could not connect
      /// </summary>
      private bool ConnectInternal()
      {
         if (_connectionHandler == null)
            return false;

         Exception ex = null; 
         try
         {
            if (ClientSocketState == SocketState.Connected)
               Disconnect(DisconnectReason.None, "Reconnecting...");

            CheckConnectionChanged(SocketState.Connecting);
            bool connected = false;
            lock (_lock)
            {
               if (!_stopped)
               {
                  if (_connectionHandler == null)
                  {
                     Disconnect(DisconnectReason.CouldNotConnect);
                     return false; 
                  }
                  if (_connectionHandler.Connect(out ex))
                  {
                     if (_serverTimeOutTimer != null)
                        _serverTimeOutTimer.Change(_serverTimeOutTimeMS * 4, _serverTimeOutTimeMS);
                     connected = true;
                  }
               }
            }
            if (connected)
            {
               CheckConnectionChanged(SocketState.Connected, DisconnectReason.None, "Connected to " + _connectionHandler.HostName);
					BeginReading();
               if (_resendMsgTimer != null)
                  _resendMsgTimer.Change(_resendMessagesTimeMS, Timeout.Infinite);

               var logonMsg = _logonMsg;
					if (logonMsg != null)
					{
						SendMessage(logonMsg);
					}
										
					return true;
            }
         }
         catch (Exception excptn)
         {
            ex = excptn;
         }
         string details = "";
         var socketState = SocketState.Disconnected;
         if (ex != null)
         {
            details = ex.Message;
         }
         if (_reconnectRetryTimeMS > 0)
         {
            socketState = SocketState.RetryConnect;
         }
         CheckConnectionChanged(socketState, DisconnectReason.CouldNotConnect, details);
         if (socketState == SocketState.RetryConnect)
            StartReconnectTask();
         return false;
      }
      /// <summary>
      /// Disconnect from the server
      /// </summary>
      public void Disconnect()
      {
         Disconnect(DisconnectReason.Manual, "", true);
      }
      private void Disconnect(DisconnectReason reason, string details = "", bool stop = false)
      {
         if (ClientSocketState == SocketState.Disconnected)
            return;
         Matrix.Utilities.Diagnostics.MessageLogger.LogEvent(String.Format("Disconnecting from messenger {0} - {1}", reason, details), System.Diagnostics.EventLogEntryType.Information);
         CancelReconnectTask(stop);
         CheckConnectionChanged(SocketState.Disconnecting);
         try
         {
            lock (_lock)
            {
               _isLoggedOn = false;
               if (_heartbeatTimer != null)
                  _heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
               if (_serverTimeOutTimer != null)
                  _serverTimeOutTimer.Change(Timeout.Infinite, Timeout.Infinite);
               if (_resendMsgTimer != null)
                  _resendMsgTimer.Change(Timeout.Infinite, Timeout.Infinite);
               if (_connectionHandler != null)
               {
                  _connectionHandler.Disconnect();
               }
            }
         }
         finally
         {
            //If reason is not manual and reconnect timer is turned on, retry connecting
            if (_reconnectRetryTimeMS > 0 && reason != DisconnectReason.None && reason != DisconnectReason.Manual)
            {
               CheckConnectionChanged(SocketState.RetryConnect, reason, details);
               StartReconnectTask();
            }
            else
            {
               CheckConnectionChanged(SocketState.Disconnected, reason, details);
            }
         }
      }
      #endregion

      #region Messages
      /// <summary>
      /// Add a new client context for sending and receiving messages
      /// </summary>
      /// <returns>the client context added</returns>
      public IClientContext AddClientContext()
      {
         var newContextID = Guid.NewGuid();
         var clientContext = new ClientContext(newContextID, this);
         _subscriberMsgList.AddContext(clientContext);
         return clientContext;
      }
      /// <summary>
      /// Remove a client context-Removes all pending sent messages that have not yet been acked
      /// </summary>
      /// <param name="clientContext">the client context to remove</param>
      /// <returns>true if it was in the list</returns>
      public bool RemoveClientContext(IClientContext clientContext)
      {
         if(clientContext != null)
         {
            var msgList = _subscriberMsgList.RemoveContext(clientContext);
            if(msgList != null)
            { 
               foreach (var msg in msgList)
               {
                  WaitResponse waitResponse;
                  if (_ackRxEvents.TryGetValue(msg.MsgKey, out waitResponse))
                  {
                     waitResponse.MsgReceived = null;
                     waitResponse.AckEvent.Set();
                  }
               }
               return true;
            }
         }
         return false;
      }
      /// <summary>
      /// Add a subscription.  These will be automatically sent when logon message is sent and ACKed
      /// </summary>
      /// <param name="clientType">type of client to which to subscribe</param>
      /// <param name="clientID">ID of client to which to subscribe</param>
      /// <param name="topic">topic to which to subscribe</param>
      /// <param name="subscribe">true to subscribe, false to unsubscribe</param>
      /// <returns></returns>
      public bool AddSubscribe(int clientType, int clientID, int topic, bool subscribe)
      {
         bool changed = false;
         MsgType msgType;
         Subscribe msgToSend = new Subscribe();
         msgToSend.ClientType = (int)clientType;
         msgToSend.ClientID = clientID;
         msgToSend.Topic = topic;
         lock (_lock)
         {
            if (subscribe)
            {
               msgType = MsgType.Subscribe;
               changed = _subscriptions.Add(msgToSend);
            }
            else
            {
               msgType = MsgType.Unsubscribe;
               changed = _subscriptions.Remove(msgToSend);
            }
         }
         if (changed && IsConnected && _isLoggedOn)
         {
            SendCommonMessage(msgType, msgToSend);
         }
         return changed;
      }
      private void SendSubscribeMessages()
      {
         HashSet<Subscribe> subscriptions;
         lock (_lock)
         {
            subscriptions = new HashSet<Subscribe>(_subscriptions);
         }
         foreach (var subscribeMsg in subscriptions)
         {
            SendCommonMessage(MsgType.Subscribe, subscribeMsg);
				OnSubscribed(new SubscribedEventArgs() { ClientType = (ClientTypes)subscribeMsg.ClientType, Topic = subscribeMsg.Topic });
         }

      }
      /// <summary>
      /// Send an Ack for a message
      /// </summary>
      /// <param name="msgToAck">the message to be acked</param>
      public void SendAckMessage(Header msgToAck)
      {
         if (msgToAck == null)
            return;
         SendCommonMessageInternal(null, MsgType.Ack, null, msgToAck.MsgKey, 0, msgToAck.OrigClientType, msgToAck.OrigClientID, 0, false, false);
      }
      /// <summary>
      /// Send an acknowledgement that a message was received but was not processed.
      /// reason and details may provide information about why it was not processed
      /// </summary>
      /// <param name="msgToNack">the message to be acked</param>
      /// <param name="reason">client defined reason identifier for the Nack</param>
      /// <param name="details">details for the Nack</param>
      public void SendNackMessage(Header msgToNack, int reason=0, string details="")
      {
         if (msgToNack == null)
            return;
         var nackDetails = new CommonMessages.NackDetails();
         nackDetails.Details = details;
         nackDetails.Reason = reason;
         SendCommonMessageInternal(null, MsgType.Nack, nackDetails, msgToNack.MsgKey, 0, msgToNack.OrigClientType, msgToNack.OrigClientID, 0, false, false);
      }
      /// <summary>
      /// Sends a common message, returns the MsgKey used
      /// </summary>
      /// <param name="clientContext">The client context that is sending the message</param>
      /// <param name="msgType">type of message</param>
      /// <param name="msgToSend">sub message to send</param>
      /// <param name="topic">the topic of this message</param>
      /// <param name="destClientType">specific client type to send it to, 0 to broadcast</param>
      /// <param name="destClientID">specific client ID to send it to, 0 for all</param>
      /// <param name="replyMsgKey">the key of the message for which this message is a reply, 0 if it is not a reply</param>
      /// <param name="storeMsg">true to store this message for resending on application restart</param>
      /// <param name="isArchived">true to set the isArchived flag</param>
      /// <returns>The msg that was sent</returns>
      public Header SendCommonMessage(IClientContext clientContext
            , MsgType msgType, Google.Protobuf.IMessage msgToSend, int topic = 0
            , int destClientType = 0, int destClientID = 0
            , int replyMsgKey = 0, bool storeMsg = false, bool isArchived = false)
      {
         return SendCommonMessageInternal(clientContext, msgType, msgToSend, Interlocked.Increment(ref _msgKey), topic,
               destClientType, destClientID, replyMsgKey, storeMsg, isArchived);
      }
      private Header SendCommonMessage(MsgType msgType, Google.Protobuf.IMessage msgToSend, int topic = 0
            , int destClientType = 0, int destClientID = 0
            , int replyMsgKey = 0, bool storeMsg = false, bool isArchived = false)
      {
         return SendCommonMessage(null, msgType, msgToSend, topic, destClientType, destClientID, replyMsgKey, storeMsg, isArchived);
      }
      /// <summary>
      /// Sends a common message and waits for a response. Returns the actual message sent
      /// </summary>
      /// <param name="clientContext">the client context that is sending this message</param>
      /// <param name="receivedMsg">The message response received, null if it was not received before maxWaitTimeMS</param>
      /// <param name="msgType">type of message</param>
      /// <param name="msgToSend">sub message to send</param>
      /// <param name="topic">the topic of this message</param>
      /// <param name="destClientType">specific client type to send it to, 0 to broadcast</param>
      /// <param name="destClientID">specific client ID to send it to, 0 for all</param>
      /// <param name="replyMsgKey">the key of the message for which this message is a reply, 0 if it is not a reply</param>
      /// <param name="maxWaitTimeMS">Maximum time to wait before timing out</param>
      /// <returns>The msg that was sent</returns> 
      public Header SendCommonMessageAndWait(out Header receivedMsg, IClientContext clientContext
            , MsgType msgType, Google.Protobuf.IMessage msgToSend, int topic
            , int destClientType, int destClientID
            , int replyMsgKey = 0
            , int maxWaitTimeMS = ClientComm.DEFAULT_WAITTIME)
      {
         receivedMsg = null;
         var waitResponse = new WaitResponse();
         int msgKey = Interlocked.Increment(ref _msgKey);
         _ackRxEvents.TryAdd(msgKey, waitResponse);

         var hdr = SendCommonMessageInternal(clientContext, msgType, msgToSend, msgKey, topic, destClientType, destClientID, replyMsgKey, false, false);
         if (waitResponse.AckEvent.WaitOne(maxWaitTimeMS))
         {
            receivedMsg = waitResponse.MsgReceived;
         }
         if (_ackRxEvents.TryRemove(msgKey, out waitResponse))
         {
            if (waitResponse != null && waitResponse.AckEvent != null)
               waitResponse.AckEvent.Dispose();
         }
         return hdr;
      }

      /// <summary>
      /// Sends a common message, returns the MsgKey used
      /// </summary>
      /// <param name="clientContext">the client context that is sending this message</param>
      /// <param name="msgType">type of message</param>
      /// <param name="msgToSend">sub message to send</param>
      /// <param name="msgKey">the key to use</param>
      /// <param name="topic">the topic of this message</param>
      /// <param name="destClientType">specific client type to send it to, 0 to broadcast</param>
      /// <param name="destClientID">specific client ID to send it to, 0 for all</param>
      /// <param name="replyMsgKey">the key of the message for which this message is a reply, 0 if it is not a reply</param>
      /// <param name="storeMsg">true to store this message for resending on application restart</param>
      /// <param name="isArchived">true to set the isArchived flag</param>
      /// <returns>The msg key that was used</returns>
      private Header SendCommonMessageInternal(IClientContext clientContext
            , MsgType msgType, Google.Protobuf.IMessage msgToSend, int msgKey, int topic
            , int destClientType, int destClientID
            , int replyMsgKey, bool storeMsg, bool isArchived
            )
      {
         Header msg = new Header();
         msg.MsgKey = msgKey;
         msg.MsgTypeID = msgType;
         msg.Topic = topic;
         msg.DestClientType = destClientType;
         msg.DestClientID = destClientID;
         msg.IsArchived = isArchived;
         msg.ReplyMsgKey = replyMsgKey;
         if (msgToSend != null)
            msg.Msg = msgToSend.ToByteString();

         if (_subscriberMsgList != null && NeedToAckMsg(msg))
         {
            _subscriberMsgList.AddSentMessage(msg, clientContext);
         }
//         lock (_lock)
         {
            if (storeMsg && NeedToAckMsg(msg) && _msgStore != null)
               _msgStore.StoreNewMessage(msg, DateTime.Now);
         }
         SendMessage(msg);
         return msg;
      }
      /// <summary>
      /// Send msg to the server
      /// </summary>
      /// <param name="msg">the message to send</param>
      /// <returns>true if it was able to send the message</returns>
      private bool SendMessage(Header msg)
      {
         if (_connectionHandler != null && msg != null)
         {
            Matrix.Utilities.Diagnostics.MessageLogger.LogMessage(String.Format("Send {0} msg (key={1}) to client ({2},{3})"
               , msg.MsgTypeID, msg.MsgKey, msg.DestClientType, msg.DestClientID));
            Exception ex;
            List<int> ackKeyList = null;
            //if this message is for a specific client, add any acks to the message
            if (msg.DestClientType > 0)
            {
               if (_subscriberMsgList != null)
               {
                  ackKeyList = _subscriberMsgList.GetNeedToAckList(msg.DestClientType, msg.DestClientID);
                  if (ackKeyList != null)
                  {
                     foreach (var key in ackKeyList)
                        msg.AckKeys.Add(key);
                  }
               }
            }
            var success = _connectionHandler.SendMessage(msg, out ex);
            //remove from list of messages that need to be acked
            if (_subscriberMsgList != null)
            {
               if (success && ackKeyList != null)
               {
                  _subscriberMsgList.RemoveFromNeedToAckList(msg.DestClientType, msg.DestClientID, ackKeyList);
               }
            }
            return success;
         }
         return false;
      }
      #endregion

      #region Other
      /// <summary>
      /// Returns true if client is online
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <returns>online status of client</returns>
      public OnlineStatus IsClientOnline(int clientType, int clientID)
      {
         if (_subscriberMsgList == null)
            return OnlineStatus.Unknown;

         if (_subscriberMsgList.IsClientOnline(clientType, clientID))
            return OnlineStatus.IsOnline;

         return OnlineStatus.NotOnline;
      }
      #endregion

      #region Private methods
      private void _connectionHandler_MessageReceived(object sender, ConnectionHandler.MessageDetails e)
      {
         if (e != null)
         {
            if (e.Error != null)
            {
               if (e.Error is System.IO.IOException ioex)
               {
                  //disconnected from remote host 
                  Disconnect(DisconnectReason.ServerDisconnected, e.Error.Message);
               }
               else
               {
                  Disconnect(DisconnectReason.Exception, e.Error.Message);
               }
            }
            if (e.Message != null)
            {
               Matrix.Utilities.Diagnostics.MessageLogger.LogMessage(String.Format("Received {0} msg (key={1}) from client ({2},{3})"
                     , e.Message.MsgTypeID, e.Message.MsgKey, e.Message.OrigClientType, e.Message.OrigClientID));
               //if this is an Ack to the Logon message
               if (e.Message.MsgTypeID == MsgType.Ack && !_isLoggedOn && _logonMsg != null && _logonMsg.MsgKey == e.Message.MsgKey)
               {
                  lock (_lock)
                  {
                     _isLoggedOn = true;
                     if (_heartbeatTimer != null)
                        _heartbeatTimer.Change(_heartbeatTimeMS, _heartbeatTimeMS / 2);
                  }
                  SendSubscribeMessages();
                  OnLogonComplete();
               }
               var msg = e.Message;
//               lock (_lock)
               {
                  //if we are tracking
                  if (_msgStore != null)
                  {
                     foreach (var ack in msg.AckKeys)
                     {
                        _msgStore.RemoveMessage(ack);
                     }
                     if (msg.MsgTypeID == MsgType.Ack)
                     {
                        _msgStore.RemoveMessage(msg.MsgKey);
                     }
                     else if (msg.ReplyMsgKey > 0)
                        _msgStore.RemoveMessage(msg.ReplyMsgKey);
                  }
               }
               bool msgHandled = false;
               if (_subscriberMsgList != null)
               {
                  if (msg.MsgTypeID == MsgType.Logoff)
                  {
                     _subscriberMsgList.SetClientOnLine(msg.OrigClientType, msg.OrigClientID, false);
                  }
                  else
                  {
                     _subscriberMsgList.SetClientOnLine(msg.OrigClientType, msg.OrigClientID, true);
                     if (msg.MsgTypeID == MsgType.Ack)
                     {
                        msgHandled = HandleReceivedMessage(msg.MsgKey, msg, true);
                     }
                     else if (msg.ReplyMsgKey > 0)
                     {
                        msgHandled = HandleReceivedMessage(msg.ReplyMsgKey, msg, false);
                     }
                     if (msg.AckKeys != null)
                     {
                        foreach (var key in msg.AckKeys)
                        {
                           HandleReceivedMessage(key, msg, true);
                        }
                     }
                  }
               }
               if (!msgHandled)
                  NoClientContext.MessageReceived?.Invoke(msg, null);
               OnMessageReceived(msg);
            }
         }
      }
      bool HandleReceivedMessage(int msgKey, Header rxMsg, bool ackReceived)
      {
         IClientContext clientContext;
         var forSentMsg = _subscriberMsgList.RemoveSentMessage(rxMsg.OrigClientType, rxMsg.OrigClientID, msgKey, out clientContext);

         WaitResponse waitResponse;
         if (_ackRxEvents.TryGetValue(msgKey, out waitResponse))
         {
            waitResponse.MsgReceived = rxMsg;
            waitResponse.AckEvent.Set();
            return true;
         }
         else if (clientContext != null)
         {
            if (ackReceived)
            {
               clientContext.AckReceived?.Invoke(forSentMsg);
            }
            else
            {
               clientContext.MessageReceived?.Invoke(rxMsg, forSentMsg);
            }
            return true;
         }
         return false;
      }
      /// <summary>
      /// Return true if this msg needs to be Acked (e.g. it needs to be tracked and resent until acked)
      /// </summary>
      /// <param name="sentMsg">the message to check</param>
      /// <returns>true if the message needs to be acked and resent until acked</returns>
      public bool NeedToAckMsg(Header sentMsg)
      {
         //only CUSTOM message sent to a specific client should be acked
         //and don't track it if the message is a Reply
         return sentMsg.MsgTypeID == MsgType.Custom && sentMsg.ReplyMsgKey == 0 && sentMsg.DestClientType > 0;
      }
      /// <summary>
      /// if a message has not been sent in the heartbeat time, send a _heartbeatTimeMS
      /// </summary>
      /// <param name="x"></param>
      void HeartbeatTimer_Timer(object x)
      {
         lock (_lock)
         {
            if (_connectionHandler != null)
            {
               if ((System.DateTime.Now - _connectionHandler.LastMessageRx) > TimeSpan.FromMilliseconds(_heartbeatTimeMS)
                     || (System.DateTime.Now - _connectionHandler.LastMessageSent) > TimeSpan.FromMilliseconds(_heartbeatTimeMS))
               {
                  Exception ex;
                  _connectionHandler.SendHeartbeatMessage(out ex);
               }
            }
         }
      }
      #region server connection checking
      private void _serverTimeoutTimer_Tick(object sender, Utilities.ThreadingTimer.TickEventArgs e)
      {
         lock (_lock)
         {
				var now = System.DateTime.Now;
				if (ClientSocketState == SocketState.Connected)
            {
               if (_connectionHandler == null || !_connectionHandler.CheckConnection())
               {
                  Disconnect(DisconnectReason.ServerDisconnected);
               }
               else if ((now - _connectionHandler.LastMessageRx) > TimeSpan.FromMilliseconds(_serverTimeOutTimeMS))
               {
                  Disconnect(DisconnectReason.ServerNotResponding);
               }
            }
         }
      }

      void ReconnectRetry(CancellationToken cancelToken)
      {
         if (!_stopped && (cancelToken == null || !cancelToken.IsCancellationRequested))
         {
            ConnectInternal();
         }
      }
      void CancelReconnectTask(bool stop = false)
      {
         lock (_lock)
         {
            if (stop)
               _stopped = true;

            if (_reconnectRetryTask != null)
            {
               _reconnectRetryTask.Cancel();
               _reconnectRetryTask = null;
            }
         }
      }
      Task StartReconnectTask()
      {
         Task reconnectTask = null;
         if (_reconnectRetryTimeMS > 0)
         {
            lock (_lock)
            {
               if (_reconnectRetryTask != null)
               {
                  _reconnectRetryTask.Cancel();
               }
               _reconnectRetryTask = new PauseAndExecuter();
               reconnectTask = _reconnectRetryTask.ExecuteAsync(ReconnectRetry, (int)_reconnectRetryTimeMS);
            }
         }
         return reconnectTask;
      }

      /// <summary>
      /// Checks if the connection status has changed and raises an event if it has.
      /// NOTE:  _lock should not be locked when calling this function
      /// </summary>
      /// <param name="reason">the disconnect reason</param>
      /// <param name="newState">the new state of the socket</param>
      /// <param name="details">details of the disconnect</param>
      /// <returns></returns>
      private void CheckConnectionChanged(SocketState newState, DisconnectReason reason = DisconnectReason.None, string details = "")
      {
         bool changed = false;
         bool isConnected = IsConnected;
         lock (_lock)
         {
            if (_stopped && newState != SocketState.Disconnecting)
               newState = SocketState.Disconnected;
            changed = (newState != ClientSocketState);
            ClientSocketState = newState;
         }
         if (changed)
            OnConnectionStatusChanged(newState, reason, details);
         if(isConnected != IsConnected)
            OnConnectChanged(IsConnected);
      }
      private void BeginReading()
      {
         if (ClientSocketState == SocketState.Connected)
         {
            lock (_lock)
            {
               _connectionHandler.BeginReading();
            }
         }
      }
      #endregion

      #region handle acks and resending messages
      private void _resendMsgTimer_Tick(object sender, Utilities.ThreadingTimer.TickEventArgs e)
      {
         if (_isLoggedOn && !_resendPaused)
         {
            foreach (var subscriber in _subscriberMsgList)
            {
               if (subscriber.IsOnline)
               {
                  var msgList = subscriber.GetMessages(_resendMessagesTimeMS, DateTime.Now);
                  if (msgList != null)
                  {
                     foreach (var msg in msgList)
                     {
                        msg.DestClientType = subscriber.Key.ClientType;
                        msg.DestClientID = subscriber.Key.ClientID;
                        msg.IsArchived = true;
                        SendMessage(msg);
                     }
                  }
               }
            }
         }
         _resendMsgTimer.Change(_resendMessagesTimeMS, Timeout.Infinite);
      }
      #endregion

      #endregion
   }
}
