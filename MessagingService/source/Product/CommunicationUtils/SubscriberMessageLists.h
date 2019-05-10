#pragma once
#include <memory>

#include "../stdafx.h"
#include "ISubscriberMessageLists.h"

namespace Matrix
{
namespace MsgService
{
namespace CommunicationUtils
{
   /// <summary>
   /// maintains lists of messages that need to be acked
   /// </summary>
   class SubscriberMessageLists : public ISubscriberMessageLists
   {
   public:
      /// <summary>
      /// Adds a subscription to the lists
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <returns>true if it was added, false if it was already there</returns>
      virtual bool AddSubscription(int clientType, int clientID) override;
      /// <summary>
      /// Removes a subscription from the lists
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <returns>true if it was removed, false if it was not there</returns>
      virtual bool RemoveSubscription(int clientType, int clientID) override;
      /// <summary>
      /// Add a message to each subscribers list to wait for an ack
      /// </summary>
      /// <param name="msg">message to add</param>
      /// <returns>true if the message was added</returns>
      virtual bool AddSentMessage(const Matrix::MsgService::CommonMessages::Header msg) override;
      /// <summary>
      /// Removes each message with a key in ackKeys from the subscriber
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <param name="ackKeys">List of keys that have been acked</param>
      /// <returns>true if the message was removed</returns>
      virtual bool RemoveSentMessages(int clientType, int clientID, const google::protobuf::RepeatedField<int>& ackKeys) override;
      /// <summary>
      /// Remove a message with msgKey from the subscriber
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <param name="msgKey">The key of the message that has been acked</param>
      /// <returns>true if the message was removed</returns>
      virtual bool RemoveSentMessage(int clientType, int clientID, int msgKey) override;

      /// <summary>
      /// Gets a list of messages that were last sent more than numSeconds ago
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <param name="numSeconds">Number of seconds since they were last sent</param>
      /// <returns>list of messages that were last sent more than numSeconds ago</returns>
      virtual std::vector<Matrix::MsgService::CommonMessages::Header> GetMessages(int clientType, int clientID, int numSeconds) override;

      /// <summary>
      /// Adds msg.msgKey to the list of msgKeys that need to be acked
      /// for each subscribing client
      /// </summary>
      /// <param name="msg">the message to add to the list of keys that need to be acked</param>
      virtual void AddToNeedToAckList(const Matrix::MsgService::CommonMessages::Header* const pMsg);
      /// <summary>
      /// Removes msg keys from the list of msg keys that need to be acked
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <param name="msgKeyList">msg keys to remove</param>
      virtual void RemoveFromNeedToAckList(int clientType, int clientID, std::vector<int> msgKeyList) override;
      /// <summary>
      /// Gets the list of msg keys that need to be acked
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <returns>list of msg keys that need to be acked</returns>
      virtual std::vector<int> GetNeedToAckList(int clientType, int clientID) override;
      /// <summary>
      /// Sets/clears the online flag for this client
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      /// <param name="isOnline">true to set online, false to set offline</param>
      void SetClientOnLine(int clientType, int clientID, bool isOnline) override;
      /// <summary>
      /// returns the online flag for this client
      /// </summary>
      /// <param name="clientType">type of client</param>
      /// <param name="clientID">id of client</param>
      bool IsClientOnline(int clientType, int clientID) override;

   };

}
}
}