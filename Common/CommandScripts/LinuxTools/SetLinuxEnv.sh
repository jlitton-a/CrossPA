#!/bin/bash
# This script uses ${COMMON_WORKSPACE} if it is set.  It will use ${WORKSPACE} if ${COMMON_WORKSPACE} is not set
# set ${COMMON_WORKSPACE} or ${WORKSPACE} should point to the Common libraries main directory.
# Set up the required environment variables for SetCommonEnv.sh.

echo "Setting Common Environment Variables for Linux"

export TARGET_SHORT_NAME="Linux"

if [ "${COMMON_WORKSPACE}" = "" ]
then
   export COMMON_WORKSPACE=${WORKSPACE}
fi

cd "${COMMON_WORKSPACE}"

# Define the Common libraries Build Environment Variables
. ${COMMON_WORKSPACE}/CommandScripts/LinuxTools/SetEnvCommon.sh
