#!/bin/sh

# This script uses environment variables defined from
# executing SetXyxEnv.sh from the command line.
#
# Set up for building Common libraries projects and then build it

cd "${COMMON_SRC_HOME}"

if [ ! "1" = "${NO_CLEAN_FIRST}" ]; then
   echo "deleting build directory - set NO_CLEAN_FIRST to 1 to skip this step"
   rm -fr "${COMMON_BUILD_DIR}"
   rm -fr "${COMMON_RELEASE_OUT_HOME}"
   rm -fr "${COMMON_DEBUG_OUT_HOME}"
   rm -fr "${COMMON_TEST_OUT_HOME}"
else
   echo "skipping deletion of build directory"
   rm -fr "${COMMON_RELEASE_OUT_HOME}"/lib/*.debug
   rm -fr "${COMMON_RELEASE_OUT_HOME}"/*.xml
fi

mkdir -p "${COMMON_BUILD_DIR}"
cd "${COMMON_BUILD_DIR}"
cmake "${COMMON_MAIN_CMAKE_DIR}"
make
