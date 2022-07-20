:: Create the source files from the .proto file
@echo off
IF [%1]==[1] (
   SET ProtocDir=%COMMON_THIRDPARTY_HOME%\lib\WinHost\x64\Release
) ELSE (
   SET ProtocDir=%COMMON_THIRDPARTY_HOME%\lib\WinHost\Win32\Release
)

set build_error=0

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:: Create the C# source file from the .proto file
@echo "Generating C# Source Files"
::CommonSDK is using protobuf 2.6.1. Version 3.0.0 is needed to generate C# files.
%ProtocDir%\protoc -I=%ProtoHome% -I="%COMMON_THIRDPARTY_HOME%"\protobuf --csharp_out=%CodeHome_CSharp% %ProtoHome%\%ProtoFileName%.proto
@if errorlevel 1 set build_error=1

@if %build_error%==1 (
   exit /b 1
)

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:: Create the C++ source file from the .proto file
@echo "Generating C++ Source Files"
%ProtocDir%\protoc -I=%ProtoHome% -I="%COMMON_THIRDPARTY_HOME%"\protobuf --cpp_out=dllexport_decl=%DLL_EXPORT_DECL%:%CodeHome% %ProtoHome%\%ProtoFileName%.proto
@if errorlevel 1 set build_error=1

@if "%build_error%"=="1" (
   exit /b 1
)

:: Need to add: '#include "../stdafx.h"' to the %ProtoFileName%.pb.h
:: to define %ProtoFileName%_EXPORTS
SET ProtoFilePath=%CodeHome%\%ProtoFileName%.pb.h
@echo working on %ProtoFilePath%
if exist %ProtoFilePath%.tmp (
   del %ProtoFilePath%.tmp
)
move %ProtoFilePath% %ProtoFilePath%.tmp
echo.#include "stdafx.h">%ProtoFilePath%
type %ProtoFilePath%.tmp >>%ProtoFilePath%
::move %ProtoFilePath% %CodeHome%\include
if exist %ProtoFilePath%.tmp (
   del %ProtoFilePath%.tmp
)

:: Want to suppress warnings in compilation of the .cc file
SET ProtoFilePath=%CodeHome%\%ProtoFileName%.pb.cc
@echo working on %ProtoFilePath%
if exist %ProtoFilePath%.tmp (
   del %ProtoFilePath%.tmp
)
move %ProtoFilePath% %ProtoFilePath%.tmp
echo.#ifdef _WIN32>>%ProtoFilePath%
echo.#pragma warning( push )>>%ProtoFilePath%
echo.#pragma warning( disable: 4251 4100 4146 4018 4267 4244 4127 26495 26812 6387 6385)>>%ProtoFilePath%
echo.#endif>>%ProtoFilePath%
type %ProtoFilePath%.tmp >>%ProtoFilePath%
echo.#ifdef _WIN32>>%ProtoFilePath%
echo.#pragma warning( pop )>>%ProtoFilePath%
echo.#endif>>%ProtoFilePath%
if exist %ProtoFilePath%.tmp (
   del %ProtoFilePath%.tmp
)

:: Want to suppress warnings in compilation of the .h file
SET ProtoFilePath=%CodeHome%\%ProtoFileName%.pb.h
@echo working on %ProtoFilePath%
if exist %ProtoFilePath%.tmp (
   del %ProtoFilePath%.tmp
)
move %ProtoFilePath% %ProtoFilePath%.tmp
echo.#ifdef _WIN32>>%ProtoFilePath%
echo.#pragma warning( push )>>%ProtoFilePath%
echo.#pragma warning( disable: 4251 4100 4146 4018 4267 4244 4127 26495 26812 6387 6385)>>%ProtoFilePath%
echo.#endif>>%ProtoFilePath%
type %ProtoFilePath%.tmp >>%ProtoFilePath%
echo.#ifdef _WIN32>>%ProtoFilePath%
echo.#pragma warning( pop )>>%ProtoFilePath%
echo.#endif>>%ProtoFilePath%
if exist %ProtoFilePath%.tmp (
   del %ProtoFilePath%.tmp
)
