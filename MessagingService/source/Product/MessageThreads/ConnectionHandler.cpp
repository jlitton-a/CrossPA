
#include "ConnectionHandler.h"
#include "Logger.h"
#include "ClientMsgHandler.h"

using namespace Matrix::Common;
using namespace Matrix::MsgService::MessageThreads;

ConnectionHandler::ConnectionHandler(std::shared_ptr<CommunicationUtils::IContextHandler> pContextHandler, unsigned short port, std::shared_ptr<ClientManager> pClientManager) : WorkerThread("ConnectionHandler")
      , _pContextHandler(pContextHandler)
      , _port(port)
      , _acceptor(pContextHandler->GetIOContext(), asio::ip::tcp::endpoint(asio::ip::tcp::v4(), port))
      , _pClientConnection(nullptr)
      , _pClientManager(pClientManager)
{
}

void ConnectionHandler::Run()
{
   LOG_MESSAGE(Logging::LogLevels::INFO_LVL) << "Server starting on port " << _port << ".";
   StartAccept();
   _pContextHandler->GetIOContext().run();
}

void ConnectionHandler::ShutDown()
{
   if (!_shuttingDown)
   {
      WorkerThread::ShutDown();
      try
      {
         _acceptor.close();
         if (_pClientConnection != nullptr)
         {
            _pClientConnection->ShutDown();
            _pClientConnection = nullptr;
         }
         if (_pClientManager)
         {
            _pClientManager = nullptr;
         }
         _shutdownComplete = true;
      }
      catch (std::exception ex)
      {
         LOG_MESSAGE(Logging::LogLevels::EXCEPTION_LVL) << "Exception closing ASIO ServerThread - " << ex.what();
      }
   }
}
void ConnectionHandler::StartAccept()
{
   if (!_shuttingDown)
   {
      LOG_MESSAGE(Logging::LogLevels::DEBUG_LVL) << "Waiting for client.";
      // Create a new client for the next connection to use.
      _pClientConnection = std::make_shared<ClientMsgHandler>(_pContextHandler, _pClientManager);

      // Asynchronously wait to accept a new client
      //
      _acceptor.async_accept(_pClientConnection->GetSocket()->lowest_layer(),
         std::bind(&ConnectionHandler::HandleAccept, shared_from_this(), _pClientConnection, std::placeholders::_1));
   }
}
void ConnectionHandler::HandleAccept(
      std::shared_ptr<ClientMsgHandler> pClientConnection,
      const boost::system::error_code& error)
{
   if (!_shuttingDown)
   {
      if (error)
      {
         LOG_MESSAGE(Logging::LogLevels::EXCEPTION_LVL) << "HandleAccept error - " << error.message();
      }
      // A new client has connected
      else
      {
         if (pClientConnection != nullptr)
         {
            LOG_MESSAGE(Logging::LogLevels::DEBUG_LVL) << "Client connected.";
            if (_pClientManager)
               _pClientManager->AddClient(pClientConnection);

            // Start the connection
            //
            pClientConnection->Run();
         }
      }
      // Accept another client
      //
      StartAccept();
   }
}
