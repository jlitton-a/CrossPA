#pragma once

#include <memory>
#include "IncludeBoostASIO.h"
#ifdef USING_SSL
#include <boost/asio/ssl.hpp>
#endif

#include "../stdafx.h"
#include "LogLevels.h"
#include "WorkerThread.h"

namespace asio = boost::asio;

namespace Matrix
{
namespace MsgService
{
namespace CommunicationUtils
{
   class IContextHandler : public virtual Matrix::Common::IWorkerThread
   {
   public:
      using Matrix::Common::IWorkerThread::StartThread;
      virtual ~IContextHandler() {}
      virtual boost::asio::io_context& GetIOContext() = 0;
#ifdef USING_SSL
      virtual boost::asio::ssl::context& GetSSLContext() = 0;
#endif
   };
}
}
}
