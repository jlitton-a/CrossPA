#pragma once

#include "../stdafx.h"

namespace Matrix
{
namespace Common
{
   /// <summary>
   /// Values retrieved from standard command line flags
   /// </summary>
   class ParseValues
   {
#ifdef _WIN32
      static const int LOG_TO_FILE_DEFAULT = 0;
#else
      static const int LOG_TO_FILE_DEFAULT = 1;
#endif

      //****************************************
      // Constructors/Destructors
      //****************************************
   public:
      ///<summary>
      ///Standard constructor that initialize the values to defaults
      ///</summary>
      ParseValues()
      {
         Initialize();
      }

      //****************************************
      // Fields
      //****************************************
   public:
      /// <summary>
      /// true if command line contains --help
      /// </summary>
      bool mDisplayHelp;
      /// <summary>
      /// true if command line contains --version
      /// </summary>
      bool mDisplayVersion;
      /// <summary>
      /// value when --loglevel=x is specified
      /// </summary>
      int mLogLevel;
      bool mIsConsole;
      bool mLogToFile;

      //****************************************
      // Methods
      //****************************************
   public:

      ///<summary>
      ///Initialize the values to defaults
      ///</summary>
      COMMONUTILS_API virtual void Initialize()
      {
         mDisplayHelp = false;
         mDisplayVersion = false;
         mLogLevel = 0;
         mIsConsole = false;
         mLogToFile = false;
      }

      /// <summary>
      /// Parses the command line for arguments and sets the values.  
      /// NOTE:  Previously set values will remain unless an argument is found.
      ///         Call Initialize() before calling this function to clear values that have been set.
      /// </summary>
      /// <param name="argc">The number of arguments.</param>
      /// <param name="argv">An array of arguments in the form --argument=value.</param>
      /// <returns>a structure containing values indicating the values of standard command line args</returns>
      COMMONUTILS_API void ParseArgumentValues(int argc
               , const char* const argv[]
               , int defaultLogLevel
               , bool logToFileDefault = LOG_TO_FILE_DEFAULT);

      /// <summary>
      /// Returns the help string for arguments used in ParseArgumentValues
      /// </summary>
      /// <param name="argc">The number of arguments.</param>
      /// <param name="argv">The arguments in the form --argument=value.</param>
      /// <returns>a structure containing values indicating the values of standard command line args</returns>
      COMMONUTILS_API virtual std::string GetHelpString();

   protected:
      /// <summary>
      /// Parses additional arguments
      /// By default it does nothing - it should be overridden by derived class to handle additional arguments
      /// </summary>
      /// <param name="index">The index of the argument in argv to be parsed.</param>
      /// <param name="argv">An array of arguments in the form --argument=value.</param>
      /// <returns>true if the argument was handled</returns>
      COMMONUTILS_API virtual bool ParseAdditionalArg(int /*index*/, const char* const /*argv*/[]);

   };

   /// <summary>
   /// Provides utilities for parsing command line arguments
   /// </summary>
   class ArgumentParser
   {
      //****************************************
      // Static Methods
      //****************************************
   public:
      /// <summary>
      /// Parses a string as a command line flag.  The string should have
      /// the format "--flag=value".When def_optional is true, the "=value" part can be omitted.
      /// </summary>
      /// <param name="str">contains the entire command line flag(aka arg). Should be of the format: "--flag=value"</param>
      /// <param name="flag">flag is the static argument key value .</param>
      /// <param name="def_optional">If true, str can be empty.</param>
      /// <returns>If str has the proper format, the string following the "=", or NULL if the parsing failed</returns>
      COMMONUTILS_API static const char* ParseFlagValue(const char* str, const char* flag, bool def_optional);

      /// <summary>
      /// Parses a string for a bool flag, in the form of either
      /// "--flag=value" or "--flag".
      ///    In the former case, the value is considered true if it does
      ///    not start with '0', 'f', or 'F'.
      ///    In the latter case, the value is taken as true.
      /// On success, stores the value of the flag in *value, and returns true.
      /// On failure, returns false without changing *value.
      /// </summary>
      /// <param name="str">the input string. Should be of the form: "--flag=1" or "--flag".</param>
      /// <param name="flag">The static argument key value used to uniquely identify an argument.</param>
      /// <param name="value">holds the parsed string value if successful.</param>
      /// <returns>True if the input string has the proper format</returns>
      COMMONUTILS_API static bool ParseBoolFlag(const char* str, const char* flag, bool* value);

      /// <summary>
      /// Parses a string for an int value in the form "--flag=value"
      /// On success, stores the value of the flag in value, and returns true.
      /// On failure, returns false without changing value.
      /// </summary>
      /// <param name="str">the input string. Should be of the form: "--flag=1".</param>
      /// <param name="flag">The static argument key value used to uniquely identify an argument.</param>
      /// <param name="value">holds the parsed string value if successful.</param>
      /// <returns>True if the input string has the proper format</returns>
      COMMONUTILS_API static bool ParseInt32Flag(const char* str, const char* flag, int32_t* value);

      /// <summary>
      /// Parses a string for a string flag, in the form of "--flag=value"
      /// On success, stores the value of the flag in value, and returns true.
      /// On failure, returns false without changing value.
      /// </summary>
      /// <param name="str">the input string. Should be of the form: "--flag=Hello".</param>
      /// <param name="flag">The static argument key value used to uniquely identify an argument.</param>
      /// <param name="value">holds the parsed string value if successful.</param>
      /// <returns>True if the input string has the proper format</returns>
      COMMONUTILS_API static bool ParseStringFlag(const char* str, const char* flag, std::string* value);
   };
}
}