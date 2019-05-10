#pragma once

#include <string>
#include <vector>
#include "../stdafx.h"

namespace Matrix
{
namespace Common
{
   /// <summary>
   /// Utility Class for encapsulating common string functions
   /// </summary>
   class StringUtils
   {
      //****************************************
      // Static Methods
      //****************************************
   public:
      /// <summary>
      /// Remove whitespace from the start of str.
      /// </summary>
      /// <param name="str">string to trim</param>
      /// <returns>str with spaces trimmed from the start</returns>
      static COMMONUTILS_API std::string& LTrim(std::string& str);

      /// <summary>
      /// Remove whitespace from the end of str.
      /// </summary>
      /// <param name="str">string to trim</param>
      /// <returns>str with spaces trimmed from the end</returns>
      static COMMONUTILS_API std::string& RTrim(std::string& str);

      /// <summary>
      /// Remove whitespace from the start and the end of str.
      /// </summary>
      /// <param name="str">string to trim</param>
      /// <returns>str with spaces trimmed from the start and the end</returns>
      static COMMONUTILS_API std::string& Trim(std::string& str);

      /// <summary>
      /// Returns a string with characters in charsToTrim removed from the start and end of str.
      /// Use this function to override the default value for 'whitespace'.
      /// </summary>
      /// <param name="str">string to trim</param>
      /// <returns>str with charsToTrim trimmed from the start and the end</returns>
      static COMMONUTILS_API std::string Trim(const std::string& str,
         const std::string& charsToTrim = " \t\n");

      /// <summary>
      /// Returns a string with characters in charsToTrim removed from the start and end of wide string str.
      /// </summary>
      /// <param name="str">string to trim</param>
      /// <returns>str with charsToTrim trimmed from the start and the end</returns>
      static COMMONUTILS_API std::wstring Trim(const std::wstring& str,
         const std::wstring& charsToTrim = L" \t\n");

      /// <summary>
      /// Split str using delimeter delim and add to elems vector.
      /// </summary>
      /// <param name="str">string to split</param>
      /// <param name="delim">delimiter to split on</param>
      /// <param name="elems">the vector that the list will be added to</param>
      /// <returns>The list of strings in str that were separated by delim</returns>
      static COMMONUTILS_API void Split(
         const std::string& str, //! the string to split
         char delim, //! the delimeter to split the string
         std::vector<std::string> &elems //!List of strings in str that were separated by delim
      );

      /// <summary>
      /// Split str using delimeter delim and returns the vector with the list.
      /// </summary>
      /// <param name="str">string to split</param>
      /// <param name="delim">delimiter to split on</param>
      /// <returns>The list of strings in str that were separated by delim</returns>
      static COMMONUTILS_API std::vector<std::string> Split(
         const std::string& str, //! the string to split
         char delim //! the delimeter to split the string
      );

      /// <summary>
      /// Split str using delimeter delim and returns the vector with the list.
      /// </summary>
      /// <param name="str">string to split</param>
      /// <param name="delim">delimiter to split on</param>
      /// <returns>The list of strings in str that were separated by delim</returns>
      static COMMONUTILS_API std::vector<std::string> Split(
         const std::string& str, //! the string to split
         std::string delim //! the delimeter to split the string
      );

      /// <summary>
      /// This function takes format char string and args and returns a formatted std::string.
      /// </summary>
      /// <param name="fmt">Format specifier</param>
      /// <param name="...">Variable number of arguments matching fmt</param>
      /// <returns>The formatted std::string </returns>
      static COMMONUTILS_API std::string Format(const char * const zcFormat, ...);

      /// <summary>
      /// Parses 'str' for a 32-bit signed integer.
      /// If successful, writes the result to value and returns true; otherwise leaves value
      /// </summary>
      /// <param name="str">String that can be interpreted as an integer</param>
      /// <param name="...">Variable number of arguments matching fmt</param>
      /// <returns>True if parse was successful</returns>
      static COMMONUTILS_API bool ParseInt32(const char* str, int* value);

   };
}
}