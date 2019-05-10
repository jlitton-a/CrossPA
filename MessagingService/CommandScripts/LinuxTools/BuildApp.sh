#!/bin/sh

# This script uses environment variables defined from
# executing SetXyxEnv.sh from the command line.
#
# Set up for building application projects and then build it

cd "${APP_SRC_HOME}"

if [ ! "1" = "${NO_CLEAN_FIRST}" ]; then
   echo "deleting build directory - set NO_CLEAN_FIRST to 1 to skip this step"
   rm -fr "${APP_BUILD_DIR}"
   rm -fr "${APP_RELEASE_OUT_HOME}"
   rm -fr "${APP_TEST_OUT_HOME}"
else
   echo "skipping deletion of build directory"
   rm -fr "${APP_RELEASE_OUT_HOME}"/lib/*.debug
   rm -fr "${APP_RELEASE_OUT_HOME}"/*.xml
fi

mkdir -p "${APP_BUILD_DIR}"
cd "${APP_BUILD_DIR}"
cmake "${APP_MAIN_CMAKE_DIR}"
make
