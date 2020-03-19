:: Set WORKSPACE and CMAKEHOME for your machine
ECHO OFF 

IF "%WORKSPACE%"=="" set WORKSPACE=%~dp0
echo WORKSPACE=%WORKSPACE%
set CMAKEHOME=C:\_Utilities\CMake\bin
echo CMAKEHOME=%CMAKEHOME%

CALL "%WORKSPACE%\CommandScripts\WinTools\SetCommonEnv.bat"
