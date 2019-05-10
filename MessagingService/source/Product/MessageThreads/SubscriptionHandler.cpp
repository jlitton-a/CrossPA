#include "Logger.h"
#include "SubscriptionHandler.h"
#include "Message.h"
#include "MessageUtils.h"
#include "ClientMsgHandler.h"

using namespace Matrix::Common;
using namespace Matrix::MsgService::MessageThreads;

SubscriptionHandler::~SubscriptionHandler()
{
   ClearAll();
};

void SubscriptionHandler::AddSubscription(std::shared_ptr<IClientMsgHandler> pClient, CommonMessages::Subscribe& subscribeMsg)
{
   if (pClient != nullptr)
   {
      CommonMessages::SubscriptionParams index(subscribeMsg.clienttype(), subscribeMsg.clientid(), subscribeMsg.topic());
      std::lock_guard<std::mutex> lock(_lock);
      auto find = _msgLookup.find(index);
      if (find == _msgLookup.end())
      {
         std::vector <std::shared_ptr<IClientMsgHandler>> newVector;
         newVector.push_back(pClient);
         _msgLookup.insert(std::make_pair(index, newVector));
      }
      else
      {
         if (std::find(find->second.begin(), find->second.end(), pClient) == find->second.end())
            find->second.push_back(pClient);
      }
   }
}
void SubscriptionHandler::RemoveSubscription(IClientMsgHandler* pClient, CommonMessages::Subscribe& unsubscribeMsg)
{
   CommonMessages::SubscriptionParams index(unsubscribeMsg.clienttype(), unsubscribeMsg.clientid(), unsubscribeMsg.topic());
   std::lock_guard<std::mutex> lock(_lock);
   auto find = _msgLookup.find(index);
   if (find != _msgLookup.end())
   {
      auto& list = find->second;
      list.erase(std::remove_if(list.begin(), list.end(),
         [pClient](std::shared_ptr<IClientMsgHandler> pMyClient) { return pClient == pMyClient.get(); }), list.end());
   }
}
void SubscriptionHandler::RemoveSubscriptionsFor(IClientMsgHandler* pClient)
{
   int numRemoved = 0;
   std::unique_lock<std::mutex> lock(_lock);
   for (auto iter = _msgLookup.begin(); iter != _msgLookup.end(); ++iter)
   {
      auto& list = iter->second;
      list.erase(std::remove_if(list.begin(), list.end(),
               [pClient, &numRemoved](std::shared_ptr<IClientMsgHandler> pMyClient)
               {
                  if (pClient == pMyClient.get())
                  {
                     numRemoved++;
                     return true;
                  }
                  return false;
               }),
               list.end());

   }
   lock.unlock();
   LOG_MESSAGE(Logging::LogLevels::DEBUG_LVL) << "Removed " << numRemoved << " subscriptions for client " << pClient->GetName();
}

void SubscriptionHandler::ClearAll()
{
   std::lock_guard<std::mutex> lock(_lock);
   _msgLookup.clear();
}
void SubscriptionHandler::SendToSubscribers(IClientMsgHandler* pSentFrom, CommonMessages::Header& msg)
{
   std::unordered_set<std::shared_ptr<IClientMsgHandler>> sendList;
   CommonMessages::SubscriptionParams index;
   auto clientType = pSentFrom->GetClientType();
   auto clientID = pSentFrom->GetClientID();

   //find all the clients that are subscribed to this type of message
   //scope for lock_guard
   {
      std::lock_guard<std::mutex> lock(_lock);
      for (auto pair : _msgLookup)
      {
         if (pair.first._clientType == clientType || pair.first._clientType == 0)
         {
            if (pair.first._clientID == clientID || pair.first._clientID == 0)
            {
               if (pair.first._topic == 0 || pair.first._topic == (int)msg.topic())
               {
                  for (auto pClient : pair.second)
                  {
                     sendList.insert(pClient);
                  }
               }
            }
         }
      }
   }

   if (msg.origclienttype() == 0)
   {
      msg.set_origclienttype(clientType);
      msg.set_origclientid(clientID);
   }
   //now send the msg to the clients we found
   if (sendList.size() > 0)
   {
      LOG_MESSAGE(Logging::LogLevels::TRACE_LVL) << "Sending msg " << CommonMessages::MessageUtils::ToString(msg.msgtypeid()) << " to subscibers";
      for (auto pClient : sendList)
      {
         if (pClient.get() != pSentFrom)
         {
            pClient->SendMsg(msg);
         }
      }
   }
}
std::vector<CommonMessages::SubscriptionParams> SubscriptionHandler::GetSubscribersTo(int clientType, int clientID)
{
   std::lock_guard<std::mutex> lock(_lock);
   std::vector<CommonMessages::SubscriptionParams> paramsList;
   for (auto pair : _msgLookup)
   {
      if (pair.first._clientType == clientType && pair.first._clientID == clientID)
      {
         for (auto pSubscriber : pair.second)
         {
            CommonMessages::SubscriptionParams param(pSubscriber->GetClientType(), pSubscriber->GetClientID(), pair.first._topic);
            paramsList.push_back(param);
         }
      }
   }
   return paramsList;
}
