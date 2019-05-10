#pragma once
#include <memory>
#include <unordered_map>
#include <mutex>
#include <algorithm>

#include "../stdafx.h"
#include "SubscriptionParams.h"

namespace CommonMessages = Matrix::MsgService::CommonMessages;

namespace Matrix
{
namespace MsgService
{
   namespace CommonMessages
   {
      class Subscribe;
      class Header;
   }

namespace MessageThreads
{
   class IClientMsgHandler;

   class SubscriptionHandler
   {
      //****************************************
      // Constructors/Destructors
      //****************************************
   public:
      MESSAGETHREADS_API SubscriptionHandler()
      {}
      MESSAGETHREADS_API ~SubscriptionHandler();

      //****************************************
      // Fields
      //****************************************
   private:
      std::mutex _lock;
      std::unordered_map<CommonMessages::SubscriptionParams, std::vector<std::shared_ptr<MessageThreads::IClientMsgHandler>>, CommonMessages::SubscriptionParamsHasher> _msgLookup;

      //****************************************
      // Methods
      //****************************************
   public:
      MESSAGETHREADS_API void AddSubscription(std::shared_ptr<IClientMsgHandler> pClient, CommonMessages::Subscribe& subscribeMsg);
      MESSAGETHREADS_API void RemoveSubscription(IClientMsgHandler* pClient, CommonMessages::Subscribe& unsubscribeMsg);
      MESSAGETHREADS_API void RemoveSubscriptionsFor(MessageThreads::IClientMsgHandler* pClient);
      MESSAGETHREADS_API void ClearAll();
      MESSAGETHREADS_API void SendToSubscribers(IClientMsgHandler* pSentFrom, CommonMessages::Header& msg);
      MESSAGETHREADS_API std::vector<CommonMessages::SubscriptionParams> GetSubscribersTo(int clientType, int clientID);
   };

}
}
}