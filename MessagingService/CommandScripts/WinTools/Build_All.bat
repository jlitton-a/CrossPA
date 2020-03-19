:: This script does a minimal Debug build of MessagingService
echo off
echo Executing Build_All.bat

:: Ensure Workspace is set correctly for our application
if NOT DEFINED %WORKSPACE% (
	SET WORKSPACE=%~dp0..\..
)
if "%1"=="1" (
   SET SKIP_TESTS=1
) else (
   SET SKIP_TESTS=0
)
if "%2"=="1" (
   SET NO_CLEAN_FIRST=1
) else (
   SET NO_CLEAN_FIRST=0
)
if "%3"=="1" (
   SET TARGET_CONFIG=x64
) else (
   SET TARGET_CONFIG=Win32
)

call "%WORKSPACE%\CommandScripts\WinTools\SetAppEnv.bat"
call "%APP_SCRIPT_HOME%\MakeSolution.bat" %SKIP_TESTS%

exit /B
