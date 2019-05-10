#pragma once

namespace Matrix
{
namespace Common
{
namespace Logging
{
   /// <summary>
   /// Logging levels.  Should be in order of importance
   /// </summary>
   enum class LogLevels
   {
      TRACE_LVL,
      DEBUG_LVL,
      INFO_LVL,
      WARNING_LVL,
      EXCEPTION_LVL,

      /// <summary>
      /// This should be the last value
      /// </summary>
      NO_LVL,

      HIGHEST_LOG_LEVEL = TRACE_LVL,
      DEFAULT_LOG_LEVEL = INFO_LVL,
      LOWEST_LOG_LEVEL = NO_LVL

   };
   inline static LogLevels IntToLogLevel(int intVar) {
      return static_cast<LogLevels>(intVar);
   }

}
}
}