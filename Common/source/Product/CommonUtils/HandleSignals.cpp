#include <signal.h>
#include <HandleSignals.h>

static volatile bool got_sigint = false;
static volatile bool got_sighup = false;
static volatile bool already_hooked_up = false;

namespace Matrix
{
   namespace Common
   {
      namespace HandleSignals
      {
         std::condition_variable shutdown_signal;
         std::mutex shutdown_mutex;
      }
   }
}

using namespace Matrix::Common;
using namespace HandleSignals;

void handle_signal(int signal)
{
   switch (signal)
   {
#ifdef _WIN32
      case SIGTERM:
      case SIGABRT:
      case SIGBREAK:
#else
      case SIGHUP:
#endif
      {
         got_sighup = true;
         std::unique_lock<std::mutex> locker(shutdown_mutex);
         shutdown_signal.notify_all();
         break;
      }
      case SIGINT:
      {
         std::unique_lock<std::mutex> locker(shutdown_mutex);
         shutdown_signal.notify_all();
         got_sigint = true;
         break;
      }
   }
}

void HandleSignals::HookupHandler()
{
   if (already_hooked_up)
   {
      return;
   }
   already_hooked_up = true;
#ifdef _WIN32
   signal(SIGINT, handle_signal);
   signal(SIGTERM, handle_signal);
   signal(SIGABRT, handle_signal);
#else
   struct sigaction sa;
   // Setup the handler
   sa.sa_handler = &handle_signal;
   // Restart the system call, if at all possible
   sa.sa_flags = SA_RESTART;
   // Block every signal during the handler
   sigfillset(&sa.sa_mask);
   // Intercept SIGHUP and SIGINT
   if (sigaction(SIGHUP, &sa, NULL) == -1)
   {
      fputs("Cannot install SIGHUP handler.", stderr);
   }
   if (sigaction(SIGINT, &sa, NULL) == -1)
   {
      fputs("Cannot install SIGINT handler.", stderr);
   }
#endif
}
void HandleSignals::UnhookHandler()
{
   if (already_hooked_up)
   {
#ifdef _WIN32
      signal(SIGINT, SIG_DFL);
      signal(SIGTERM, SIG_DFL);
      signal(SIGABRT, SIG_DFL);
#else
      struct sigaction sa;
      // Setup the sighub handler
      sa.sa_handler = SIG_DFL;
      // Restart the system call, if at all possible
      sa.sa_flags = SA_RESTART;
      // Block every signal during the handler
      sigfillset(&sa.sa_mask);
      // Intercept SIGHUP and SIGINT
      if (sigaction(SIGHUP, &sa, NULL) == -1)
      {
         fputs("Cannot uninstall SIGHUP handler.", stderr);
      }
      if (sigaction(SIGINT, &sa, NULL) == -1)
      {
         fputs("Cannot uninstall SIGINT handler.", stderr);
      }
#endif

      already_hooked_up = false;
   }
}

bool HandleSignals::WasSignalReceived()
{
   return got_sighup || got_sigint;
}

// Return true iff a SIGINT has been received since the last time this
// function was called.
bool HandleSignals::GotSIGINT()
{
   bool result = got_sigint;
   got_sigint = false;
   return result;
}

// Return true iff a SIGHUP has been received since the last time this
// function was called.
bool HandleSignals::GotSIGHUP()
{
   bool result = got_sighup;
   got_sighup = false;
   return result;
}
