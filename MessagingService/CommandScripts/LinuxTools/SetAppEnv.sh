#!/bin/bash

# Called by each of the other SetXyzEnv CommandScripts.
# The following environment variables should be set before calling this script:
#  ${TARGET_SHORT_NAME}
#  ${APP_WORKSPACE}

# Set our build variables.
export TARGET_NAME="${TARGET_SHORT_NAME}Host"

#set up CommonSDK environment variables
if [ "${COMMON_WORKSPACE}" = "" ]
then
   export COMMON_WORKSPACE="${APP_WORKSPACE}/Common"
fi

. ${COMMON_WORKSPACE}/CommandScripts/LinuxTools/Set${TARGET_SHORT_NAME}Env.sh

echo Setting Message Service environment variables
echo APP_WORKSPACE is ${APP_WORKSPACE}

export APP_COMPLETE="NO"
export CROSS_COMPILE_BUILD="x86_64-linux-gnu"

export APP_SCRIPT_HOME="${APP_WORKSPACE}/CommandScripts/LinuxTools"

export APP_SRC_HOME="${APP_WORKSPACE}/source"
export APP_PRODUCT_HOME="${APP_SRC_HOME}/Product"
export APP_TEST_HOME="${APP_SRC_HOME}/Tests"
export APP_THIRDPARTY_HOME="${APP_SRC_HOME}/ThirdParty"

export APP_MAIN_CMAKE_DIR="${APP_WORKSPACE}"
export APP_BUILD_DIR="${APP_MAIN_CMAKE_DIR}/build_${TARGET_NAME}"

export APP_OUTPUT_PREFIX="${APP_WORKSPACE}/Output"
export APP_RELEASE_OUT_HOME="${APP_OUTPUT_PREFIX}Release/${TARGET_NAME}"
export APP_TEST_OUT_HOME="${APP_OUTPUT_PREFIX}Release/${TARGET_NAME}"
