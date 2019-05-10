#pragma once
//disable Inherits Via Dominance warning
#ifdef _WIN32
#pragma warning( push )
#pragma warning( disable: 4250)
#endif

#include <memory>
#include <boost/asio.hpp>

#include "../stdafx.h"
#include "SharedFromThis.h"
#include "WorkerThread.h"
#include "ICommHandler.h"
#include "IContextHandler.h"
#include "CommHandlerSignals.h"

namespace asio = boost::asio;
using namespace boost::asio::ip;
#ifdef USING_SSL
namespace ssl = boost::asio::ssl;
typedef boost::asio::ssl::stream<tcp::socket> socket_type;
#else
typedef tcp::socket socket_type;
#endif 

namespace Matrix
{
namespace MsgService
{
   namespace CommonMessages
   {
      class Message;
      class Header;
   };

namespace CommunicationUtils
{
   /// <summary>
   /// Class that handles sending and receiving messages
   /// </summary>
   class CommHandler : public Matrix::Common::WorkerThread, public virtual ICommHandler
   {
      //****************************************
      // Constructors/Destructors
      //****************************************
   public:
      /// <summary>
      /// Initializes a new instance of the <see cref="CommHandler"/> class.
      /// </summary>
      /// <param name="ioContext">ASIO context</param>
      /// <param name="name">Name to use for logging (if blank, ipaddress will be used)</param>
      COMMUNICATIONUTILS_API CommHandler(std::shared_ptr<IContextHandler> pContextHandler, std::string name = ""
            , bool isServer = false
            , int clientType = 0
            , int clientID = 0
            );
      /// <summary>
      /// Standard destructor
      /// </summary>
      COMMUNICATIONUTILS_API virtual ~CommHandler();

      //****************************************
      // Fields
      //****************************************
   protected:
      std::shared_ptr<IContextHandler> _pContextHandler;
      asio::io_context::strand _strand;
      //make it shared so async functions will not crash on Shutdown
      std::shared_ptr<socket_type> _pSocket;
      std::string _name;
      bool _isServer;
      //indicates that a manual disconnect was called
      bool _stopped;
      int _clientType;
      int _clientID;
   private:
      SocketState _socketState;
      DisconnectReason _disconnectReason;
      std::shared_ptr<CommonMessages::Message> _pRxMsg;
      std::shared_ptr<CommonMessages::Message> _pSendMsg;
      std::unique_ptr<CommonMessages::Message> _pEmptyMsg;
      int _emptySize;
      ConnectionChangeSignal _connectionChangeEvent;
      SocketStateChangeSignal _socketStateChangeEvent;
      MessageRxSignal _messageRxEvent;
      int64_t _msgRxCount;
      int64_t _msgSendCount;

      //****************************************
      // Methods
      //****************************************
   public:
      /// <summary>
      /// Register to receive notifications if IsConnected changes 
      /// </summary>
      /// <param name="callback">A callback function that will be called when the parameter is updated.</param>
      /// <returns>A connection that can be disconnected when notification is no longer needed</returns>
      COMMUNICATIONUTILS_API ConnectionChangeConnection AddConnectionChangeObserver(const ConnectionChangeCallback& callback);
      /// <summary>
      /// Register to receive notifications if GetSocketState changes
      /// </summary>
      /// <param name="callback">A callback function that will be called when the parameter is updated.</param>
      /// <returns>A connection that can be disconnected when notification is no longer needed</returns>
      COMMUNICATIONUTILS_API SocketStateChangeConnection AddSocketStateChangeObserver(const SocketStateChangeCallback& callback);
      /// <summary>
      /// Register to receive notifications of Messages received
      /// </summary>
      /// <param name="callback">A callback function that will be called when the parameter is updated.</param>
      /// <returns>A connection that can be disconnected when notification is no longer needed</returns>
      COMMUNICATIONUTILS_API MessageRxConnection AddMessageRxObserver(const MessageRxCallback& callback);
      COMMUNICATIONUTILS_API virtual void Diagnostics(DiagnosticTypes type) override;
      COMMUNICATIONUTILS_API virtual std::string GetDiagnosticsInfo() override;
      /// <summary>
      /// Gets the current connection state.
      /// </summary>
      COMMUNICATIONUTILS_API SocketState GetSocketState() { return _socketState; }
      /// <summary>
      /// Gets the socket.
      /// </summary>
      COMMUNICATIONUTILS_API socket_type* GetSocket();

      /// <summary>
      /// Starts the read process
      /// </summary>
      COMMUNICATIONUTILS_API void Run() override;
      /// <summary>
      /// Signals that the client comm should shutdown completely
      /// </summary>
      COMMUNICATIONUTILS_API virtual void ShutDown() override;
      /// <summary>
      /// Asynchrounously disconnects the socket
      /// </summary>
      COMMUNICATIONUTILS_API virtual void Disconnect();
      /// <summary>
      /// Returns true if Disconnect() was called
      /// </summary>
      COMMUNICATIONUTILS_API bool IsStopped() { return _stopped || IsShuttingDown(); }
      /// <summary>
      /// Returns true if the socket is connected 
      /// </summary>
      COMMUNICATIONUTILS_API bool IsConnected() { return _socketState == SocketState::Connected; }

      /// <summary>
      /// Returns a name to use for this client - can be used for logging
      /// </summary>
      /// <returns>A name to use for this client</return>
      COMMUNICATIONUTILS_API virtual std::string GetName() override;
      /// <summary>
      /// Asynchronously sends a message on the socket.  The HandleWrite
      /// method is called asynchrounously
      /// </summary>
      /// <param name="msg">The message to send</param>
      /// <returns>True if it successfully kicked off the async send</return>
      COMMUNICATIONUTILS_API virtual bool SendMsg(CommonMessages::Header& msg);
      /// <summary>
      /// Asynchronously sends a heartbeat message on the socket.  The HandleWrite
      /// method is called asynchrounously
      /// </summary>
      /// <returns>True if it successfully kicked off the async send</return>
      COMMUNICATIONUTILS_API bool SendHeartbeat();
   protected:
      /// <summary>
      /// Performs the message handling.  if pMsg is null, it is a heartbeat message
      /// </summary>
      /// <param name="msg">The message received, null if a heartbeat message was received</param>
      COMMUNICATIONUTILS_API virtual void HandleMessageReceived(std::unique_ptr<CommonMessages::Header> pMsg);
      /// <summary>
      /// Raises the Message Received event
      /// </summary>
      /// <param name="msg">The message received</param>
      COMMUNICATIONUTILS_API void OnMessageReceived(CommonMessages::Header* pMsg);
      /// <summary>
      /// Asynchrounously reads the next message.  The HandleMessageReceived
      /// method will be called to handle the message recieved
      /// </summary>
      COMMUNICATIONUTILS_API void StartRead();
      /// <summary>
      /// Disconnects the socket
      /// </summary>
      /// <param name="reason">The reason for the disconnect</param>
      COMMUNICATIONUTILS_API virtual void Disconnect(DisconnectReason reason);
      /// <summary>
      /// Updates the SocketState
      /// </summary>
      /// <param name="newState">The new socket state</param>
      /// <param name="reason">The reason for a disconnect</param>
      /// <returns>true if the socket state changed</returns>
      COMMUNICATIONUTILS_API virtual bool CheckConnectionChanged(SocketState newState, DisconnectReason reason = DisconnectReason::None);
      /// <summary>
      /// Raises the Connection Changed event
      /// </summary>
      /// <param name="isConnected">true if it is now connected, false if it is disconnected</param>
      COMMUNICATIONUTILS_API virtual void OnConnectionChanged(bool isConnected);
      /// <summary>
      /// Disconnect will call this asynchrounously
      /// </summary>
      /// <param name="reason">The reason for the disconnect</param>
      COMMUNICATIONUTILS_API virtual void HandleDisconnect(DisconnectReason reason);
   private:
      /// <summary>
      /// StartRead will call this asynchrounously
      /// </summary>
      COMMUNICATIONUTILS_API virtual void HandleRead(const boost::system::error_code& error,
            size_t bytes_transferred);
      /// <summary>
      /// SendMsg will call this asynchrounously
      /// </summary>
      COMMUNICATIONUTILS_API virtual void HandleWrite(const boost::system::error_code& error,
            size_t bytes_transferred);

      COMMUNICATIONUTILS_API void HandleHandshake(const boost::system::error_code& error);

#ifdef USING_SSL
      bool VerifyCertificate(bool preVerified,
         boost::asio::ssl::verify_context& ctx);
#endif
      //needed to to have shared_from_this work for derived classes 
      std::shared_ptr<CommHandler> shared_from_this() { return shared_from(this); }
      std::shared_ptr<const CommHandler> shared_from_this() const { return shared_from(this); }

   };

}
}
}
#ifdef _WIN32
#pragma warning( pop )
#endif
