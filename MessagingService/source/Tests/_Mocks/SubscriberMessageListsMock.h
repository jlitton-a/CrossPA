#pragma once
#include <gmock/gmock.h>
#include "ISubscriberMessageLists.h"

namespace CommunicationUtils = Matrix::MsgService::CommunicationUtils;

namespace testing
{
class SubscriberMessageListsMock : virtual public CommunicationUtils::ISubscriberMessageLists
{
public:
   SubscriberMessageListsMock() {};
   virtual ~SubscriberMessageListsMock() {};

   MOCK_METHOD2(AddSubscription, bool(int,int));
   MOCK_METHOD2(RemoveSubscription, bool(int, int));
   MOCK_METHOD1(AddSentMessage, bool(const Matrix::MsgService::CommonMessages::Header));
   MOCK_METHOD3(RemoveSentMessages, bool(int, int, const google::protobuf::RepeatedField<int>&));
   MOCK_METHOD3(RemoveSentMessage, bool(int, int, int));
   MOCK_METHOD3(GetMessages, std::vector<Matrix::MsgService::CommonMessages::Header>(int, int, int));
   MOCK_METHOD1(AddToNeedToAckList, void(const Matrix::MsgService::CommonMessages::Header* const));
   MOCK_METHOD3(RemoveFromNeedToAckList, void(int, int, std::vector<int>));
   MOCK_METHOD2(GetNeedToAckList, std::vector<int>(int, int));
   MOCK_METHOD3(SetClientOnLine, void(int, int, bool));
   MOCK_METHOD2(IsClientOnline, bool(int, int));

};
}