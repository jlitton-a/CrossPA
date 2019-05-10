:: Set WORKSPACE and CMAKEHOME for your machine
ECHO OFF 

set WORKSPACE=C:\_Work\Sandbox\CrossPlatformApps\Common
echo WORKSPACE=%WORKSPACE%
set CMAKEHOME=C:\_Utilities\CMake\bin
echo CMAKEHOME=%CMAKEHOME%

CALL "%WORKSPACE%\CommandScripts\WinTools\SetAppEnv.bat"
