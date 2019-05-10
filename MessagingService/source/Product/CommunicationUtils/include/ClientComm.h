#pragma once
//disable Inherits Via Dominance warning
#ifdef _WIN32
#pragma warning( push )
#pragma warning( disable: 4250)
#endif


#include <boost/asio.hpp>
#include <memory>

#include "../stdafx.h"
#include "SharedFromThis.h"
#include "CommHandler.h"
#include "MessageUtils.h"
#include "ClientTypes.h"
#include "SubscriptionParams.h"
#include "IClientComm.h"
#include "CountdownTimer.h"

namespace asio = boost::asio;
namespace CommonMessages = Matrix::MsgService::CommonMessages;

namespace Matrix
{
   namespace Common
   {
      namespace Logging
      {
         class Logger;
      }
   }

namespace MsgService
{
   namespace CommonMessages
   {
      class Message;
      class Header;
   }

namespace CommunicationUtils
{
   class ISubscriberMessageLists;

   /// <summary>
   /// Class for connecting to and communicating with the Messaging Service.  
   /// Will automatically attempt to reconnect when connection is lost.
   /// </summary>
   class ClientComm : public CommHandler, public virtual IClientComm
   {
   public:
      enum class OnlineStatus
      {
         UNKNOWN,
         OnLine,
         OffLine
      };
      //****************************************
      // Constructors/Destructors
      //****************************************
   public:
      /// <summary>
      /// Initializes a new instance of the <see cref="ClientComm"/> class.
      /// </summary>
      /// <param name="pContextHandler">The context handler</param>
      /// <param name="ipAddress">IP Address of the server to connect</param>
      /// <param name="port">Port to use to connect</param>
      /// <param name="clientType">The type for this client</param>
      /// <param name="clientID">The ID for this client</param>
      /// <param name="reconnectRetryTimeMS">Number of milliseconds to retry to connect when connection is lost</param>
      /// <param name="name">The optional name for this client (for logging)</param>
      /// <param name="pLogonSubMsg">Additional Logon sub message to send with the logon message</param>
      /// <param name="trackSentMessages">True to track sent messages and resend if they are not acked (not fully implemented yet)</param>
      COMMUNICATIONUTILS_API ClientComm(std::shared_ptr<IContextHandler> pContextHandler, const std::string ipAddress, const std::string port
            , int clientType, int clientID
            , uint32_t reconnectRetryTimeMS = 0, const std::string name = ""
            , const Matrix::MsgService::CommonMessages::Logon* const pLogonSubMsg = nullptr
            , bool trackSentMessages = false
            , std::shared_ptr<ISubscriberMessageLists> pSubscriberMsgLists = nullptr);
      /// <summary>
      /// Standard destructor
      /// </summary>
      COMMUNICATIONUTILS_API ~ClientComm();

      //****************************************
      // Fields
      //****************************************
   private:
      std::string _ipAddress;
      std::string _port;
      uint32_t _reconnectRetryTimeMS;
      boost::asio::ip::tcp::resolver _resolver;
      std::unique_ptr<CommonMessages::Message> _pMessage;
      bool _isLoggedOn;
      std::unique_ptr<CommonMessages::Header> _pLogonMsg;
      Matrix::Common::CountdownTimer _reconnectTimer;
      int _msgKey;
      //List of subscriptions to send after logon to message service
      std::vector<Matrix::MsgService::CommonMessages::SubscriptionParams> _subscriptions;
      std::shared_ptr<ISubscriberMessageLists> _pSubscriberMsgLists;

      //****************************************
      // Methods
      //****************************************
   public:
      using CommHandler::Disconnect;
      COMMUNICATIONUTILS_API bool IsLoggedOn() { return _isLoggedOn; }
      /// <summary>
      /// Asynchrounously attempt to connect to the server
      /// </summary>
      COMMUNICATIONUTILS_API bool Connect() override;
      /// <summary>
      /// Sends a Common message
      /// </summary>
      /// <param name="msgType">The type of message to send</param>
      /// <param name="pMessage">The sub-message</param>
      /// <param name="topic">The topic of the message</param>
      /// <param name="destClientType">The specific client type to send to, 0 for all</param>
      /// <param name="destClientID">The specific client ID to send to, 0 for all</param>
      /// <param name="isArchived">True to set the isArchived bit in the message</param>
      COMMUNICATIONUTILS_API int SendCommonMsg(CommonMessages::MsgType msgType
            , const google::protobuf::MessageLite* const pMessage = nullptr
            , int topic = 0
            , int destClientType = 0
            , int destClientID = 0
            , bool isArchived = false);
      /// <summary>
      /// Sends a Subscription
      /// </summary>
      /// <param name="objectTypeID">object type id (from ObjectTypes table) of the object updated</param>
      /// <param name="objectID">ID of the object that was updated</param>
      COMMUNICATIONUTILS_API bool Subscribe(CommonMessages::ClientTypes clientType, int clientID = 0, bool subscribe = true) override;

      COMMUNICATIONUTILS_API OnlineStatus IsClientOnline(int clientType, int clientID = 0);
   protected:
      /// <summary>
      /// Retry connecting to the message service
      /// </summary>
      COMMUNICATIONUTILS_API void RetryConnect();
      /// <summary>
      /// Handles the connection attempt
      /// </summary>
      COMMUNICATIONUTILS_API bool WrappedConnect(bool isRetry);
      /// <summary>
      /// Completes the successful connection
      /// </summary>
      /// <param name="endpoint">The endpoint that was connected</param>
      COMMUNICATIONUTILS_API void ConnectComplete(asio::ip::tcp::resolver::iterator endpoint);
      /// <summary>
      /// Raises the Connection Changed event
      /// </summary>
      /// <param name="isConnected">true if it is now connected, false if it is disconnected</param>
      COMMUNICATIONUTILS_API virtual void OnConnectionChanged(bool isConnected) override;
      /// <summary>
      /// Override to handle received messages 
      /// </summary>
      /// <param name="pMsg">The message received</param>
      COMMUNICATIONUTILS_API virtual void HandleMessageReceived(std::unique_ptr<CommonMessages::Header> pMsg) override;
      /// <summary>
      /// Override to Cancel the reconnect timer
      /// </summary>
      /// <param name="pMsg">The reason for the disconnect</param>
      COMMUNICATIONUTILS_API virtual void Disconnect(DisconnectReason reason) override;
      /// <summary>
      /// Override to start the reconnect timer when configured
      /// </summary>
      /// <param name="pMsg">The reason for the disconnect</param>
      COMMUNICATIONUTILS_API virtual void HandleDisconnect(DisconnectReason reason) override;
      /// <summary>
      /// Send an ack message for msgToAck
      /// </summary>
      /// <param name="msgToAck">The message to Ack</param>
      COMMUNICATIONUTILS_API bool SendAckMessage(const Matrix::MsgService::CommonMessages::Header msgToAck);

      //needed to to have shared_from_this work for derived classes 
      std::shared_ptr<ClientComm> shared_from_this() { return shared_from(this); }
      std::shared_ptr<const ClientComm> shared_from_this() const { return shared_from(this); }

   private:
      void SendSubscribeMessages();
      int SendCommonMsgInternal(CommonMessages::MsgType msgType
            , const google::protobuf::MessageLite* const pMessage
            , int msgKey
            , int topic
            , int destClientType
            , int destClientID
            , bool isArchived = false);
      /// <summary>
      /// Return true if an ack should be sent for this msg
      /// </summary>
      /// <param name="rxMsg">the message to check</param>
      /// <returns>true if an ack should be sent</returns>
      bool NeedToAckRxMsg(const Matrix::MsgService::CommonMessages::Header* const pRxMsg);
      /// <summary>
      /// Return true if this msg needs to be tracked and resent until acked
      /// </summary>
      /// <param name="sentMsg">the message to check</param>
      /// <returns>true if we should resend if not acked</returns>
      bool NeedToTrackSentMsg(const Matrix::MsgService::CommonMessages::Header* const pSentMsg);

   };
};
}
}
#ifdef _WIN32
#pragma warning( pop )
#endif
