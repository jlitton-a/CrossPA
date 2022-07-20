:: This script uses environment variables defined in SetCommonEnv.bat
:: Parameter 1: Build Configuration (Release/Debug). Defaults to Release.
:: Parameter 2: Bypass building test and sample projects. Defaults to 0 (false)
::
:: Can pass in a build configuration if desired in 1st parameter (Debug or Release)
echo Executing MakeSolution.bat
echo off

cd "%COMMON_WORKSPACE%"

IF NOT %NO_CLEAN_FIRST%==1 (
   echo removing %COMMON_BUILD_DIR%
   IF EXIST "%COMMON_BUILD_DIR%" RMDIR /S /Q "%COMMON_BUILD_DIR%"
   echo removing %COMMON_RELEASE_OUT_HOME%
   IF EXIST "%COMMON_RELEASE_OUT_HOME%" RMDIR /S /Q "%COMMON_RELEASE_OUT_HOME%"
   echo removing %COMMON_DEBUG_OUT_HOME%
   IF EXIST "%COMMON_DEBUG_OUT_HOME%" RMDIR /S /Q "%COMMON_DEBUG_OUT_HOME%"
   echo removing %COMMON_TEST_OUT_HOME%
   IF EXIST "%COMMON_TEST_OUT_HOME%" RMDIR /S /Q "%COMMON_TEST_OUT_HOME%"
)else (
   echo Not removing Build Files
)

:: The second parameter lets the caller bypass building Test and Sample projects
if [%1]==[] (
      SET BYPASS_TESTS=0
      echo No BYPASS_TESTS Parameter found... Defaulting to 0
   ) else (
      SET BYPASS_TESTS=%1
      echo BYPASS_TESTS Configuration Parameter found
   )
)
echo Bypass Building Tests: %BYPASS_TESTS%

:: Create Build Directory is needed
IF not exist "%COMMON_BUILD_DIR%" (MKDIR "%COMMON_BUILD_DIR%")
CD "%COMMON_BUILD_DIR%"

::--------------------------------------------------------
:: Run CMake to generate the solution and project files
::--------------------------------------------------------
if [%TARGET_CONFIG%]==[] (
   SET TARGET_CONFIG=Win32
)

if [%TARGET_CONFIG%]==[Win32] (
   SET TARGET=-G "Visual Studio 16 2019" -A Win32
) else if [%TARGET_CONFIG%]==[x64] (
   SET TARGET=-G "Visual Studio 16 2019" -A x64 -T host=x64
)
@echo TARGET_CONFIG=%TARGET_CONFIG%; TARGET=%TARGET%

:: if it wasn't defined, see if it is in the path
echo Making with path
cmake.exe %TARGET% "%COMMON_MAIN_CMAKE_DIR%"
if %ERRORLEVEL%==0 (goto BuildSolution)

echo try %CMAKEHOME%
:: if CMAKEHOME is defined, use it
if NOT "%CMAKEHOME%"=="" (
	echo Making with CMAKEHOME
	"%CMAKEHOME%"\cmake.exe "%COMMON_MAIN_CMAKE_DIR%"
	if %ERRORLEVEL%==0 (goto BuildSolution)
)

:: If CMake isn't in the Path, try finding it in the default install directories
echo CMake not found in Path. Looking in "C:\Program Files (x86)\CMake\bin"
set PROGFILE_CMAKEHOME="C:\Program Files (x86)\CMake\bin"
%PROGFILE_CMAKEHOME%\cmake.exe "%COMMON_MAIN_CMAKE_DIR%"
if %ERRORLEVEL%==0 (goto BuildSolution)

echo CMake not found in Path. Looking in "C:\Program Files\CMake\bin"
set PROGFILE_CMAKEHOME="C:\Program Files\CMake\bin"
%PROGFILE_CMAKEHOME%\cmake.exe "%COMMON_MAIN_CMAKE_DIR%"

:: Now Create the solution
: BuildSolution

echo.Done with CMake

