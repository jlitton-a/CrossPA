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
      ClientComm.OnlineStatus IsClientOnline(int clientType, int clientID);

   }
   /// <summary>
   /// Class for connecting to and communicating with the Messaging Service.  
   /// Will send a periodic heartbeat and will automatically attempt to reconnect when connection is lost.
   /// If a logon msg is passed in, it will automatically send it and any subscriptions once connected.
   /// </summary>
   public class ClientComm : IClientComm
	{
		#region enums, types and const
      public enum OnlineStatus
      {
         Unknown,
         IsOnline,
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
		/// Default port if it is not specified
		/// </summary>
		public const int DEFAULT_PORT = 8888;
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
      /// <param name="hostname">host address</param>
      /// <param name="port">port to use</param>
      /// <param name="logonMsg">The Logon message to send after connecting</param>
      /// <param name="reconnectRetryTimeMS">Number of milliseconds between connect retries when connection is lost; 0 to not retry</param>
      /// <param name="heartbeatTimeMS">Number of milliseconds between when keep alive messages should be sent; 0 to not send heartbeats</param>
      /// <param name="serverTimeOutTimeMS">Number of milliseconds to consider the server has timed out; 0 to not check</param>
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
		/// <param name="reconnectRetryTimeMS">Number of milliseconds between connect retries when connection is lost; 0 to not retry</param>
		/// <param name="heartbeatTimeMS">Number of milliseconds between when keep alive messages should be sent; 0 to not send heartbeats</param>
		/// <param name="serverTimeOutTimeMS">Number of milliseconds to consider the server has timed out; 0 to not check</param>
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
      /// <param name="logonMsg">The Logon message to send after connecting</param>
      /// <param name="reconnectRetryTimeMS">Number of milliseconds between connect retries when connection is lost; 0 to not retry</param>
      /// <param name="heartbeatTimeMS">Number of milliseconds between when keep alive messages should be sent; 0 to not send heartbeats</param>
      /// <param name="serverTimeOutTimeMS">Number of milliseconds to consider the server has timed out; 0 to not check</param>
      /// <param name="trackSentMessages">True to track sent messages and resend if they are not acked (not fully implemented yet)</param>
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
         if (resendMessagesTimeMS > 0)
         {
            _subscriberMsgList = subscriberMsgList ?? new SubscriberMessageLists();
            _resendMsgTimer = resendMsgTimer ?? new Matrix.Utilities.ThreadingTimer();
            _resendMsgTimer.Tick += _resendMsgTimer_Tick;
         }
         ClientSocketState = SocketState.Disconnected;
			Name = name;
			_msgKey = 1;
			_subscriptions = new HashSet<Subscribe>();
			if (logonMsg != null)
			{
				_logonMsg = new Header();
				_logonMsg.MsgKey = _msgKey++;
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

      }

      /// <summary>
      /// Disconnect and dispose timers and tasks
      /// </summary>
      public void Dispose()
		{
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
			/// <param name="isConnected">true if it is currently connected</param>
			/// <param name="reason">When isConnected is false, the reason</param>
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
      protected virtual void OnLogonComplete()
      {
         //only do this once on startup
         //load messages that need to be sent, send them, then add them to the store
         //to be removed by an ack
         IMsgStoreRecordList msgStoreList = null;
         lock (_lock)
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
                  msgStoreRecord.HdrMsg.MsgKey = _msgKey++;
                  msgStoreRecord.HdrMsg.IsArchived = true;
                  lock (_lock)
                  {
                     _msgStore.AddMsgStoreRecord(msgStoreRecord, msgStoreRecord.HdrMsg.MsgKey);
                  }
                  if (_subscriberMsgList != null && NeedToTrackSentMsg(msgStoreRecord.HdrMsg))
                  {
                     _subscriberMsgList.AddSentMessage(msgStoreRecord.HdrMsg);
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

		public class SubscribedEventArgs : EventArgs
		{
			public CommonMessages.ClientTypes ClientType { get; set; }
			public int Topic { get; set; }
		}
	
		/// <summary>
		/// Event occurs after subscribe
		/// </summary>
		public event EventHandler<SubscribedEventArgs> Subscribed;
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
               _logonMsg.MsgKey = _msgKey++;
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
         SendCommonMessageInternal(MsgType.Ack, null, msgToAck.MsgKey, 0, msgToAck.OrigClientType, msgToAck.OrigClientID, false);
      }
      /// <summary>
      /// Sends a common message, returns the MsgKey used
      /// </summary>
      /// <param name="msgType">type of message</param>
      /// <param name="msgToSend">sub message to send</param>
      /// <param name="topic">the topic of this message</param>
      /// <param name="destClientType">specific client type to send it to, 0 to broadcast</param>
      /// <param name="destClientID">specific client ID to send it to, 0 for all</param>
      /// <param name="storeMsg">true to store this message for resending on application restart</param>
      /// <param name="isArchived">true to set the isArchived flag</param>
      /// <returns>The msg key that was used</returns>
      public Header SendCommonMessage(MsgType msgType, Google.Protobuf.IMessage msgToSend, int topic = 0, int destClientType = 0, int destClientID = 0, bool storeMsg = false, bool isArchived = false)
      {
         return SendCommonMessageInternal(msgType, msgToSend, _msgKey++, topic, destClientType, destClientID, storeMsg, isArchived);
      }

      /// <summary>
      /// Sends a common message, returns the MsgKey used
      /// </summary>
      /// <param name="msgType">type of message</param>
      /// <param name="msgToSend">sub message to send</param>
      /// <param name="msgKey">the key to use</param>
      /// <param name="topic">the topic of this message</param>
      /// <param name="destClientType">specific client type to send it to, 0 to broadcast</param>
      /// <param name="destClientID">specific client ID to send it to, 0 for all</param>
      /// <param name="storeMsg">true to store this message for resending on application restart</param>
      /// <param name="isArchived">true to set the isArchived flag</param>
      /// <returns>The msg key that was used</returns>
      private Header SendCommonMessageInternal(MsgType msgType, Google.Protobuf.IMessage msgToSend, int msgKey, int topic, int destClientType, int destClientID, bool storeMsg, bool isArchived = false)
      {
         Header msg = new Header();
         msg.MsgKey = msgKey;
         msg.MsgTypeID = msgType;
         msg.Topic = topic;
         msg.DestClientType = destClientType;
         msg.DestClientID = destClientID;
         msg.IsArchived = isArchived;
         if (msgToSend != null)
            msg.Msg = msgToSend.ToByteString();

         if (_subscriberMsgList != null && NeedToTrackSentMsg(msg))
         {
            _subscriberMsgList.AddSentMessage(msg);
         }
         lock (_lock)
         {
            if (storeMsg && _msgStore != null)
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
               bool needToAck = NeedToAckRxMsg(msg);
               lock (_lock)
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
                  }
               }

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
                        _subscriberMsgList.RemoveSentMessage(msg.OrigClientType, msg.OrigClientID, msg.MsgKey);
                     }
                  }
                  if (msg.AckKeys != null)
                  {
                     _subscriberMsgList.RemoveSentMessages(msg.OrigClientType, msg.OrigClientID, msg.AckKeys);
                  }
                  //Add this message as one that needs to be acked
                  //                     if (needToAck)
                  //                        _subscriberMsgList.AddToNeedToAckList(msg);
               }

               OnMessageReceived(e.Message);
               //TODO: for now ack each message, eventually use a timer to ack multiple messages 
               if (needToAck)
               {
                  SendAckMessage(e.Message);
               }
            }
         }
      }
      /// <summary>
      /// Return true if an ack should be sent for this msg
      /// </summary>
      /// <param name="rxMsg">the message to check</param>
      /// <returns>true if an ack should be sent</returns>
      bool NeedToAckRxMsg(Header rxMsg)
      {
         //only CUSTOM message sent to a specific client should be acked
         return rxMsg.MsgTypeID == MsgType.Custom && rxMsg.DestClientType > 0;
      }
      /// <summary>
      /// Return true if this msg needs to be tracked and resent until acked
      /// </summary>
      /// <param name="sentMsg">the message to check</param>
      /// <returns>true if we should resend if not acked</returns>
      bool NeedToTrackSentMsg(Header sentMsg)
      {
         //only CUSTOM message sent to a specific client should be acked
         return sentMsg.MsgTypeID == MsgType.Custom && sentMsg.DestClientType > 0;
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
      /// <param name="reason"></param>
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
