#!/bin/sh
# This shellscript sets the cross compiling environment needed for building ARM
# applications targeted to the Cylon.
#
# This script is run from SetEnvCommon.sh after # ${COMMON_SCRIPT_HOME} has been
# set.

# CMake files use CROSS_COMPILE_FULL, CROSS_COMPILE_FULL_HOST (CMAKE_FIND_ROOT_PATH)
echo "Configuring for Cylon CrossCompiler"

# Set PATH to where the top level where the ARM cross-compiler toolchain is installed.
export  CYLON_HOME="/usr"

if [ -d "${CYLON_HOME}" ]; then
   #name of the cross compiler toolchain
   export CYLON_TRIPLET="arm-linux-gnueabi"
   #root location where the sub-directories for target libraries are located
   export CYLON_SYSROOTS="${CYLON_HOME}/arm-linux-gnueabi"
   #location where the actual compilers 9gcc, g++, etc.) are located
   export CROSS_COMPILER_HOME="${CYLON_HOME}/bin"
   export PATH="${CROSS_COMPILER_HOME}:${PATH}"
   #This should be the prefix used for the compiler toolchain (e.g. xyz- when compiler is called xyz-gcc, xyz-g++)
   export TARGET_PREFIX="${CYLON_TRIPLET}-"

   # Execute our locally modified environment shell.
   CYLON_ENV_SH="${COMMON_SCRIPT_HOME}/environment-setup-${TARGET_SHORT_NAME}.sh"
   . "${CYLON_ENV_SH}"

   export CROSS_COMPILE="${TARGET_PREFIX}"
   export CROSS_COMPILE_FULL_HOST="${CYLON_SYSROOTS}"
   export CROSS_COMPILE_HOST="${CYLON_TRIPLET}"
   export CROSS_COMPILE_FULL="${CROSS_COMPILER_HOME}/${CROSS_COMPILE}"
   export COMPILE_TYPE="CYLON"
fi

