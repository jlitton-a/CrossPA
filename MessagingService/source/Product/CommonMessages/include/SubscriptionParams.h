#pragma once
#include <functional>

namespace Matrix
{
namespace MsgService
{
namespace CommonMessages
{
   struct SubscriptionParams
   {
   public:
      SubscriptionParams(int clientType = 0, int clientID = 0, int topic = 0)
      {
         _clientType = clientType;
         _clientID = clientID;
         _topic = topic;
      };
      bool operator==(const SubscriptionParams &other) const
      {
         return (_clientType == other._clientType
            && _clientID == other._clientID
            && _topic == other._topic);
      }
   public:
      int _clientType;
      int _clientID;
      int _topic;
   };
   struct SubscriptionParamsHasher
   {
      std::size_t operator()(const SubscriptionParams& k) const
      {
         using std::size_t;
         using std::hash;
         using std::string;

         return ((hash<int>()(k._clientType)
            ^ (hash<int>()(k._clientID)
               ^ (hash<int>()(k._topic) << 1)) >> 1))
            ;
      }
   };
}
}
}