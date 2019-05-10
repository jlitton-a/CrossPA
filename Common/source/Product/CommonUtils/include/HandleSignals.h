#include "../stdafx.h"
#include <condition_variable>

namespace Matrix
{
namespace Common
{
   //TODO: use boost signals instead
   namespace HandleSignals
   {
      COMMONUTILS_API extern std::condition_variable shutdown_signal;
      COMMONUTILS_API extern std::mutex shutdown_mutex;

      /// <summary>
      /// Hookup a signal handler
      /// </summary>
      COMMONUTILS_API void HookupHandler();
      /// <summary>
      /// Unhook a signal handler
      /// </summary>
      COMMONUTILS_API void UnhookHandler();
      /// <summary>
      /// Returns true if any signal is received
      /// </summary>
      COMMONUTILS_API bool WasSignalReceived();
      /// <summary>
      /// Return true iff a SIGINT has been received since the last time this function was called
      /// </summary>
      COMMONUTILS_API bool GotSIGINT();
      /// <summary>
      /// Return true iff a SIGHUP has been received since the last time this function was called
      /// </summary>
      COMMONUTILS_API bool GotSIGHUP();
   }  // namespace HandleSignals
}
}