#include <string.h>
#include <iostream>

#include "stdafx.h"
#include "ArgumentParser.h"
#include "StringUtils.h"

#define TEST_PREFIX_ ""

/** The basis for this code was obtained from GoogleTest source code. Its purpose is to 
provide a generic method for parsing command line args. */

using namespace Matrix::Common;

// <summary>
// Parses the command line for standard arguments and returns the results
// </summary>
void ParseValues::ParseArgumentValues(
      int argc
      , const char* const argv[]
      , int defaultLogLevel
      , bool logToFileDefault)
{
   mLogToFile = logToFileDefault;
   mLogLevel = defaultLogLevel;
   if (argc > 1)
   {
      for (int index = 0; index < argc; index++)
      {
         if (ArgumentParser::ParseBoolFlag(argv[index], "help", &mDisplayHelp))
            continue;
         if (ArgumentParser::ParseBoolFlag(argv[index], "version", &mDisplayVersion))
            continue;
         if (ArgumentParser::ParseInt32Flag(argv[index], "loglevel", &mLogLevel))
            continue;
         if (ArgumentParser::ParseBoolFlag(argv[index], "console", &mIsConsole))
            continue;
         else if (ArgumentParser::ParseBoolFlag(argv[index], "logtofile", &mLogToFile))
            continue;

         if (ParseAdditionalArg(index, argv))
         {
            continue;
         }
      }
   }
}
bool ParseValues::ParseAdditionalArg(int, const char* const [])
{
   return false;
}

/* Gets a help string defining the standard command line options
*/
std::string ParseValues::GetHelpString()
{
   return
      "--help       : display help information\n"
      "--version    : display version\n"
      "--console    : enter interactive console mode\n"
      "--logtofile  : log output to a file\n" 
      "--loglevel=n : set verbosity of logging to value\n"
      "       0=Trace\n"
      "       1=Debug\n"
      "       2=Informational\n"
      "       3=Warning\n"
      "       5=Error\n";
}

/* Parses a string as a command line flag.  The string should have
the format "--flag=value".  When def_optional is true, the "=value" part can be omitted.
*/
const char* ArgumentParser::ParseFlagValue(const char* str, const char* flag, bool def_optional)
{
	// str and flag must not be NULL.
	if (str == NULL || flag == NULL) 
		return NULL;

	// The flag must start with "--" followed by TEST_PREFIX_.
	const std::string flag_str = std::string("--") + TEST_PREFIX_ + flag;
	const size_t flag_len = flag_str.length();
	if (strncmp(str, flag_str.c_str(), flag_len) != 0) 
		return NULL;

	// Skips the flag name.
	const char* flag_end = str + flag_len;

	// When def_optional is true, it's OK to not have a "=value" part.
	if (def_optional && (flag_end[0] == '\0')) 
	{
		return flag_end;
	}

	// If def_optional is true and there are more characters after the
	// flag name, or if def_optional is false, there must be a '=' after
	// the flag name.
	if (flag_end[0] != '=') return NULL;

	// Returns the string after "=".
	return flag_end + 1;
}

/* Parses a string for a bool flag, in the form of either
	"--flag=value" or "--flag".
	In the former case, the value is considered true if it does
	not start with '0', 'f', or 'F'.
	In the latter case, the value is taken as true.
	On success, stores the value of the flag in *value, and returns true.  
	On failure, returns false without changing *value. 
*/
bool ArgumentParser::ParseBoolFlag(const char* str, const char* flag, bool* value)
{
	// Gets the value of the flag as a string.
	const char* const value_str = ParseFlagValue(str, flag, true);

	// Aborts if the parsing failed.
	if (value_str == NULL) 
		return false;

	// Converts the string value to a bool.
	*value = !(*value_str == '0' || *value_str == 'f' || *value_str == 'F');
	return true;
}

/* Parses a string for an int value in the form "--flag=value"
On success, stores the value of the flag in *value, and returns true.
On failure, returns false without changing *value.
*/
bool ArgumentParser::ParseInt32Flag(const char* str, const char* flag, int32_t* value)
{
	// Gets the value of the flag as a string.
	const char* const value_str = ParseFlagValue(str, flag, false);

	// Aborts if the parsing failed.
	if (value_str == NULL) 
		return false;

	// Sets *value to the value of the flag.
	return StringUtils::ParseInt32(value_str, value);
}

/* Parses a string for a string flag, in the form of "--flag=value"
On success, stores the value of the flag in *value, and returns true.
On failure, returns false without changing *value.
*/
bool ArgumentParser::ParseStringFlag(const char* str, const char* flag, std::string* value)
{
	// Gets the value of the flag as a string.
	const char* const value_str = ParseFlagValue(str, flag, false);

	// Aborts if the parsing failed.
	if (value_str == NULL) return false;

	// Sets *value to the value of the flag.
	*value = value_str;
	return true;
}
