#ifndef _WIN32
#include <unistd.h>
#endif
#include <stdlib.h>
#include <string.h>
#include <iostream>

#include "ArgumentParser.h"
#include "Logger.h"
#include "HandleSignals.h"
#include "TestClientThread.h"
#include "StringUtils.h"

/**
Main application loop

@Author JLitton
*/

// Keystroke values used only in debug mode
#define KS_TOGGLE_SEND   'd'
#define KS_TOGGLE_SUBSCRIBE    's'
#define KS_DIAGNOSTICS 'D'
#define KS_HALT         'X'
#define KS_HELP         '?'
#define KS_ENTER        0x0a  // The <enter> key

bool g_bShutDown;

using namespace Matrix::Common;

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
   int clienttype = 1;
   int clientID = 100;
   int freq = 500;
   std::string ipAddress = "127.0.0.1";
   if (parseValues.mDisplayVersion)
   {
      std::cout << "Version: 1.0";
      quit = true;
   }
   if (parseValues.mDisplayHelp)
   {
      std::cout <<
         parseValues.GetHelpString() <<
         "--ipaddress=n      : Sets the server ip address.  (default = " << ipAddress << ")." << std::endl <<
         "--port=n           : Sets the server port. (default = " << port << ")." << std::endl <<
         "--clienttype=n     : Sets the client type. (default = " << clienttype << ")." << std::endl <<
         "--clientid=n       : Sets the client ID. (default = " << clientID << ")." << std::endl <<
         "--freq=n           : Sets the requency of door requests in ms. (default = " << freq << ")." << std::endl;
      quit = true;
   }
   if (quit)
      return 0;

   std::cout << '\n';
   //look for additional command line arguments
   if (argc > 1)
   {
      for (int index = 0; index < argc; index++)
      {
         int val;
         std::string value;
         if (ArgumentParser::ParseInt32Flag(argv[index], "port", &val))
         {
            if (val > 0)
               port = (uint16_t)val;
         }
         else if (ArgumentParser::ParseStringFlag(argv[index], "ipaddress", &value))
         {
            if (value != "")
               ipAddress = value;
         }
         else if (ArgumentParser::ParseInt32Flag(argv[index], "clienttype", &val))
         {
            clienttype = val;
         }
         else if (ArgumentParser::ParseInt32Flag(argv[index], "clientid", &val))
         {
            clientID = val;
         }
         else if (ArgumentParser::ParseInt32Flag(argv[index], "freq", &val))
         {
            freq = val;
         }

      }
   }


   std::shared_ptr<TestApp::TestClientThread> mpCommThread = nullptr;

   //initialize everything
   try
   {
      Logging::Logger::SetGlobalLogger(std::unique_ptr<Logging::Logger>(
         new Logging::Logger("TEST_APP", Logging::IntToLogLevel(parseValues.mLogLevel), parseValues.mIsConsole, parseValues.mLogToFile)));

      mpCommThread = std::make_shared<TestApp::TestClientThread>(ipAddress, StringUtils::Format("%d", port), clienttype, clientID, freq);
      mpCommThread->StartThread();

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
                  mpCommThread->ShutDown();
                  break;
               }
               case KS_DIAGNOSTICS:
               {
                  mpCommThread->ShowDiagnostics();
                  break;
               }
               case KS_TOGGLE_SEND:
               {
                  mpCommThread->ToggleSendMessages();
                  break;
               }
               case KS_TOGGLE_SUBSCRIBE:
               {
                  mpCommThread->ToggleSubscribe();
                  break;
               }
               case KS_HELP:
               case KS_ENTER:
               {
                  std::cout <<
                     "CONSOLE COMMANDS:" << std::endl <<
                     "    " << KS_TOGGLE_SEND << ": toggle send messages" << std::endl <<
                     "    " << KS_TOGGLE_SUBSCRIBE << ": toggle subscribe" << std::endl <<
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
   if (mpCommThread)
   {
      mpCommThread->ShutDown();
      mpCommThread->WaitForShutdown(10, 10);
      mpCommThread = nullptr;
   }
   Logging::Logger::ClearGlobalLogger();

   return 0;
}

/** @}*/
