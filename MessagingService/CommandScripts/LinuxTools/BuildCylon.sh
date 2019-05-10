#!/bin/sh

# The caller can define custom WORKSPACE AND COMMON_WORKSPACE prior to calling this script.
# If they are not already defined, this script defines them using the default settings:
# WORKSPACE and APP_WORKSPACE are up 2 levels from this script 

# Make sure WORKSPACE is set correctly for the Application

if [ "${WORKSPACE}" = "" ]
then
   cd `dirname $BASH_SOURCE`/../..
   export WORKSPACE=${PWD}
fi
echo WORKSPACE=${WORKSPACE}

# Define the Environment Variables used during the Build Process
. ${WORKSPACE}/CommandScripts/LinuxTools/SetCylonEnv.sh

cd ${WORKSPACE}

. ${APP_SCRIPT_HOME}/BuildApp.sh ${TARGET_IP_ADDR}