#pragma once
#include <memory>
#include <boost/asio.hpp>

#include "../stdafx.h"
#include "WorkerThread.h"
#include "CommHandlerSignals.h"

namespace asio = boost::asio;
using namespace boost::asio::ip;

namespace Matrix
{
namespace MsgService
{
namespace CommunicationUtils
{
   enum class DiagnosticTypes
   {
      Start,
      Stop,
      Display
   };
   /// <summary>
   /// Interface to allow unit test mocking
   /// </summary>
   class ICommHandler : public virtual Matrix::Common::IWorkerThread
   {
   public:
      virtual ~ICommHandler() {}
      virtual bool SendMsg(CommonMessages::Header& msg) = 0;
      virtual bool IsConnected() = 0;
      virtual void Disconnect() = 0;
      virtual void Diagnostics(DiagnosticTypes type) = 0;
      virtual std::string GetDiagnosticsInfo() = 0;
      virtual ConnectionChangeConnection AddConnectionChangeObserver(const ConnectionChangeCallback& callback) = 0;
      virtual SocketStateChangeConnection AddSocketStateChangeObserver(const SocketStateChangeCallback& callback) = 0;
      virtual MessageRxConnection AddMessageRxObserver(const MessageRxCallback& callback) = 0;
   };

}
}
}