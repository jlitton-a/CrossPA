:: Set WORKSPACE, COMMON_WORKSPACE and CMAKEHOME for your machine
ECHO OFF

set COMMON_WORKSPACE=%~dp0..\Common
echo COMMON_WORKSPACE=%COMMON_WORKSPACE%
set WORKSPACE=%~dp0
echo WORKSPACE=%WORKSPACE%
set CMAKEHOME=C:\_Utilities\CMake\bin
echo CMAKEHOME=%CMAKEHOME%

CALL "%WORKSPACE%\CommandScripts\WinTools\SetAppEnv.bat"
