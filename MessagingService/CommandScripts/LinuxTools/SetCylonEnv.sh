#!/bin/bash
# This script uses ${APP_WORKSPACE} if it is set.  It will use ${WORKSPACE} if ${APP_WORKSPACE} is not set
# set ${APP_WORKSPACE} or ${WORKSPACE} should point to the application main directory.
# Set up the required environment variables for SetAppEnv.sh.

echo "Setting Message Service Environment Variables for Cylon"

export TARGET_SHORT_NAME="Cylon"

if [ "${APP_WORKSPACE}" = "" ]
then
   export APP_WORKSPACE=${WORKSPACE}
fi

cd "${APP_WORKSPACE}"

# Define the Application Build Environment Variables
. ${APP_WORKSPACE}/CommandScripts/LinuxTools/SetAppEnv.sh

