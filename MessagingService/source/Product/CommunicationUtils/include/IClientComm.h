#pragma once

#include "../stdafx.h"
#include "ICommHandler.h"
#include "MessageUtils.h"
#include "ClientTypes.h"

namespace asio = boost::asio;

namespace Matrix
{
namespace MsgService
{
namespace CommunicationUtils
{
   class IClientComm : public virtual ICommHandler
   {
   public:
      /// <summary>
      /// Standard destructor
      /// </summary>
      virtual ~IClientComm() {}
      /// <summary>
      /// Connect to the message service
      /// </summary>
      virtual bool Connect() = 0;
      /// <summary>
      /// Sends a Common message
      /// </summary>
      /// <param name="msgType">The type of message to send</param>
      /// <param name="pMessage">The sub-message</param>
      /// <param name="topic">The topic of the message</param>
      /// <param name="destClientType">The specific client type to send to, 0 for all</param>
      /// <param name="destClientID">The specific client ID to send to, 0 for all</param>
      /// <param name="isArchived">true to set the isArchived flag in the message</param>
      virtual int SendCommonMsg(CommonMessages::MsgType msgType
         , const google::protobuf::MessageLite* const pMessage = nullptr
         , int topic = 0
         , int destClientType = 0
         , int destClientID = 0
         , bool isArchived = false) = 0;
      /// <summary>
      /// Sends a Subscription
      /// </summary>
      /// <param name="clientType">type of client to subscribe to</param>
      /// <param name="clientID">ID of the client to subscribe to</param>
      /// <param name="subscribe">true to subscribe, false to unsubscribe</param>
      virtual bool Subscribe(CommonMessages::ClientTypes clientType, int clientID = 0, bool subscribe = true) = 0;
   };

};
}
}
