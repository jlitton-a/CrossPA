:: set WORKSPACE to point to the Application trunk directory.

:: Export the standard set of environment variables.  
::
:: This script uses ${APP_WORKSPACE} if it is set.  It will use ${WORKSPACE} if ${APP_WORKSPACE} is not set
:: If neither is set, it will set both to the current directory

IF NOT DEFINED WORKSPACE (
   SET WORKSPACE=%~dp0..\..
)

IF NOT DEFINED APP_WORKSPACE (
   SET APP_WORKSPACE=%WORKSPACE%
)

:: Load Common environment variables.
IF NOT DEFINED COMMON_WORKSPACE (
   SET COMMON_WORKSPACE=%APP_WORKSPACE%\Common
)
CALL "%COMMON_WORKSPACE%\CommandScripts\WinTools\SetCommonEnv.bat"

echo Setting MessagingService environment variables
echo APP_WORKSPACE is %APP_WORKSPACE%

:: Set our build variables.
SET TARGET_NAME=WinHost

SET APP_COMPLETE="NO"
SET APP_SCRIPT_HOME=%APP_WORKSPACE%\CommandScripts\WinTools
SET APP_SRC_HOME=%APP_WORKSPACE%\source
SET APP_PRODUCT_HOME=%APP_SRC_HOME%\Product
SET APP_TEST_HOME=%APP_SRC_HOME%\Tests

SET APP_OUTPUT_PATH=%APP_WORKSPACE%
SET APP_OUTPUT_PREFIX=%APP_OUTPUT_PATH%\Output%
SET APP_RELEASE_OUT_HOME=%APP_OUTPUT_PREFIX%Release\%TARGET_NAME%
SET APP_RELEASEWITHDEB_OUT_HOME=%APP_OUTPUT_PREFIX%RelWithDebInfo\%TARGET_NAME%
SET APP_DEBUG_OUT_HOME=%APP_OUTPUT_PREFIX%Debug\%TARGET_NAME%
SET APP_TEST_OUT_HOME=%APP_OUTPUT_PREFIX%Test\%TARGET_NAME%

SET APP_MAIN_CMAKE_DIR=%APP_WORKSPACE%
SET APP_BUILD_DIR=%APP_MAIN_CMAKE_DIR%\build_%TARGET_NAME%
