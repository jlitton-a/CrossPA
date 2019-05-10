# This script Runs all the test in the test directory
echo Executing RunTests.bat

if [ "${WORKSPACE}" = "" ]
then
   cd `dirname $BASH_SOURCE`/../..
   export WORKSPACE=${PWD}
fi

if [ "${APP_TEST_OUT_HOME}"=="" ]
then
   export APP_TEST_OUT_HOME=${WORKSPACE}/OutputRelease/LinuxHost
fi

echo Running unit tests in directory ${APP_TEST_OUT_HOME}

FILTER=
if [ "$0"!=="" ]
then
   FILTER=--gtest_filter=$0
fi

CALL_RETURN=0
cd ${APP_TEST_OUT_HOME}
FILES="*_Test"
for THIS_TEST in ${FILES}
do
   echo "Running test ${THIS_TEST} ..."
   LD_LIBRARY_PATH=${APP_RELEASE_HOME} ${APP_TEST_OUT_HOME}/${THIS_TEST} --gtest_output=xml:${APP_TEST_OUT_HOME}/
CALL_RETURN=$?
   echo "Test Result: " ${CALL_RETURN}
done

