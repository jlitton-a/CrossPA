using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

using Matrix.MessagingService.CommonMessages;
using Matrix.MessagingService.CommunicationUtils;
using Google.Protobuf;

namespace Matrix.MessagingService.TestApp
{
   /// <summary>
   /// Form for testing Messaging Service
   /// </summary>
	public partial class SubscriberForm : Form
	{
      ClientComm _messengerClient;

		/// <summary>
		/// Constructor
		/// </summary>
		public SubscriberForm()
		{
			InitializeComponent();

         Logon logonInfoMsg = new Logon();
         logonInfoMsg.ClientType = Convert.ToInt32(_clientTypeTextBox.Text);
         logonInfoMsg.ClientID = Convert.ToInt32(_clientIDTextBox.Text);

         _messengerClient = new ClientComm("TestApp", _ipAddressTextBox.Text, Convert.ToInt32(_portTextBox.Text), logonInfoMsg, 1000);
         _messengerClient.ConnectionStatusChanged += _messengerClient_ConnectionStatusChanged;
         _messengerClient.MessageReceived += _messengerClient_MessageReceived;

         this._portTextBox.Text = ClientComm.DEFAULT_PORT.ToString();

         SetupButtons();
		}

      #region Methods
      /// <summary>
      /// Set button states
      /// </summary>
      private void SetupButtons()
		{
         if (_messengerClient.ClientSocketState == ClientComm.SocketState.Disconnected || _messengerClient.ClientSocketState == ClientComm.SocketState.Disconnecting)
            _connectButton.Text = "Connect";
         else
            _connectButton.Text = "Disconnect";
		}
      Logon _logonInfoMsg;
      string _ipAddress;
      int _port;
      /// <summary>
      /// Connect to server
      /// </summary>
      private void Connect()
		{
			try
			{
            var clientType = Convert.ToInt32(_clientTypeTextBox.Text);
            var clientID = Convert.ToInt32(_clientIDTextBox.Text);
            var port = Convert.ToInt32(_portTextBox.Text);
            if (_logonInfoMsg == null
                  || _logonInfoMsg.ClientType != clientType || _logonInfoMsg.ClientID != clientID
                  || _ipAddress != _ipAddressTextBox.Text || _port != port)
            {
               _port = port;
               _ipAddress = _ipAddressTextBox.Text;
               _logonInfoMsg = new Logon();
               _logonInfoMsg.ClientType = clientType;
               _logonInfoMsg.ClientID = clientID;
               _messengerClient.ChangeConnection(_ipAddressTextBox.Text, port, _logonInfoMsg);
            }
            _messengerClient.Connect();
         }
         catch (SocketException ex)
			{
				MessageBox.Show(string.Format("SocketException: {0}", ex));
			}
		}

		/// <summary>
		/// Disconnect from server
		/// </summary>
		private void Disconnect()
		{
			_messengerClient.Disconnect();
		}
		
		/// <summary>
		/// Subscribe to message topic
		/// </summary>
		private void Subscribe(bool subscribe = true)
		{
         int topic = Convert.ToInt32(_subTopicTextBox.Text);
         int clientType = Convert.ToInt32(_subClientTypeTextBox.Text);
         int clientID = Convert.ToInt32(_subClientIDTextBox.Text);
         _messengerClient.AddSubscribe(clientType, clientID, topic, subscribe);
      }
      #endregion

      /// <summary>
      /// _subscribeButton click event handler
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void _subscribeButton_Click(object sender, EventArgs e)
		{
			Subscribe(true);
		}

		/// <summary>
		/// _connectButton click event handler
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void _connectButton_Click(object sender, EventArgs e)
		{
			if (_messengerClient.ClientSocketState == ClientComm.SocketState.Disconnected || _messengerClient.ClientSocketState == ClientComm.SocketState.Disconnecting)
				Connect();
			else
				Disconnect();
		}

      private void _messengerClient_MessageReceived(object sender, Header e)
      {
         if(e != null)
            AddText(e.ToString());
      }


      private void _messengerClient_ConnectionStatusChanged(object sender, ClientComm.ConnectionStatusChangedEventArgs e)
      {
         string info;
         if (e.Reason == ClientComm.DisconnectReason.None)
            info = string.Format("Connection Status: {0}; {1}", e.ClientSocketState, e.Details);
         else
            info = string.Format("Connection Status: {0}; {1}; {2}", e.ClientSocketState, e.Reason, e.Details);

         AddText(info);
         if (InvokeRequired)
            this.Invoke(new Action(() => SetupButtons()));
         else
            SetupButtons();
      }
      private void AddText(string textToAdd)
      {
         if (_outputTextBox.InvokeRequired)
            _outputTextBox.Invoke(new Action(() => _outputTextBox.AppendText(textToAdd + "\n")));
         else
            _outputTextBox.AppendText(textToAdd + "\n");
      }

      /// <summary>
      /// _clearButton event handler
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void _clearButton_Click(object sender, EventArgs e)
		{
			_outputTextBox.Text = "";
		}

		/// <summary>
		/// _unsubscribeButton click event handler
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void _unsubscribeButton_Click(object sender, EventArgs e)
		{
			Subscribe(false);
		}
	}
}
