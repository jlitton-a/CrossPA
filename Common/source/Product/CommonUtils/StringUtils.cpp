#include <algorithm>
#include <functional>
#include <vector>
#include <cctype>
#include <stdarg.h>
#include <stdio.h>
#include <limits.h>

#include "StringUtils.h"

using namespace Matrix::Common;

/** Remove spaces from the start of str
*/
std::string& StringUtils::LTrim(std::string& str)
{
   str.erase(str.begin(), std::find_if(str.begin(), str.end(), std::not1(std::ptr_fun<int, int>(std::isspace))));
	return str;
}

/** Remove spaces from the end of str
*/
std::string& StringUtils::RTrim(std::string& str)
{
   str.erase(std::find_if(str.rbegin(), str.rend(), std::not1(std::ptr_fun<int, int>(std::isspace))).base(), str.end());
	return str;
}

/** Remove spaces from both the start and the end of str
*/
std::string& StringUtils::Trim(std::string& str)
{
	return LTrim(RTrim(str));
}

/* Returns a string with 'whitespace' removed from the start and end of str. 
Use this function to override the default value for 'whitespace'.
*/
std::string StringUtils::Trim(const std::string& str, const std::string& charsToTrim)
{
   const auto strBegin = str.find_first_not_of(charsToTrim);
	if (strBegin == std::string::npos)
		return "";

	const auto strEnd = str.find_last_not_of(charsToTrim);
	const auto strRange = strEnd - strBegin + 1;

	return str.substr(strBegin, strRange);
}

/* Returns a string with 'whitespace' removed from the start and end of wide string str.
*/
std::wstring StringUtils::Trim(const std::wstring& str, const std::wstring& charsToTrim)
{
   const auto strBegin = str.find_first_not_of(charsToTrim);
   if (strBegin == std::string::npos)
      return L"";

   const auto strEnd = str.find_last_not_of(charsToTrim);
   const auto strRange = strEnd - strBegin + 1;

   return str.substr(strBegin, strRange);
}

/** Split str using delimeter delim and add to elems vector
*/
void StringUtils::Split(
		const std::string& str, //! the string to split
		char delim, //! the delimeter to split the string
		std::vector<std::string> &elems //!List of strings in str that were separated by delim
	) 
{
	std::stringstream ss(str);
	std::string item;

	while (std::getline(ss, item, delim))
	{
		elems.push_back(item);
	}
}

/** Split str using delimeter delim and add to elems vector
	@return The list of strings in str that were separated by delim */
std::vector<std::string> StringUtils::Split(
		const std::string& str, //! the string to split
		char delim //! the delimeter to split the string
	) 
{
	std::vector<std::string> elems;
	Split(str, delim, elems);
	return elems;
}

/** Split str using delimeter seperator and add to elems vector
	@return The list of strings in str that were separated by seperator */
std::vector<std::string> StringUtils::Split(
	const std::string& str, //! the string to split
	std::string delim //! the delimeter to split the string
	) 
{
	std::vector<std::string> output;
	std::string::size_type prev_pos = 0, pos = 0;

	while ((pos = str.find(delim, pos)) != std::string::npos)
	{
		std::string substring(str.substr(prev_pos, pos - prev_pos));
		output.push_back(substring);
		prev_pos = ++pos;
	}

	output.push_back(str.substr(prev_pos, pos - prev_pos));

	return output;
}

//Format a string using arguments like those for printf
std::string StringUtils::Format(const char * const zcFormat, ...)
{
   // initialize use of the variable argument array
   va_list vaArgs;
   va_start(vaArgs, zcFormat);

   // reliably acquire the size
   // from a copy of the variable argument array
   // and a functionally reliable call to mock the formatting
   va_list vaArgsCopy;
   va_copy(vaArgsCopy, vaArgs);
   const int iLen = std::vsnprintf(NULL, 0, zcFormat, vaArgsCopy);
   va_end(vaArgsCopy);

   // return a formatted string without risking memory mismanagement
   // and without assuming any compiler or platform specific behavior
   std::vector<char> zc(iLen + 1);
   std::vsnprintf(zc.data(), zc.size(), zcFormat, vaArgs);
   va_end(vaArgs);
   return std::string(zc.data(), zc.size()-1);
}

/** Parses 'str' for a 32-bit signed integer.  If successful, writes
the result to *value and returns true; otherwise leaves *value
unchanged and returns false. */
bool StringUtils::ParseInt32(const char* str, int* value)
{
   // Parses the environment variable as a decimal integer.
   char* end = NULL;
   const long long_value = strtol(str, &end, 10);  // NOLINT
                                                   // Has strtol() consumed all characters in the string?
   if (*end != '\0')
   {
      // No - an invalid character was encountered.
      return false;
   }

   // Is the parsed value in the range of an Int32?
   const int32_t result = static_cast<int32_t>(long_value);
   if (long_value == LONG_MAX || long_value == LONG_MIN ||
      // The parsed value overflows as a long.  (strtol() returns LONG_MAX or LONG_MIN when the input overflows.)
      result != long_value
      // The parsed value overflows as an Int32.
      )
   {
      return false;
   }

   *value = result;
   return true;
}
