#include <gtest/gtest.h>
#include "ArgumentParser.h"
#include "Logger.h"

using namespace Matrix::Common;

GTEST_API_ int main(int argc, char **argv) {
	printf("Running ThreadMessages_Test\n");

   ParseValues parseValues;
   parseValues.ParseArgumentValues(argc, argv, (int)Logging::LogLevels::INFO_LVL, false);
   Logging::Logger::SetGlobalLogger(std::unique_ptr<Logging::Logger>(
      new Logging::Logger("TEST", Logging::IntToLogLevel(parseValues.mLogLevel), true, false, false, Logging::LogLevels::NO_LVL, true)));

	testing::InitGoogleTest(&argc, argv);
	auto result = RUN_ALL_TESTS();
   Logging::Logger::ClearGlobalLogger();
   return result;
}
