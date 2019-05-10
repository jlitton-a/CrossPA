#include <gtest/gtest.h>

#include "ArgumentParser.h"

using namespace Matrix::Common;

// Tests ParseValues::Initialize
TEST(ArgumentParserTest, ParseValues_Initialize)
{
   //Setup
   ParseValues parseValues;
   parseValues.mDisplayHelp = true;
   parseValues.mDisplayVersion = true;
   parseValues.mLogLevel = 12;

   //Test
   parseValues.Initialize();

   //Expectations
   EXPECT_FALSE(parseValues.mDisplayHelp);
   EXPECT_FALSE(parseValues.mDisplayVersion);
   EXPECT_NE(parseValues.mLogLevel, 12);
}

// Tests ParseValues::ParseArgumentValues with log level argument
TEST(ArgumentParserTest, ParseValues_WithLogLevel)
{
   //Setup
   ParseValues parseValues;
   ASSERT_EQ(parseValues.mLogLevel, 0);

   const char* const withArgs[2] = { NULL, "--loglevel=1" };

   //Test
   parseValues.ParseArgumentValues(2, withArgs, 5);
   
   //Expectations
   EXPECT_EQ(parseValues.mLogLevel, 1);
}
// Tests ParseValues::ParseArgumentValues with log level argument
TEST(ArgumentParserTest, ParseValues_WithoutLogLevel)
{
   //Setup
   ParseValues parseValues;
   ASSERT_EQ(parseValues.mLogLevel, 0);

   const char* const withArgs[2] = { NULL, NULL };

   //Test
   parseValues.ParseArgumentValues(2, withArgs, 5);

   //Expectations
   EXPECT_EQ(parseValues.mLogLevel, 5);
}


// Tests ParseValues::ParseArgumentValues with arguments
TEST(ArgumentParserTest, ParseValues_WithHelp)
{
   //Setup
   char* noArgs[2] = { NULL, NULL };
   ParseValues parseValues;
   parseValues.ParseArgumentValues(0, noArgs, 0);
   ASSERT_FALSE(parseValues.mDisplayHelp);
   const char* const  withArgs[2] = { NULL, "--help" };

   //Test
   parseValues.ParseArgumentValues(2, withArgs, 0);
   
   //Expectations
   EXPECT_TRUE(parseValues.mDisplayHelp);
}

// Tests ParseValues::ParseArgumentValues with version argument
TEST(ArgumentParserTest, ParseValues_WithVersion)
{
   //Setup
   char* noArgs[2] = { NULL, NULL };
   ParseValues parseValues;
   parseValues.ParseArgumentValues(0, noArgs, 0);
   ASSERT_FALSE(parseValues.mDisplayVersion);
   const char* const  withArgs[2] = { NULL, "--version" };

   //Test
   parseValues.ParseArgumentValues(2, withArgs, 0);
   
   //Expectations
   EXPECT_TRUE(parseValues.mDisplayVersion);
}

// Tests ParseValues::GetHelpString
TEST(ArgumentParserTest, ParseValues_GetHelpString)
{
   //Setup
   ParseValues parseValues;

   //Test
   std::string help = parseValues.GetHelpString();

   //Expectations
   EXPECT_FALSE(help.find("--help") == std::string::npos);
}
