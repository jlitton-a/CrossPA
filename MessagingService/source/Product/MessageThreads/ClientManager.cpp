#include "Logger.h"
#include "ClientManager.h"
#include "SubscriptionHandler.h"
#include "StringUtils.h"

using namespace Matrix::Common;
using namespace Matrix::MsgService::MessageThreads;

ClientManager::ClientManager(int freqMS) : TimedThread("ClientManager", freqMS)
      , _pSubscriptionHandler(std::make_shared<SubscriptionHandler>())
{}

ClientManager::~ClientManager()
{
   ShutDown();
   WaitForShutdown(100, 10);
   _clients.clear();
   _pSubscriptionHandler->ClearAll();
   _pSubscriptionHandler = nullptr;
}
void ClientManager::ShutDown()
{
   if (!_shuttingDown)
   {
      TimedThread::ShutDown();
      ClearAll();
      _shutdownComplete = true;
   }
}
void ClientManager::ClearAll()
{
   LOG_MESSAGE(Logging::LogLevels::DEBUG_LVL) << "Closing " << _clients.size() << " clients!";

   std::lock_guard<std::mutex> lock(_lock);
   for (auto client : _clients)
   {
      client->ShutDown();
      client->WaitForShutdown(100, 10);
   }
}

void ClientManager::Diagnostics(Matrix::MsgService::CommunicationUtils::DiagnosticTypes )
{
   std::lock_guard<std::mutex> lock(_lock);
   std::string msg;
   msg = "\n===========DIAGNOSTICS===========";
   if (_clients.size() == 0)
      msg = "\nNo clients connected.";
   else
   {
      for (auto client : _clients)
      {
         msg += StringUtils::Format("\n%s", client->GetDiagnosticsInfo().c_str());
      }
   }
   msg += "\n=================================";
   LOG_MESSAGE(Logging::LogLevels::NO_LVL) << msg;
}

void ClientManager::AddClient(std::shared_ptr<ClientMsgHandler> pClient)
{
   std::lock_guard<std::mutex> lock(_lock);
   _clients.push_back(pClient);
}

std::shared_ptr<ClientMsgHandler> ClientManager::GetClient(int clientTypeID, int clientID)
{
   std::lock_guard<std::mutex> lock(_lock);
   auto it = find_if(_clients.begin(), _clients.end(),
         [clientTypeID, clientID](std::shared_ptr<ClientMsgHandler> pClient) -> bool {
               return pClient->GetClientType() == clientTypeID && pClient->GetClientID() == clientID;
         });
   if (it != _clients.end())
      return *it;
   else
      return nullptr;
}

bool ClientManager::ExecuteLoopActions()
{
   std::lock_guard<std::mutex> lock(_lock);
   _clients.erase(
         std::remove_if(
            _clients.begin(),
            _clients.end(),
            [](std::shared_ptr<ClientMsgHandler> pClient) -> bool {
               return pClient->IsShutdownComplete();
            }
         ),
         _clients.end());
   return true;

}