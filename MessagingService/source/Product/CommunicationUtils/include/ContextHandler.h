#pragma once
//disable Inherits Via Dominance warning
#ifdef _WIN32
#pragma warning( push )
#pragma warning( disable: 4250)
#endif

#include <memory>
#include <boost/asio.hpp>

#include "../stdafx.h"
#include "WorkerThread.h"
#include "IContextHandler.h"

namespace asio = boost::asio;

namespace Matrix
{
namespace MsgService
{
namespace CommunicationUtils
{
   /// <summary>
   /// Class for creating an asio IOContext and running it in a thread.
   /// </summary>
   class ContextHandler :  public Matrix::Common::WorkerThread, public virtual IContextHandler
   {
      //****************************************
      // Constructors/Destructors
      //****************************************
#ifdef USING_SSL
   private:
      boost::asio::ssl::context& _sslContext;
   public:
      /// <summary>
      /// Creates the context handler and sets the log level
      /// </summary>
      COMMUNICATIONUTILS_API ContextHandler(
            , boost::asio::ssl::context& sslContext);

      /// <summary>
      /// Gets the SSL Context
      /// </summary>
      COMMUNICATIONUTILS_API boost::asio::ssl::context& GetSSLContext() override;
#else
   public:
      COMMUNICATIONUTILS_API ContextHandler();
#endif
      COMMUNICATIONUTILS_API virtual ~ContextHandler() {};
      //****************************************
      // Fields
      //****************************************
   private:
      boost::asio::io_context _ioContext;
      boost::asio::executor_work_guard<boost::asio::io_context::executor_type> _workGuard;

      //****************************************
      // Methods
      //****************************************
   public:
      using Matrix::Common::WorkerThread::StartThread;
      /// <summary>
      /// Gets the IOContext
      /// </summary>
      COMMUNICATIONUTILS_API boost::asio::io_context& GetIOContext() override;
      /// <summary>
      /// Stops the IOContext and shuts down the thread
      /// </summary>
      COMMUNICATIONUTILS_API void ShutDown() override;

   private:
      void Run() override;
   };
}
}
}
#ifdef _WIN32
#pragma warning( pop )
#endif
