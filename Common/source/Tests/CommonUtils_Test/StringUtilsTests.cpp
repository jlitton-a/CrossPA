#include <gtest/gtest.h>

#include "StringUtils.h"

using namespace Matrix::Common;

class StringUtilsTest : public testing::Test {
protected:
	virtual void SetUp() {
	}
	virtual void TearDown() {
		//this will be called after each test
	}
};

// Tests LTrim
TEST(StringUtilsTest, LTrim) {
   //Setup
   std::string before = "  Some text to trim \r\n";

   //Test
   std::string after = StringUtils::LTrim(before);

   EXPECT_EQ(after, "Some text to trim \r\n");
}

// Tests RTrim
TEST(StringUtilsTest, RTrim) {
   //Setup
   std::string before = "  Some text to trim \r\n";

   //Test
   std::string after = StringUtils::RTrim(before);

   //Expectations
   EXPECT_EQ(after, "  Some text to trim");
}

// Tests Trim
TEST(StringUtilsTest, Trim) {
   //Setup
   std::string before = "  Some text to trim \r\n";

   //Test
   std::string after = StringUtils::Trim(before);

   //Expectations
   EXPECT_EQ(after, "Some text to trim");
}

// Tests Trim
TEST(StringUtilsTest, Trim_empty) {
   //Setup
   std::string before;

   //Test
   std::string after = StringUtils::Trim(before);

   //Expectations
   EXPECT_EQ(after, "");
}

// Tests Trim with charsToTrim
TEST(StringUtilsTest, Trim_with_chars) {
   //Setup
   std::string before = "x  xSome text to trim x\r\n";
   
   //Test
   std::string after = StringUtils::Trim(before, "x \r\n");

   //Expectations
   EXPECT_EQ(after, "Some text to trim");
}

// Tests Trim with wstring with charstoTrim
TEST(StringUtilsTest, Trim_wstring) {
   //Setup
   std::wstring before = L"  Some text to trim \r\n";
   
   //Test
   std::wstring after = StringUtils::Trim(before, L" \r\n");

   //Expectations
   EXPECT_EQ(after, L"Some text to trim");
}

// Tests Trim with charsToTrim
TEST(StringUtilsTest, Trim_wstring_empty) {
   //Setup
   std::wstring before;
   
   //Test
   std::wstring after = StringUtils::Trim(before, L"x \r\n");

   //Expectations
   EXPECT_EQ(after, L"");
}

// Tests Split with vector param
TEST(StringUtilsTest, Split_elem_param) {
   //Setup
   std::string valToSplit = "Some+Text+To+Trim";
   char delim = '+';
   std::vector<std::string> start;
   start.push_back("myval");

   //Test
   StringUtils::Split(valToSplit, delim, start);

   //Expectations
   EXPECT_EQ(start.size(), (size_t)5);
}
// Tests Split with char delim
TEST(StringUtilsTest, Split_char_delim) {
   //Setup
   std::string valToSplit = "Some+Text+To+Trim";
   char delim = '+';

   //Test
   auto list = StringUtils::Split(valToSplit, delim);

   //Expectations
   EXPECT_EQ(list.size(), (size_t)4);
}
// Tests Split with string delim
TEST(StringUtilsTest, Split_string_delim) {
   //Setup
   std::string valToSplit = "This+;Is+;Some+;Text+;xyz";
   std::string delim = "+;";

   //Test
   auto list = StringUtils::Split(valToSplit, delim);

   //Expectations
   EXPECT_EQ(list.size(), (size_t)5);
}


// Tests Format with char format
TEST(StringUtilsTest, Format) {
   //Setup

   //Test
   auto actual = StringUtils::Format("%s = %d", "xyz", 55);

   //Expectations
   EXPECT_EQ(actual, "xyz = 55");
}

// Tests ParseInt32 with valid integer
TEST(StringUtilsTest, ParseInt32) {
   //Setup
   std::string expected = "55";
   int actual;

   //Test
   auto success = StringUtils::ParseInt32(expected.c_str(), &actual);

   //Expectations
   EXPECT_TRUE(success);
   EXPECT_EQ(actual, 55);
}

// Tests ParseInt32 with invalid character
TEST(StringUtilsTest, ParseInt32_invalid_char) {
   //Setup
   std::string expected = "5x5";
   int actual;
   //Test
   auto success = StringUtils::ParseInt32(expected.c_str(), &actual);

   //Expectations
   EXPECT_FALSE(success);
}

// Tests ParseInt32 with value that is a long
TEST(StringUtilsTest, ParseInt32_too_long) {
   //Setup
   std::string expected = "1234567890123456789";
   int actual;
   //Test
   auto success = StringUtils::ParseInt32(expected.c_str(), &actual);

   //Expectations
   EXPECT_FALSE(success);
}