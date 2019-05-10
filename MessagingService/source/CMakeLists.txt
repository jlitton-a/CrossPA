CMAKE_MINIMUM_REQUIRED(VERSION 2.8.12)

# Use solution folders. 
SET_PROPERTY(GLOBAL PROPERTY USE_FOLDERS ON) 

# !!! Set up the compiler to use, first (i.e. before the PROJECT() call.)
if (UNIX)
   # Cross compiling locations.
   SET(CMAKE_FIND_ROOT_PATH_MODE_PROGRAM NEVER)
   SET(CMAKE_FIND_ROOT_PATH_MODE_LIBRARY ONLY)
   SET(CMAKE_FIND_ROOT_PATH_MODE_INCLUDE ONLY)
endif()

# The project will use the compiler specified above (or the default compiler for Windows.)
PROJECT(MessagingService)

MESSAGE(STATUS "Adding MessagingService Files = $ENV{APP_SRC_HOME}")
INCLUDE("$ENV{COMMON_SRC_HOME}/CMakeInclude.txt")
SET(APP_PRODUCT_HOME "$ENV{APP_PRODUCT_HOME}")
SET(APP_TEST_HOME "$ENV{APP_TEST_HOME}")

if (MSVC)
   STRING(REGEX REPLACE "\\\\" "/" APP_PRODUCT_HOME ${APP_PRODUCT_HOME})
   STRING(REGEX REPLACE "\\\\" "/" APP_TEST_HOME ${APP_TEST_HOME})
   
   #Used for making subfolders in VisualStudio
   SET(APP_VS_SUBFOLDER_MESSAGINGSERVICE "MessagingService")
endif ()

# Add Product projects.
ADD_SUBDIRECTORY("${APP_PRODUCT_HOME}/MessagingService")
ADD_SUBDIRECTORY("${APP_PRODUCT_HOME}/CommonMessages")
ADD_SUBDIRECTORY("${APP_PRODUCT_HOME}/CommunicationUtils")
ADD_SUBDIRECTORY("${APP_PRODUCT_HOME}/MessageThreads")

#Add Test projects
if(NOT $ENV{BYPASS_TESTS})
   MESSAGE(STATUS "Including Test Projects")
   ADD_SUBDIRECTORY("${APP_TEST_HOME}")
endif()

