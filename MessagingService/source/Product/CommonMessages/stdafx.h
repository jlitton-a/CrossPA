// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently

#pragma once

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN            // Exclude rarely-used stuff from Windows headers
#endif

#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS      // some CString constructors will be explicit

#ifdef _WIN32
#ifdef CommonMessages_EXPORTS
#define COMMONMESSAGES_API __declspec(dllexport)
#else
#define COMMONMESSAGES_API __declspec(dllimport)
#endif
#else
#define COMMONMESSAGES_API __attribute__((__visibility__("default")))
#endif

