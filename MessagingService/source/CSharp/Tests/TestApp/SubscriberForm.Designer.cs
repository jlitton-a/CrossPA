namespace Matrix.MsgService.TestApp
{
   partial class SubscriberForm
   {
      /// <summary>
      /// Required designer variable.
      /// </summary>
      private System.ComponentModel.IContainer components = null;

      /// <summary>
      /// Clean up any resources being used.
      /// </summary>
      /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
      protected override void Dispose(bool disposing)
      {
         if (disposing && (components != null))
         {
            components.Dispose();
         }
         base.Dispose(disposing);
      }

      #region Windows Form Designer generated code

      /// <summary>
      /// Required method for Designer support - do not modify
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         this._outputTextBox = new System.Windows.Forms.TextBox();
         this._subTopicTextBox = new System.Windows.Forms.TextBox();
         this.label1 = new System.Windows.Forms.Label();
         this._subscribeButton = new System.Windows.Forms.Button();
         this.groupBox1 = new System.Windows.Forms.GroupBox();
         this._clientIDTextBox = new System.Windows.Forms.TextBox();
         this.label7 = new System.Windows.Forms.Label();
         this.label5 = new System.Windows.Forms.Label();
         this._clientTypeTextBox = new System.Windows.Forms.TextBox();
         this._connectButton = new System.Windows.Forms.Button();
         this.label2 = new System.Windows.Forms.Label();
         this.label3 = new System.Windows.Forms.Label();
         this._portTextBox = new System.Windows.Forms.TextBox();
         this._ipAddressTextBox = new System.Windows.Forms.TextBox();
         this._clearButton = new System.Windows.Forms.Button();
         this._unsubscribeButton = new System.Windows.Forms.Button();
         this.label4 = new System.Windows.Forms.Label();
         this._subClientTypeTextBox = new System.Windows.Forms.TextBox();
         this._subscriptionsGroupBox = new System.Windows.Forms.GroupBox();
         this._subClientIDTextBox = new System.Windows.Forms.TextBox();
         this.label6 = new System.Windows.Forms.Label();
         this.groupBox1.SuspendLayout();
         this._subscriptionsGroupBox.SuspendLayout();
         this.SuspendLayout();
         // 
         // _outputTextBox
         // 
         this._outputTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this._outputTextBox.Location = new System.Drawing.Point(5, 197);
         this._outputTextBox.Multiline = true;
         this._outputTextBox.Name = "_outputTextBox";
         this._outputTextBox.ReadOnly = true;
         this._outputTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
         this._outputTextBox.Size = new System.Drawing.Size(619, 263);
         this._outputTextBox.TabIndex = 0;
         // 
         // _subTopicTextBox
         // 
         this._subTopicTextBox.Location = new System.Drawing.Point(74, 43);
         this._subTopicTextBox.Name = "_subTopicTextBox";
         this._subTopicTextBox.Size = new System.Drawing.Size(78, 20);
         this._subTopicTextBox.TabIndex = 1;
         this._subTopicTextBox.Text = "0";
         // 
         // label1
         // 
         this.label1.AutoSize = true;
         this.label1.Location = new System.Drawing.Point(34, 47);
         this.label1.Name = "label1";
         this.label1.Size = new System.Drawing.Size(34, 13);
         this.label1.TabIndex = 2;
         this.label1.Text = "Topic";
         // 
         // _subscribeButton
         // 
         this._subscribeButton.Location = new System.Drawing.Point(236, 11);
         this._subscribeButton.Name = "_subscribeButton";
         this._subscribeButton.Size = new System.Drawing.Size(75, 23);
         this._subscribeButton.TabIndex = 3;
         this._subscribeButton.Text = "Subscribe";
         this._subscribeButton.UseVisualStyleBackColor = true;
         this._subscribeButton.Click += new System.EventHandler(this._subscribeButton_Click);
         // 
         // groupBox1
         // 
         this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.groupBox1.Controls.Add(this._clientIDTextBox);
         this.groupBox1.Controls.Add(this.label7);
         this.groupBox1.Controls.Add(this.label5);
         this.groupBox1.Controls.Add(this._clientTypeTextBox);
         this.groupBox1.Controls.Add(this._connectButton);
         this.groupBox1.Controls.Add(this.label2);
         this.groupBox1.Controls.Add(this.label3);
         this.groupBox1.Controls.Add(this._portTextBox);
         this.groupBox1.Controls.Add(this._ipAddressTextBox);
         this.groupBox1.Location = new System.Drawing.Point(11, 12);
         this.groupBox1.Name = "groupBox1";
         this.groupBox1.Size = new System.Drawing.Size(613, 100);
         this.groupBox1.TabIndex = 5;
         this.groupBox1.TabStop = false;
         this.groupBox1.Text = "Connection";
         // 
         // _clientIDTextBox
         // 
         this._clientIDTextBox.Location = new System.Drawing.Point(212, 71);
         this._clientIDTextBox.Name = "_clientIDTextBox";
         this._clientIDTextBox.Size = new System.Drawing.Size(100, 20);
         this._clientIDTextBox.TabIndex = 12;
         this._clientIDTextBox.Text = "0";
         // 
         // label7
         // 
         this.label7.AutoSize = true;
         this.label7.Location = new System.Drawing.Point(159, 74);
         this.label7.Name = "label7";
         this.label7.Size = new System.Drawing.Size(47, 13);
         this.label7.TabIndex = 13;
         this.label7.Text = "Client ID";
         // 
         // label5
         // 
         this.label5.AutoSize = true;
         this.label5.Location = new System.Drawing.Point(15, 74);
         this.label5.Name = "label5";
         this.label5.Size = new System.Drawing.Size(60, 13);
         this.label5.TabIndex = 10;
         this.label5.Text = "Client Type";
         // 
         // _clientTypeTextBox
         // 
         this._clientTypeTextBox.Location = new System.Drawing.Point(81, 71);
         this._clientTypeTextBox.Name = "_clientTypeTextBox";
         this._clientTypeTextBox.Size = new System.Drawing.Size(72, 20);
         this._clientTypeTextBox.TabIndex = 9;
         this._clientTypeTextBox.Text = "10";
         // 
         // _connectButton
         // 
         this._connectButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this._connectButton.Location = new System.Drawing.Point(532, 69);
         this._connectButton.Name = "_connectButton";
         this._connectButton.Size = new System.Drawing.Size(75, 23);
         this._connectButton.TabIndex = 8;
         this._connectButton.Text = "Connect";
         this._connectButton.UseVisualStyleBackColor = true;
         this._connectButton.Click += new System.EventHandler(this._connectButton_Click);
         // 
         // label2
         // 
         this.label2.AutoSize = true;
         this.label2.Location = new System.Drawing.Point(49, 48);
         this.label2.Name = "label2";
         this.label2.Size = new System.Drawing.Size(26, 13);
         this.label2.TabIndex = 7;
         this.label2.Text = "Port";
         // 
         // label3
         // 
         this.label3.AutoSize = true;
         this.label3.Location = new System.Drawing.Point(17, 22);
         this.label3.Name = "label3";
         this.label3.Size = new System.Drawing.Size(58, 13);
         this.label3.TabIndex = 6;
         this.label3.Text = "IP Address";
         // 
         // _portTextBox
         // 
         this._portTextBox.Location = new System.Drawing.Point(81, 45);
         this._portTextBox.Name = "_portTextBox";
         this._portTextBox.Size = new System.Drawing.Size(72, 20);
         this._portTextBox.TabIndex = 5;
         this._portTextBox.Text = "8888";
         // 
         // _ipAddressTextBox
         // 
         this._ipAddressTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this._ipAddressTextBox.Location = new System.Drawing.Point(81, 19);
         this._ipAddressTextBox.Name = "_ipAddressTextBox";
         this._ipAddressTextBox.Size = new System.Drawing.Size(518, 20);
         this._ipAddressTextBox.TabIndex = 4;
         this._ipAddressTextBox.Text = "localhost";
         // 
         // _clearButton
         // 
         this._clearButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this._clearButton.Location = new System.Drawing.Point(543, 165);
         this._clearButton.Name = "_clearButton";
         this._clearButton.Size = new System.Drawing.Size(75, 23);
         this._clearButton.TabIndex = 6;
         this._clearButton.Text = "Clear";
         this._clearButton.UseVisualStyleBackColor = true;
         this._clearButton.Click += new System.EventHandler(this._clearButton_Click);
         // 
         // _unsubscribeButton
         // 
         this._unsubscribeButton.Location = new System.Drawing.Point(236, 40);
         this._unsubscribeButton.Name = "_unsubscribeButton";
         this._unsubscribeButton.Size = new System.Drawing.Size(75, 23);
         this._unsubscribeButton.TabIndex = 7;
         this._unsubscribeButton.Text = "Unsubcribe";
         this._unsubscribeButton.UseVisualStyleBackColor = true;
         this._unsubscribeButton.Click += new System.EventHandler(this._unsubscribeButton_Click);
         // 
         // label4
         // 
         this.label4.AutoSize = true;
         this.label4.Location = new System.Drawing.Point(8, 21);
         this.label4.Name = "label4";
         this.label4.Size = new System.Drawing.Size(60, 13);
         this.label4.TabIndex = 9;
         this.label4.Text = "Client Type";
         // 
         // _subClientTypeTextBox
         // 
         this._subClientTypeTextBox.Location = new System.Drawing.Point(74, 17);
         this._subClientTypeTextBox.Name = "_subClientTypeTextBox";
         this._subClientTypeTextBox.Size = new System.Drawing.Size(78, 20);
         this._subClientTypeTextBox.TabIndex = 8;
         this._subClientTypeTextBox.Text = "0";
         // 
         // _subscriptionsGroupBox
         // 
         this._subscriptionsGroupBox.Controls.Add(this._subClientIDTextBox);
         this._subscriptionsGroupBox.Controls.Add(this.label6);
         this._subscriptionsGroupBox.Controls.Add(this._subClientTypeTextBox);
         this._subscriptionsGroupBox.Controls.Add(this.label4);
         this._subscriptionsGroupBox.Controls.Add(this._subTopicTextBox);
         this._subscriptionsGroupBox.Controls.Add(this.label1);
         this._subscriptionsGroupBox.Controls.Add(this._unsubscribeButton);
         this._subscriptionsGroupBox.Controls.Add(this._subscribeButton);
         this._subscriptionsGroupBox.Location = new System.Drawing.Point(12, 118);
         this._subscriptionsGroupBox.Name = "_subscriptionsGroupBox";
         this._subscriptionsGroupBox.Size = new System.Drawing.Size(525, 73);
         this._subscriptionsGroupBox.TabIndex = 10;
         this._subscriptionsGroupBox.TabStop = false;
         this._subscriptionsGroupBox.Text = "Subscriptions";
         // 
         // _subClientIDTextBox
         // 
         this._subClientIDTextBox.Location = new System.Drawing.Point(158, 27);
         this._subClientIDTextBox.Name = "_subClientIDTextBox";
         this._subClientIDTextBox.Size = new System.Drawing.Size(67, 20);
         this._subClientIDTextBox.TabIndex = 10;
         this._subClientIDTextBox.Text = "0";
         // 
         // label6
         // 
         this.label6.AutoSize = true;
         this.label6.Location = new System.Drawing.Point(158, 11);
         this.label6.Name = "label6";
         this.label6.Size = new System.Drawing.Size(47, 13);
         this.label6.TabIndex = 11;
         this.label6.Text = "Client ID";
         // 
         // SubscriberForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(636, 464);
         this.Controls.Add(this._subscriptionsGroupBox);
         this.Controls.Add(this._clearButton);
         this.Controls.Add(this.groupBox1);
         this.Controls.Add(this._outputTextBox);
         this.Name = "SubscriberForm";
         this.ShowIcon = false;
         this.Text = "Message Subscriber";
         this.groupBox1.ResumeLayout(false);
         this.groupBox1.PerformLayout();
         this._subscriptionsGroupBox.ResumeLayout(false);
         this._subscriptionsGroupBox.PerformLayout();
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.TextBox _outputTextBox;
      private System.Windows.Forms.TextBox _subTopicTextBox;
      private System.Windows.Forms.Label label1;
      private System.Windows.Forms.Button _subscribeButton;
      private System.Windows.Forms.GroupBox groupBox1;
      private System.Windows.Forms.Button _connectButton;
      private System.Windows.Forms.Label label2;
      private System.Windows.Forms.Label label3;
      private System.Windows.Forms.TextBox _portTextBox;
      private System.Windows.Forms.TextBox _ipAddressTextBox;
      private System.Windows.Forms.Button _clearButton;
      private System.Windows.Forms.Button _unsubscribeButton;
      private System.Windows.Forms.Label label4;
      private System.Windows.Forms.TextBox _subClientTypeTextBox;
      private System.Windows.Forms.Label label5;
      private System.Windows.Forms.TextBox _clientTypeTextBox;
      private System.Windows.Forms.GroupBox _subscriptionsGroupBox;
      private System.Windows.Forms.TextBox _subClientIDTextBox;
      private System.Windows.Forms.Label label6;
      private System.Windows.Forms.TextBox _clientIDTextBox;
      private System.Windows.Forms.Label label7;
   }
}

