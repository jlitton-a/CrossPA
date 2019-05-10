#!/bin/bash

# Called by each of the other SetXyzEnv CommandScripts.
# The following environment variables should be set before calling this script:
#  ${TARGET_SHORT_NAME}
#  ${COMMON_WORKSPACE}

# Set our build variables.
export TARGET_NAME="${TARGET_SHORT_NAME}Host"
export SHELL_TO_RUN="SetEnvCrossCompile${TARGET_SHORT_NAME}.sh"

echo Setting Common environment variables
echo COMMON_WORKSPACE is ${COMMON_WORKSPACE}

export COMMON_COMPLETE="NO"
export CROSS_COMPILE_BUILD="x86_64-linux-gnu"

export COMMON_SCRIPT_HOME="${COMMON_WORKSPACE}/CommandScripts/LinuxTools"

export COMMON_SRC_HOME="${COMMON_WORKSPACE}/source"
export COMMON_PRODUCT_HOME="${COMMON_SRC_HOME}/Product"
export COMMON_TEST_HOME="${COMMON_SRC_HOME}/Tests"
export COMMON_THIRDPARTY_HOME="${COMMON_SRC_HOME}/ThirdParty"

export COMMON_MAIN_CMAKE_DIR="${COMMON_WORKSPACE}"
export COMMON_BUILD_DIR="${COMMON_MAIN_CMAKE_DIR}/build_${TARGET_NAME}"

export COMMON_OUTPUT_PREFIX="${COMMON_WORKSPACE}/Output"
export COMMON_RELEASE_OUT_HOME="${COMMON_OUTPUT_PREFIX}Release/${TARGET_NAME}"
export COMMON_TEST_OUT_HOME="${COMMON_OUTPUT_PREFIX}Release/${TARGET_NAME}"

. "${COMMON_SCRIPT_HOME}/${SHELL_TO_RUN}"

#Linux does not have DEBUG so make it the same as RELEASE
export COMMON_THIRDPARTY_LIB_PATH_DEBUG="${COMMON_THIRDPARTY_HOME}/lib/${TARGET_NAME}/Release"
export COMMON_THIRDPARTY_LIB_PATH_RELEASE="${COMMON_THIRDPARTY_HOME}/lib/${TARGET_NAME}/Release"

#Delete any broken links to ThirdParty Library Files
find ${COMMON_THIRDPARTY_LIB_PATH_RELEASE} -type l | while read f; do if [ ! -e "$f" ]; then rm -f "$f"; fi; done

FILES=${COMMON_THIRDPARTY_LIB_PATH_RELEASE}/lib*.so.*
#for each library file
for fullLibFile in ${FILES}
do
   #if it is not a symbolic link
   if [ ! -L ${fullLibFile} ]
   then
      libFile="${fullLibFile##*/}"
      baseFile=${libFile%%.*}
      versionNum=${libFile##*so.}
      #Need to create link files: filename.so and filename.so.versionNum
      #these are the filenames applications atually link to
      if [ "$versionNum" != "$libFile" ]
      then
         versionNum=${versionNum%%.*}
         if [ ! -f "$COMMON_THIRDPARTY_LIB_PATH_RELEASE/$baseFile".so ]
         then
            echo "creating symbol link $baseFile".so
            ln -s "$COMMON_THIRDPARTY_LIB_PATH_RELEASE/$libFile" "$COMMON_THIRDPARTY_LIB_PATH_RELEASE/$baseFile".so
         fi
         if [ ! -f "$COMMON_THIRDPARTY_LIB_PATH_RELEASE/$baseFile".so.$versionNum ]
         then
            echo "creating symbol link $baseFile".so.$versionNum
            ln -s "$COMMON_THIRDPARTY_LIB_PATH_RELEASE/$libFile" "$COMMON_THIRDPARTY_LIB_PATH_RELEASE/$baseFile".so.$versionNum
         fi
      fi
   fi
done
