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
#include "CommHandler.h"

namespace asio = boost::asio;
namespace CommunicationUtils = Matrix::MsgService::CommunicationUtils;

namespace Matrix
{
namespace MsgService
{
namespace MessageThreads
{
   class ClientManager;

   /// <summary>
   /// Interface to allow unit test mocking
   /// </summary>
   class IClientMsgHandler : public virtual CommunicationUtils::ICommHandler
   {
   public:
      virtual ~IClientMsgHandler() {}
      /// <summary>
      /// Returns the type for this client.
      /// </summary>
      /// <returns>The type of this client</returns>
      virtual int GetClientType() = 0;
      /// <summary>
      /// Returns the ID for this client.
      /// </summary>
      /// <returns>The ID of this client</returns>
      virtual int GetClientID() = 0;
   };

   /// <summary>
   /// Class that handles messages received from a client
   /// </summary>
   class ClientMsgHandler : public CommunicationUtils::CommHandler, public virtual IClientMsgHandler
   {
      //****************************************
      // Constructors/Destructors
      //****************************************
   public:
      using CommunicationUtils::CommHandler::GetName;
      using CommunicationUtils::CommHandler::SendMsg;
      /// <summary>
      /// Initializes a new instance of the <see cref="ClientMsgHandler"/> class.
      /// </summary>
      /// <param name="ioContext">io_context to use</param>
      /// <param name="pClientManager">ClientManager and subscription manager</param>
      MESSAGETHREADS_API ClientMsgHandler(std::shared_ptr<CommunicationUtils::IContextHandler> pContextHandler, std::shared_ptr<ClientManager> pClientManager);

      /// <summary>
      /// Standard destructor
      /// </summary>
      MESSAGETHREADS_API virtual ~ClientMsgHandler();

      //****************************************
      // Fields
      //****************************************
   private:
      bool _isAuthenticated;
      std::shared_ptr<ClientManager> _pClientManager;

      //****************************************
      // Methods
      //****************************************
   public:
      /// <summary>
      /// Returns the type for this client.
      /// </summary>
      /// <returns>The type of this client</returns>
      MESSAGETHREADS_API virtual int GetClientType() override { return _clientType; }
      /// <summary>
      /// Returns the ID for this client.
      /// </summary>
      /// <returns>The ID of this client</returns>
      MESSAGETHREADS_API virtual int GetClientID() override { return _clientID; }
      /// <summary>
      /// Returns the Name for this client (mostly used for logging purposes.)
      /// </summary>
      /// <returns>The Name of this client</returns>
      MESSAGETHREADS_API virtual std::string GetName() override;
      /// <summary>
      /// Returns true if this client is logged on and authenticated
      /// </summary>
      /// <returns>true if the client has been authenticated</returns>
      MESSAGETHREADS_API bool IsAuthenticated() { return _isAuthenticated; }

      MESSAGETHREADS_API virtual std::string GetDiagnosticsInfo() override;

   protected:
      /// <summary>
      /// Override to handle messages that are received.
      /// </summary>
      /// <param name="pMsg">The message that was received</param>
      MESSAGETHREADS_API virtual void HandleMessageReceived(std::unique_ptr<CommonMessages::Header> pMsg) override;
      /// <summary>
      /// Override to perform additional steps when disconnect has completed.
      /// </summary>
      /// <param name="reason">The reason for the disconnect</param>
      MESSAGETHREADS_API virtual void HandleDisconnect(CommunicationUtils::DisconnectReason reason) override;
      /// <summary>
      /// Sends pMsg to subscribers
      /// </summary>
      /// <param name="pMsg">The message to send</param>
      MESSAGETHREADS_API void SendMessageToSubscribers(CommonMessages::Header* pMsg);

   private:
      //needed to to have shared_from_this work for derived classes 
      std::shared_ptr<ClientMsgHandler> shared_from_this() { return shared_from(this); }
      std::shared_ptr<const ClientMsgHandler> shared_from_this() const { return shared_from(this); }
   };

}
}
}
#ifdef _WIN32
#pragma warning( pop )
#endif
