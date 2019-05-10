#ifndef _WIN32
#include <unistd.h>
#endif
#include <stdlib.h>
#include <string.h>
#include <iostream>
#include <memory>
#include <vector>

#include "ArgumentParser.h"
#include "Logger.h"
#include "HandleSignals.h"
#include "ConnectionHandler.h"
#include "ClientManager.h"
#include "ContextHandler.h"

/**
Main application loop

@Author JLitton
*/

// Keystroke values used only in debug mode
#define KS_HALT       'X'
#define KS_HELP       '?'
#define KS_DIAGNOSTICS 'D'
#define KS_ENTER      0x0a  // The <enter> key

bool g_bShutDown;

using namespace Matrix::Common;
namespace MessageThreads = Matrix::MsgService::MessageThreads;
namespace CommunicationUtils = Matrix::MsgService::CommunicationUtils;

//******************************************************************
int main(
      int      argc,    ///< The number of command line parameters
      char *   argv[]   ///< The command line parameters - the array will have argc entries
)
{
   ParseValues parseValues;
   parseValues.ParseArgumentValues(argc, argv, (int)Logging::LogLevels::DEFAULT_LOG_LEVEL);
   bool quit = false;
   uint16_t port = 8888;
   int freq = 500;
   if (parseValues.mDisplayVersion)
   {
      std::cout << "Version: 1.0";
      quit = true;
   }
   if (parseValues.mDisplayHelp)
   {
      std::cout <<
         parseValues.GetHelpString() <<
         "--console          : enter interactive console mode." << std::endl <<
         "--port=n           : Sets the port for listening. (default = " << port << ")." << std::endl <<
         "--freq=n           : Sets the requency of checking for client disconnects in ms. (default = " << freq << ")." << std::endl;
      quit = true;
   }
   if (quit)
      return 0;

   //look for additional command line arguments
   if (argc > 1)
   {
      for (int index = 0; index < argc; index++)
      {
         int val;
         if (ArgumentParser::ParseInt32Flag(argv[index], "port", &val))
         {
            if (val > 0)
               port = (uint16_t)val;
         }
         else if (ArgumentParser::ParseInt32Flag(argv[index], "freq", &val))
         {
            freq = val;
         }
      }
   }
   Logging::Logger::SetGlobalLogger(std::unique_ptr<Logging::Logger>(
         new Logging::Logger("MsgService", Logging::IntToLogLevel(parseValues.mLogLevel), parseValues.mIsConsole, parseValues.mLogToFile)));

#ifdef USING_SSL
   boost::asio::ssl::context sslContext(boost::asio::ssl::context::sslv23);
   sslContext.set_options(
         boost::asio::ssl::context::default_workarounds
         | boost::asio::ssl::context::no_sslv2
         | boost::asio::ssl::context::single_dh_use);
//   sslContext.set_password_callback(boost::bind(&server::get_password, this));
//   sslContext.use_certificate_chain_file("server.pem");
//   sslContext.use_private_key_file("server.pem", boost::asio::ssl::context::pem);
//   sslContext.use_tmp_dh_file("dh2048.pem");
   auto pContextHandler = std::make_shared<CommunicationUtils::ContextHandler>(sslContext);
#else
   auto pContextHandler = std::make_shared<CommunicationUtils::ContextHandler>();
#endif
   std::shared_ptr<MessageThreads::ConnectionHandler> pConnectionHandler = nullptr;
   auto pClientManager = std::make_shared<MessageThreads::ClientManager>(freq);
   //initialize everything
   try
   {
      pClientManager->StartThread();
      pConnectionHandler = std::make_shared<MessageThreads::ConnectionHandler>(pContextHandler, port, pClientManager);
      pConnectionHandler->StartThread();

      int nInput;
      int nInputCount = 0;

      if (parseValues.mIsConsole)
      {
         // Process commands from the console window
         while (g_bShutDown == false && !false) //HandleSignals::WasSignalReceived())
         {
            // getchar only returns once <cr> is entered, but we input 1 char at a time.
            // If we get more than a single character, read to the end, don't process,
            nInput = getchar();
            nInputCount = 1;

            if (nInput == -1)
            {
               g_bShutDown = true;
               break;
            }

            if (nInput != KS_ENTER)
            {
               while (getchar() != KS_ENTER)
                  nInputCount++;
            }

            if (nInputCount != 1)
            {
               continue;
            }

            switch (nInput)
            {
                  // 'X' key will terminate the app
               case KS_HALT:
               {
                  g_bShutDown = true;
                  LOG_MESSAGE(Logging::LogLevels::INFO_LVL) << "Server shutting down";
                  break;
               }
               case KS_DIAGNOSTICS:
               {
                  pClientManager->Diagnostics(CommunicationUtils::DiagnosticTypes::Display);
                  break;
               }
               case KS_HELP:
               case KS_ENTER:
               {
                  std::cout <<
                     "CONSOLE COMMANDS:" << std::endl <<
                     "    " << KS_DIAGNOSTICS << ": Diagnostics" << std::endl <<
                     "    " << KS_HELP << ": Help" << std::endl <<
                     "    " << KS_HALT << ": quit" << std::endl;
                  break;
               }
               default:
               {
                  char strInput[2];
                  strInput[0] = (char)nInput;
                  strInput[1] = '\0';
                  printf("Unknown Command %s.\n", strInput);
                  break;
               }
            }//switch on nInput
         }//while not shutdown
      }//if is console
      else
      {
         // If not using the interactive console, wait until notified of a shutdown signal.
         std::unique_lock<std::mutex> locker(HandleSignals::shutdown_mutex);
         HandleSignals::shutdown_signal.wait(locker, [&]()
                  { return HandleSignals::WasSignalReceived(); }
            );
      }
   }
   catch (std::exception & ex)
   {
      throw ex;
   }
   if (pConnectionHandler)
   {
      pConnectionHandler->ShutDown();
      pConnectionHandler->WaitForShutdown(5);
      pConnectionHandler = nullptr;
   }
   if (pClientManager != nullptr)
   {
      pClientManager->ShutDown();
      pClientManager->WaitForShutdown(5);
      pClientManager = nullptr;
   }
   if (pContextHandler != nullptr)
   {
      pContextHandler->ShutDown();
      pContextHandler->WaitForShutdown(5);
      pContextHandler = nullptr;
   }
   Logging::Logger::ClearGlobalLogger();

   //for (std::thread& t : threadPool) {
   //   t.join();
   //}
   return 0;
}

/** @}*/
