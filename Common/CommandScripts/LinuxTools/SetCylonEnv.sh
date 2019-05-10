#!/bin/bash
# This script uses ${COMMON_WORKSPACE} if it is set.  It will use ${WORKSPACE} if ${COMMON_WORKSPACE} is not set
# set ${COMMON_WORKSPACE} or ${WORKSPACE} should point to the Common libraries main directory.

echo "Setting Common Environment Variables for Cylon"

export TARGET_SHORT_NAME="Cylon"

OrigAppDir=${PWD}

if [ "${COMMON_WORKSPACE}" = "" ]
then
   export COMMON_WORKSPACE=${WORKSPACE}
fi

cd "${COMMON_WORKSPACE}"

# Define the Common Build Environment Variables
. ${COMMON_WORKSPACE}/CommandScripts/LinuxTools/SetEnvCommon.sh

cd ${OrigAppDir}
