#include <gtest/gtest.h>

#include "Logger.h"


using namespace Matrix::Common::Logging;

TEST(LoggerTest, IntToLogLevel)
{
   //Setup
   int intLevel = (int)LogLevels::WARNING_LVL;

   //Test
   auto logLevel = IntToLogLevel(intLevel);

   //Expectations
   EXPECT_EQ((int)LogLevels::WARNING_LVL, (int)logLevel);

   //Cleanup
}


TEST(LoggerTest, LOG_MESSAGE_WL_NullLogger_DoesNotCrash)
{
   //Setup
   Logger* pLogger = nullptr;
   std::stringstream buffer;
   //Redirect the logging to a string
   std::streambuf * old = std::clog.rdbuf(buffer.rdbuf());

   //Test
   LOG_MESSAGE_WL(pLogger, Matrix::Common::Logging::LogLevels::DEBUG_LVL) << "LOG_MESSAGE_WL_NullLogger_Works WORKS!";

   //Expectations
   std::string text = buffer.str();
   printf(text.c_str());
   EXPECT_NE(0, (int)text.size());

   //cleanup
   std::clog.rdbuf(old);
}

TEST(LoggerTest, LOG_MESSAGE_WL_Logs)
{
   //Setup
   std::stringstream buffer;
   //Redirect the logging to a string
   std::streambuf * old = std::clog.rdbuf(buffer.rdbuf());
   auto pLogger = new Logger("TEST", LogLevels::TRACE_LVL, true, false, false, LogLevels::NO_LVL, true);

   //Test
   LOG_MESSAGE_WL(pLogger, Matrix::Common::Logging::LogLevels::DEBUG_LVL) << "LOG_MESSAGE_WL_Works WORKS!";

   //Expectations
   std::string text = buffer.str();
   printf(text.c_str());
   EXPECT_NE(0, (int)text.size());

   //cleanup
   delete pLogger;
   std::clog.rdbuf(old);
}

TEST(LoggerTest, LOG_MESSAGE_WL_DoesNotLog)
{
   //Setup
   auto pLogger = new Logger("TEST", LogLevels::INFO_LVL, true, false, false, LogLevels::EXCEPTION_LVL, true);

   //Test
   LOG_MESSAGE_WL(pLogger, Matrix::Common::Logging::LogLevels::DEBUG_LVL) << "LOG_MESSAGE_WL_DoesNotLog This should not be logged!";

   //Expectations

   //Cleanup
   delete pLogger;
}

TEST(LoggerTest, LOG_MESSAGE_with_NoGlobalLogger)
{
   //Setup
   std::stringstream buffer;
   //Redirect the logging to a string
   std::streambuf * old = std::clog.rdbuf(buffer.rdbuf());

   //Test
   LOG_MESSAGE(LogLevels::DEBUG_LVL) << "LOG_MESSAGE_with_NoGlobalLogger WORKS!";

   //Expectations
   std::string text = buffer.str();
   printf(text.c_str());
   EXPECT_NE(0, (int)text.size());

   //cleanup
   std::clog.rdbuf(old);
}
TEST(LoggerTest, LOG_MESSAGE_with_SetGlobalLogger)
{
   //Setup
   std::stringstream buffer;
   //Redirect the logging to a string
   std::streambuf * old = std::clog.rdbuf(buffer.rdbuf());
   Logger::SetGlobalLogger(std::unique_ptr<Logger>(new Logger("TEST", LogLevels::TRACE_LVL, true, false, false, LogLevels::TRACE_LVL, true)));

   //Test
   LOG_MESSAGE(LogLevels::DEBUG_LVL) << "LOG_MESSAGE_with_SetGlobalLogger WORKS!";

   //Expectations
   std::string text = buffer.str();
   printf(text.c_str());
   EXPECT_NE(0, (int)text.size());

   //Cleanup
   Logger::ClearGlobalLogger();

   //cleanup
   std::clog.rdbuf(old);
}
