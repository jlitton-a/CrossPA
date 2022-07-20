#include "MessageUtils.h"
#include "SubscriberMessageLists.h"

using namespace Matrix::MsgService::CommunicationUtils;

bool SubscriberMessageLists::AddSubscription(int /*clientType*/, int /*clientID*/)
{
   return false;
}
bool SubscriberMessageLists::RemoveSubscription(int /*clientType*/, int /*clientID*/)
{
   return false;
}

bool SubscriberMessageLists::AddSentMessage(const Matrix::MsgService::CommonMessages::Header msg) 
{
   return false;
}

bool SubscriberMessageLists::RemoveSentMessages(int /*clientType*/, int /*clientID*/, const google::protobuf::RepeatedField<int>& /*ackKeys*/)
{
   return false;
}

bool SubscriberMessageLists::RemoveSentMessage(int /*clientType*/, int /*clientID*/, int /*msgKey*/)
{
   return false;
}

std::vector<Matrix::MsgService::CommonMessages::Header> SubscriberMessageLists::GetMessages(int /*clientType*/, int /*clientID*/, int /*numSeconds*/)
{
   std::vector<Matrix::MsgService::CommonMessages::Header> list;
   return list;
}

void SubscriberMessageLists::AddToNeedToAckList(const Matrix::MsgService::CommonMessages::Header* const /*pMsg*/)
{
}
void SubscriberMessageLists::RemoveFromNeedToAckList(int /*clientType*/, int /*clientID*/, std::vector<int> /*msgKeyList*/)
{
}
std::vector<int> SubscriberMessageLists::GetNeedToAckList(int /*clientType*/, int /*clientID*/)
{
   std::vector<int> list;
   return list;
}

/// <summary>
/// Sets/clears the online flag for this client
/// </summary>
/// <param name="clientType">type of client</param>
/// <param name="clientID">id of client</param>
/// <param name="isOnline">true to set online, false to set offline</param>
void SubscriberMessageLists::SetClientOnLine(int /*clientType*/, int /*clientID*/, bool /*isOnline*/)
{
}

/// <summary>
/// returns the online flag for this client
/// </summary>
/// <param name="clientType">type of client</param>
/// <param name="clientID">id of client</param>
bool SubscriberMessageLists::IsClientOnline(int /*clientType*/, int /*clientID*/)
{
   return false;
}
