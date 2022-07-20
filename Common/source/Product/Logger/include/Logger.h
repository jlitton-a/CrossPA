#pragma once

#include "../stdafx.h"

#ifdef _WIN32
#pragma warning( push )
#pragma warning( disable: 6031 26812 26451 6388 6285 26498 26110 26495 26439 26812)
#endif
#include <boost/filesystem.hpp>
#include <boost/log/expressions.hpp>
#include <boost/log/sinks.hpp>
#include <boost/log/sources/record_ostream.hpp>
#include <boost/log/sources/severity_logger.hpp>
#ifdef _WIN32
#pragma warning( pop )
#endif

#include <ostream>
#include <iostream>
#include <string>
#include <memory>
#include <mutex>

#include "LogLevels.h"
#ifdef _WIN32
static const int LOG_TO_FILE_DEFAULT = 0;
#else
static const int LOG_TO_FILE_DEFAULT = 1;
#endif

namespace Matrix
{
namespace Common
{
namespace Logging
{
   namespace sinks = boost::log::sinks;
   namespace sources = boost::log::sources;

   /// <summary>
   /// Send a human-readable text for LogLevels to the stream.
   /// </summary>
   /// <param name="strm">Stream to which to add the LogLevels text.</param>
   /// <param name="logLevel">The LogLevels value to add to the stream.</param>
   /// <returns>the stream.</returns>
   std::ostream& operator<< (std::ostream& Strm, const LogLevels& Level);

   /// <summary>
   /// Class for configuring the boost logging.
   /// </summary>
   class Logger
   {
   public:
      //****************************************
      // Setup a single global logger
      //****************************************
   private:
      static std::shared_ptr<Logger> _pGlobalLogger;
      static std::mutex _lock;
   public:
      /// <summary>
      /// Set the global logger.  This can only happen once.  Subsequent calls are ignored
      /// </summary>
      /// <param name="pLogger">The Logger to use.</param>
      LOGGER_API static bool SetGlobalLogger(std::shared_ptr<Logger> pLogger)
      {
         std::lock_guard<std::mutex> lock(_lock);
         if (_pGlobalLogger == nullptr && pLogger != nullptr)
         {
            _pGlobalLogger = std::move(pLogger);
            return true;
         }
         return false;
      }
      LOGGER_API static void ClearGlobalLogger()
      {
         std::lock_guard<std::mutex> lock(_lock);
         _pGlobalLogger = nullptr;
      }
      /// <summary>
      /// Gets the global logger.
      /// </summary>
      /// <returns>The global logger.</returns>
      inline LOGGER_API static std::shared_ptr<Logger> GetGlobalLogger()
      {
         return _pGlobalLogger;
      }
      //****************************************
      // Types/Typedefs
      //****************************************
   private:
      typedef sinks::asynchronous_sink<sinks::text_ostream_backend>  stdout_sink_async;   // Asynchronous stdout sink.
      typedef sinks::synchronous_sink<sinks::text_ostream_backend>  stdout_sink_sync;   // Synchronous stdout sink (for google tests)
      typedef sinks::asynchronous_sink<sinks::text_file_backend>     file_sink_async;     // Asynchronous text file sink.
      typedef sinks::synchronous_sink<sinks::text_file_backend>     file_sink_sync;     // Synchronous text file sink (for google tests)
#ifdef _WIN32
      typedef sinks::asynchronous_sink<sinks::simple_event_log_backend> event_sink_async; // Asynchronous Event viewer sync
      typedef sinks::synchronous_sink<sinks::simple_event_log_backend> event_sink_sync; // Synchronous Event viewer sync (for google tests)
#endif

      //****************************************
      // Constructors/Destructors
      //****************************************
   public:
      /// <summary>
      /// Logger constructor.
      /// </summary>
      /// <param name="name">Name that will be written to the log file for each logged message.</param>
      /// <param name="loggingLevel">Log level below which messages will not be logged.</param>
      /// <param naem="logToStdOut">True to log messages to stdout.</param>
      /// <param name="logToTextFile">True to write to a text file.</param>
      /// <param name="logToEventViewer">True to write to the Windows Event Log.</param>
      /// <param name="useSynchronous">true to use the syncrhonous logging.</param>
      LOGGER_API Logger(const std::string& name, LogLevels loggingLevel = LogLevels::DEFAULT_LOG_LEVEL,
            bool logToStdOut = false, bool logToTextFile = LOG_TO_FILE_DEFAULT, bool logToEventViewer = true,
            LogLevels eventLogLevel = LogLevels::DEFAULT_LOG_LEVEL,
            bool useSynchronous = false);
      /// <summary>
      /// Destructor
      /// </summary>
      LOGGER_API ~Logger();
   private:
      /// <summary>
      /// Initialize the boost log
      /// </summary>
      void Initialize();

      //****************************************
      // Fields
      //****************************************
   private:
      std::string _name;
      LogLevels _loggingLevel;
      LogLevels _eventLogLevel; //different level for windows event viewer
      bool _logToStdOut;
      bool _logToTextFile;
      bool _logToEventViewer;
      bool _useSynchronous;
      std::vector<boost::shared_ptr<sinks::sink>>  _sinks;
      sources::severity_logger<LogLevels>  _severityLogger;

      //****************************************
      // Methods
      //****************************************
   public:
      /// <summary>
      /// Returns the boost logger used for logging messages.  Used by macro LOG_MESSAGE
      /// </summary>
      /// <returns>The boost logger used for logging messages.</returns>
      LOGGER_API sources::severity_logger<LogLevels>& GetLogger() { return _severityLogger; }

      /// <summary>
      /// Returns the logging level used by the logger.
      /// </summary>
      /// <returns>The logging level used by the logger.</returns>
      LOGGER_API LogLevels GetLoggingLevel() { return _loggingLevel; }

      /// <summary>
      /// Changes the logging level used by the logger.
      /// </summary>
      /// <param name="loggingLevel">Logging level to use in the logger.</param>
      LOGGER_API void SetLoggingLevel(const LogLevels loggingLevel);

   private:
      Logger& operator=(const Logger&) { return *this; }

      /// <summary>
      /// Initialize the stdout sink.
      /// </summary>
      /// <param name="formatter">Formatter for the message.</param>
      template<typename T>
      boost::shared_ptr<sinks::sink> InitializeStdoutSink(boost::log::formatter& formatter);

      /// <summary>
      /// Initialize the text file sink.
      /// </summary>
      /// <param name="formatter">Formatter for the message.</param>
      template<typename T>
      boost::shared_ptr<sinks::sink> InitializeFileSink(boost::log::formatter& formatter);

#ifdef _WIN32
      /// <summary>
      /// Initialize the Windows Event Viewer sink (simple for now - may want to change this to advanced later).
      /// </summary>
      /// <param name="formatter">Formatter for the messages.</param>
      template<typename T>
      boost::shared_ptr<sinks::sink> InitializeEventViewerSink(boost::log::formatter& formatter);
#endif

      /// <returns>
      /// Returns the path where log files will be created.  Currently, this is
      /// the directory where the executable lives.
      /// </returns>
      static std::string GetLogDirectory();

      /// <summary>
      /// Resets the logging level filter to _loggingLevel for the Sink.
      /// </summary>
      /// <param name="sink">The sink for which the new level will be applied.</param>
      template<typename T>
      void ResetLoggingLevel(boost::shared_ptr<sinks::sink> sink);

   };
}

}
}

/// <summary>
/// Macro to simplify logging.
/// Usage: LOG_MESSAGE_WL(pTheLogger, Matrix::Common::Logging::LogLevels::DEBUG_LVL) << "I want to print a message with a name: " << _name << ", and a number " << 15 << ".";
/// </summary>
/// <param name="pLogger">the Logger.</param>
/// <param name="logLevel">The log level associated with the message.</param>
#define LOG_MESSAGE_WL(pLogger, logLevel)\
   if (nullptr == pLogger) { std::clog << "LOG_MESSAGE_WL: pLogger is nullptr!" << std::endl; }\
   else BOOST_LOG_SEV((pLogger)->GetLogger(), logLevel)

/// <summary>
/// Macro to simplify logging.
/// Usage: LOG_MESSAGE(Matrix::Common::Logging::LogLevels::DEBUG_LVL) << "I want to print a message with a name: " << _name << ", and a number " << 15 << ".";
/// </summary>
/// <param name="pLogger">the Logger.</param>
/// <param name="logLevel">The log level associated with the message.</param>
#define LOG_MESSAGE(logLevel)\
   LOG_MESSAGE_WL((Matrix::Common::Logging::Logger::GetGlobalLogger()).get(), logLevel)
