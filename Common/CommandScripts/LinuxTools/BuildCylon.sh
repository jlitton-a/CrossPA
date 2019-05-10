#!/bin/sh

# The caller can define custom WORKSPACE and COMMON_WORKSPACE prior to calling this script.
# If they are not already defined, this script defines them using the default settings:
# WORKSPACE and COMMON_WORKSPACE are up 2 levels from this script 

# Make sure WORKSPACE is set correctly for Common libraries

if [ "${WORKSPACE}" = "" ]
then
   cd `dirname $BASH_SOURCE`/../..
   export WORKSPACE=${PWD}
fi
echo WORKSPACE=${WORKSPACE}

# Define the Environment Variables used during the Build Process
. ${WORKSPACE}/CommandScripts/LinuxTools/SetCylonEnv.sh

cd ${WORKSPACE}

. ${COMMON_SCRIPT_HOME}/BuildCommon.sh ${TARGET_IP_ADDR}