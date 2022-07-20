#include "IncludeBoostASIO.h"
#include <memory>

#include "ContextHandler.h"
#include "Logger.h"

using namespace Matrix::Common;
using namespace Matrix::MsgService::CommunicationUtils;

#ifdef USING_SSL
ContextHandler::ContextHandler(
      , boost::asio::ssl::context& sslContext
      )
      : WorkerThread("ContextHandler")
      , _sslContext(sslContext)
#else
ContextHandler::ContextHandler()
   : WorkerThread("ContextHandler")
#endif
   , _workGuard(boost::asio::make_work_guard(_ioContext))
{
}
boost::asio::io_context& ContextHandler::GetIOContext()
{ 
   return _ioContext; 
}
#ifdef USING_SSL
boost::asio::ssl::context& ContextHandler::GetSSLContext()
{
   return _sslContext;
}
#endif

void ContextHandler::ShutDown()
{
   WorkerThread::ShutDown();
   _ioContext.stop();
}
void ContextHandler::Run()
{
   for (;;)
   {
      try
      {
         _ioContext.run();
         break; // run() exited normally
      }
      catch (std::exception& e)
      {
         // Deal with exception as appropriate.
         LOG_MESSAGE(Logging::LogLevels::EXCEPTION_LVL) << "Error in _ioContext.run: " << e.what();
      }
   };
   _shutdownComplete = true;
}
