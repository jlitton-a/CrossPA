#include <boost/log/core.hpp>
#include <boost/log/sources/basic_logger.hpp>
#include <boost/log/support/date_time.hpp>
#include <boost/log/utility/setup/common_attributes.hpp>
#include <boost/core/null_deleter.hpp>
#include "Logger.h"

// Commonly used boost type nicknames.
namespace expressions = boost::log::expressions;
namespace keywords = boost::log::keywords;

BOOST_LOG_ATTRIBUTE_KEYWORD(severity, "Severity", Matrix::Common::Logging::LogLevels)

namespace Matrix
{
namespace Common
{
namespace Logging
{
   static const char* LogLevelNames[] =
   {
      "TRACE",
      "DEBUG",
      "INFO",
      "WARN",
      "ERROR",
      "NONE"
   };
   // stream operator to support logging LogLevels.
   std::ostream & operator<< (std::ostream & strm, const LogLevels & logLevel)
   {
      // Make sure the passed Level fits in the strings[] table.  If it doesn't
      // simply print an integer.
      if ((0 <= (int)logLevel) && (static_cast<std::size_t>(logLevel) < (sizeof(LogLevelNames) / sizeof(*LogLevelNames))))
      {
         strm << LogLevelNames[(int)logLevel];
      }
      else
      {
         strm << static_cast<int>(logLevel);
      }

      return strm;
   }

   std::shared_ptr<Logger> Logger::_pGlobalLogger = nullptr;
   std::mutex Logger::_lock;

   Logger::Logger(const std::string& name, LogLevels loggingLevel
         , bool logToStdOut, bool logToTextFile, bool logToEventViewer
         , LogLevels eventLogLevel
         , bool useSynchronous
         ) :
         _name(name)
         , _loggingLevel(loggingLevel)
         , _eventLogLevel(eventLogLevel)
         , _logToStdOut(logToStdOut)
         , _logToTextFile(logToTextFile)
         , _logToEventViewer(logToEventViewer)
         , _useSynchronous(useSynchronous)
   {
#ifndef _WIN32
      //Windows only
      _logToEventViewer = false;
#endif
      Initialize();

   }

   Logger::~Logger()
   {
      boost::shared_ptr<boost::log::core> pCore = boost::log::core::get();

      for (auto iter : _sinks)
      {
         iter->flush();
         pCore->remove_sink(iter);
      }
      _sinks.clear();
      pCore->remove_all_sinks();
   }
   void Logger::Initialize()
   {
      boost::shared_ptr<boost::log::core> pCore = boost::log::core::get();

      // Set up the common log line formatter for all sinks (except custom sinks)
      boost::log::formatter fmt = expressions::stream
            << expressions::format_date_time<boost::posix_time::ptime>("TimeStamp", "%Y/%m/%d %H:%M:%S.%f-")
            << _name << "-"
            << std::setw(6) << std::left << severity
            << ": " << expressions::smessage;

      // Use the boost log sink for stdout if requested.
      if (_logToStdOut)
      {
         if (_useSynchronous)
            pCore->add_sink(InitializeStdoutSink<stdout_sink_sync>(fmt));
         else
            pCore->add_sink(InitializeStdoutSink<stdout_sink_async>(fmt));
      }

      // The boost log file sink if requested. This writes to a text file.
      if (_logToTextFile)
      {
         if (_useSynchronous)
            pCore->add_sink(InitializeFileSink<file_sink_sync>(fmt));
         else
            pCore->add_sink(InitializeFileSink<file_sink_async>(fmt));
      }

      // The Event Log Sink if requested. This sink uses the older Windows NT Event Logging API which logs to the 
      // basic Windows Application Event Log.
#ifdef _WIN32
      if (_logToEventViewer)
      {
         if (_useSynchronous)
            pCore->add_sink(InitializeEventViewerSink<event_sink_sync>(fmt));
         else
            pCore->add_sink(InitializeEventViewerSink<event_sink_async>(fmt));
      }
#endif

      pCore->add_global_attribute("TimeStamp", boost::log::attributes::utc_clock());

      // Add attributes (date, time, severity, etc.) to the log message formatting.
      boost::log::add_common_attributes();
   }

   void Logger::SetLoggingLevel(const LogLevels loggingLevel)
   {
      _loggingLevel = loggingLevel;

      for (auto iter : _sinks)
      {
         if (_useSynchronous)
            ResetLoggingLevel<file_sink_sync>(iter);
         else
            ResetLoggingLevel<file_sink_async>(iter);
      }
   }

   std::string Logger::GetLogDirectory()
   {
      boost::filesystem::path logFileDir;
#ifdef _WIN32
      TCHAR path[MAX_PATH];

      //if we can't get the path to the current module, use c:
      if (GetModuleFileName(NULL, path, MAX_PATH) == 0)
      {
         logFileDir = "C:";
      }
      //otherwise, find the path to the current module
      else
      {
         logFileDir = std::string(&path[0]);
         logFileDir = logFileDir.parent_path();
      }

      // Add trailing separator.
      logFileDir += "\\";
#else
      // Use current directory for linux.
      logFileDir = ".";
#endif
      return logFileDir.string();
   }


   template<typename T>
   boost::shared_ptr<sinks::sink> Logger::InitializeFileSink(boost::log::formatter& lineFormatter)
   {
      auto logDir = GetLogDirectory();

      // Configure the sink
      boost::shared_ptr<T> pFileSink = boost::make_shared<T>(
            keywords::file_name = logDir + "LOG_" + _name + "_%Y%m%d_%3N.txt",  // File name format.
            keywords::rotation_size = 500 * 1024,                             // Maximum size for each file.
            keywords::auto_flush = true,                                      // Flush after every write.
            keywords::open_mode = (std::ios::out | std::ios::app)             // Append to partial files on program restart.
         );

      // Set up where the rotated files will be stored.
      pFileSink->locked_backend()->set_file_collector(sinks::file::make_collector(
         keywords::target = logDir,                   // Target directory for log file storage.
         keywords::max_size = 2 * 1024 * 1024,           // Maximum total size of the log files, in bytes.
         keywords::min_free_space = 5 * 1024 * 1024      // Minimum free space on the drive, in bytes.
      ));

      pFileSink->set_formatter(lineFormatter);
      pFileSink->set_filter(severity >= _loggingLevel);
      _sinks.push_back(pFileSink);

      return boost::static_pointer_cast<sinks::sink>(pFileSink);
   }

   template<typename T>
   boost::shared_ptr<sinks::sink> Logger::InitializeStdoutSink(boost::log::formatter & lineFormatter)
   {
      boost::shared_ptr<T> pStdoutSink = boost::make_shared<T>();

      // Configure the sink according to the sink template type.
      pStdoutSink->set_formatter(lineFormatter);
      pStdoutSink->set_filter(severity >= _loggingLevel);
      pStdoutSink->locked_backend()->add_stream(boost::shared_ptr<std::ostream>(&std::clog, boost::null_deleter()));
      _sinks.push_back(pStdoutSink);

      return boost::static_pointer_cast<sinks::sink>(pStdoutSink);
   }

#ifdef _WIN32
   template<typename T>
   boost::shared_ptr<sinks::sink> Logger::InitializeEventViewerSink(boost::log::formatter & lineFormatter)
   {
      boost::shared_ptr<T> pEventSink = boost::make_shared<T>(keywords::log_source = _name);

      // Configure the sink according to the sink template type.
      pEventSink->set_formatter(lineFormatter);
      pEventSink->set_filter(severity >= _eventLogLevel);
      // We'll have to map our custom levels to the event log event types
      sinks::event_log::custom_event_type_mapping<LogLevels> mapping("Severity");
      mapping[LogLevels::INFO_LVL] = sinks::event_log::info;
      mapping[LogLevels::WARNING_LVL] = sinks::event_log::warning;
      mapping[LogLevels::EXCEPTION_LVL] = sinks::event_log::error;
      pEventSink->locked_backend()->set_event_type_mapper(mapping);
      _sinks.push_back(pEventSink);

      return boost::static_pointer_cast<sinks::sink>(pEventSink);
   }
#endif

   // Resets logging level filter for a single sink.
   template<typename T>
   void Logger::ResetLoggingLevel(boost::shared_ptr<sinks::sink> sink)
   {
      boost::shared_ptr<T> pSink = boost::static_pointer_cast<T>(sink);
      pSink->reset_filter();
      pSink->set_filter(severity >= _loggingLevel);
   }

}

}
}