// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently

#pragma once

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN            // Exclude rarely-used stuff from Windows headers
#endif

#ifdef _WIN32
#else
#include <strings.h>
#endif

#include <sstream>

#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS      // some CString constructors will be explicit

#define DELETE_PTR(p) if (p!=nullptr) { delete p; p=nullptr; }
#define DELETE_ARRAY(p) if (p!=nullptr) { delete[] p; p=nullptr; }

// Ref: https://support.microsoft.com/en-us/kb/155196
#define __STRING2__(x) #x
#define __STRING__(x) __STRING2__(x)

// Handle different functions for Windows and Linux
#ifdef _WIN32
	inline int StrCaseCmp(const char* s1, const char* s2) {
		return _stricmp(s1, s2);
}
#else
	typedef unsigned long DWORD;
	typedef unsigned char BYTE;

	#include <inttypes.h>
	typedef int64_t __int64;

	inline int StrCaseCmp(const char* s1, const char* s2) {
		return strcasecmp(s1, s2);
		}
#endif

#ifdef _WIN32
	#ifdef CommonUtils_EXPORTS
		#define COMMONUTILS_API __declspec(dllexport)
	#else
		#define COMMONUTILS_API __declspec(dllimport)
	#endif
#else
	#define COMMONUTILS_API __attribute__((__visibility__("default")))
#endif

