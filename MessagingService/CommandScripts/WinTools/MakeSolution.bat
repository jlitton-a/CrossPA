:: This script uses environment variables defined in SetAppEnv.bat
:: Parameter 1: Build Configuration (Release/Debug). Defaults to Release.
:: Parameter 2: Bypass building test and sample projects. Defaults to 0 (false)
::
:: Can pass in a build configuration if desired in 1st parameter (Debug or Release)
echo Executing MakeSolution.bat
echo off

cd "%APP_WORKSPACE%"

IF NOT %NO_CLEAN_FIRST%==1 (
   echo removing %APP_BUILD_DIR%
   IF EXIST "%APP_BUILD_DIR%" RMDIR /S /Q "%APP_BUILD_DIR%"
   echo removing %APP_RELEASE_OUT_HOME%
   IF EXIST "%APP_RELEASE_OUT_HOME%" RMDIR /S /Q "%APP_RELEASE_OUT_HOME%"
   echo removing %APP_DEBUG_OUT_HOME%
   IF EXIST "%APP_DEBUG_OUT_HOME%" RMDIR /S /Q "%APP_DEBUG_OUT_HOME%"
   echo removing %APP_TEST_OUT_HOME%
   IF EXIST "%APP_TEST_OUT_HOME%" RMDIR /S /Q "%APP_TEST_OUT_HOME%"
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
IF not exist "%APP_BUILD_DIR%" (MKDIR "%APP_BUILD_DIR%")
CD "%APP_BUILD_DIR%"

::--------------------------------------------------------
:: Run CMake to generate the solution and project files
::--------------------------------------------------------

:: if it wasn't defined, see if it is in the path
echo Making with path
cmake.exe "%APP_MAIN_CMAKE_DIR%"
if %ERRORLEVEL%==0 (goto BuildSolution)

echo try %CMAKEHOME%
:: if CMAKEHOME is defined, use it
if NOT "%CMAKEHOME%"=="" (
	echo Making with CMAKEHOME
	"%CMAKEHOME%"\cmake.exe "%APP_MAIN_CMAKE_DIR%"
	if %ERRORLEVEL%==0 (goto BuildSolution)
)

:: If CMake isn't in the Path, try finding it in the default install directories
echo CMake not found in Path. Looking in "C:\Program Files (x86)\CMake\bin"
set PROGFILE_CMAKEHOME="C:\Program Files (x86)\CMake\bin"
%PROGFILE_CMAKEHOME%\cmake.exe "%APP_MAIN_CMAKE_DIR%"
if %ERRORLEVEL%==0 (goto BuildSolution)

echo CMake not found in Path. Looking in "C:\Program Files\CMake\bin"
set PROGFILE_CMAKEHOME="C:\Program Files\CMake\bin"
%PROGFILE_CMAKEHOME%\cmake.exe "%APP_MAIN_CMAKE_DIR%"

:: Now Create the solution
: BuildSolution

echo.Done with CMake
