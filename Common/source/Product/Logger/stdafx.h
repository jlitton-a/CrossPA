#pragma once

#ifndef VC_EXTRALEAN
#define VC_EXTRALEAN            // Exclude rarely-used stuff from Windows headers
#endif

#ifdef _WIN32
#include <winsock2.h>
#include <Windows.h>
#endif

#ifdef _WIN32
#ifdef Logger_EXPORTS
#define LOGGER_API __declspec(dllexport) 
#else
#define LOGGER_API __declspec(dllimport) 
#endif
#else
#define LOGGER_API
#endif

