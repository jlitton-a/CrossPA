#pragma once

#include <boost/asio.hpp>
#include <memory>

#include "../stdafx.h"
#include "WorkerThread.h"
#include "ClientMsgHandler.h"
#include "ClientManager.h"
#include "IContextHandler.h"

namespace asio = boost::asio;
namespace CommunicationUtils = Matrix::MsgService::CommunicationUtils;

namespace Matrix
{
namespace MsgService
{
namespace MessageThreads
{
   /// <summary>
   /// Class that handles incoming connections by creating a ClientHandler and handing it off to a ClientManager
   /// </summary>
   class ConnectionHandler : public Matrix::Common::WorkerThread
   {
      //****************************************
      // Constructors/Destructors
      //****************************************
   public:
      /// <summary>
      /// Initializes a new instance of the <see cref="ConnectionHandler"/> class.
      /// </summary>
      /// <param name="ioContext">ASIO context</param>
      /// <param name="port">port on which to listen.</param>
      /// <param name="pClientManager">Connections will be handed off to this client manager.</param>
      MESSAGETHREADS_API ConnectionHandler(std::shared_ptr<CommunicationUtils::IContextHandler> pContextHandler, unsigned short port, std::shared_ptr<ClientManager> pClientManager);
      MESSAGETHREADS_API ~ConnectionHandler()
      {
         ShutDown();
      };

   private:
      ConnectionHandler();

      //****************************************
      // Fields
      //****************************************
   private:
      std::shared_ptr<CommunicationUtils::IContextHandler> _pContextHandler;
      unsigned short _port;
      asio::ip::tcp::acceptor _acceptor;
      std::shared_ptr<ClientMsgHandler> _pClientConnection;
      std::shared_ptr<ClientManager> _pClientManager;

      //****************************************
      // Methods
      //****************************************
   public:
      /// <summary>
      /// Signify to the thread to stop accepting connections
      /// </summary>
      MESSAGETHREADS_API virtual void ShutDown() override;
   protected:
      /// <summary>
      /// The thread Run function - starts accepting connections
      /// </summary>
      MESSAGETHREADS_API virtual void Run() override;
   private:
      void StartAccept();
      void HandleAccept(
         std::shared_ptr<ClientMsgHandler> pClientConnection,
         const boost::system::error_code& error);

      //needed to to have shared_from_this work for derived classes 
      std::shared_ptr<ConnectionHandler> shared_from_this() { return shared_from(this); }
      std::shared_ptr<const ConnectionHandler> shared_from_this() const { return shared_from(this); }
   };
}
}
}