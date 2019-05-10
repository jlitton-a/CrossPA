#pragma once
#include <memory>

#include "../stdafx.h"
#include "TimedThread.h"
#include "ClientMsgHandler.h"

namespace CommunicationUtils = Matrix::MsgService::CommunicationUtils;

namespace Matrix
{
namespace MsgService
{
namespace MessageThreads
{
   class SubscriptionHandler;

   class ClientManager : public Matrix::Common::TimedThread
   {
      //****************************************
      // Constructors/Destructors
      //****************************************
   public:
      /// <summary>
      /// Initializes a new instance of the <see cref="ClientManager"/> class.
      /// </summary>
      /// <param name="freqMS">Frequency to check for removing dead clients.</param>
      MESSAGETHREADS_API ClientManager(int freqMS = 1000);

      MESSAGETHREADS_API ~ClientManager();

      //****************************************
      // Fields
      //****************************************
   private:
      std::vector<std::shared_ptr<ClientMsgHandler>> _clients;
      std::mutex _lock;
      std::shared_ptr<SubscriptionHandler> _pSubscriptionHandler;
      //****************************************
      // Methods
      //****************************************
   public:
      /// <summary>
      /// Returns the subscription handler.
      /// </summary>
      /// <returns>the subscription handler <see cref="SubscriptionHandler"/></returns>
      std::shared_ptr<SubscriptionHandler> GetSubscriptionHandler() { return _pSubscriptionHandler; }
      /// <summary>
      /// Adds a client to the list.
      /// </summary>
      /// <param name="pClient">The client to add <see cref="ClientMsgHandler"/></param>
      void AddClient(std::shared_ptr<ClientMsgHandler> pClient);
      /// <summary>
      /// Clears out all the clients from the list.
      /// </summary>
      void ClearAll();
      /// <summary>
      /// Prints out diagnostic information.
      /// </summary>
      MESSAGETHREADS_API void Diagnostics(CommunicationUtils::DiagnosticTypes type);
      /// <summary>
      /// Shuts down the thread that checks for and removes disconnected clients
      /// </summary>
      void ShutDown() override;
      /// <summary>
      /// Finds and returns the specified client if it is in the list
      /// </summary>
      /// <returns>the the specified client if it is in the list, nullptr otherwise <see cref="ClientMsgHandler"/></returns>
      std::shared_ptr<ClientMsgHandler> GetClient(int clientTypeID, int clientID);

   protected:
      virtual bool ExecuteLoopActions() override;

   private:
   };

}
}
}