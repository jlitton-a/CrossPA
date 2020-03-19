CMAKE_MINIMUM_REQUIRED(VERSION 3.2)

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
PROJECT(Common)

SET(COMMON_SRC_HOME "$ENV{COMMON_SRC_HOME}")
MESSAGE(STATUS "Adding Common Files = ${COMMON_SRC_HOME}")
INCLUDE("${COMMON_SRC_HOME}/CMakeInclude.txt")

# Can define BYPASS_TESTS to turn off building Test and Sample Projects
if(NOT DEFINED ENV{BYPASS_TESTS})
   SET(ENV{BYPASS_TESTS} 0)
endif()
MESSAGE(STATUS "ByPass Tests: " $ENV{BYPASS_TESTS})

if (MSVC)
   #Used for making subfolders in VisualStudio
   SET(COMMON_VS_SUBFOLDER_UTILS "$ENV{COMMON_VS_SUBFOLDER}/Utils")
   SET(COMMON_VS_SUBFOLDER_THIRDPARTY "$ENV{COMMON_VS_SUBFOLDER}/Third Party")
endif()

# Add Product projects.
ADD_SUBDIRECTORY("${COMMON_PRODUCT_HOME}/CommonUtils")
ADD_SUBDIRECTORY("${COMMON_PRODUCT_HOME}/Logger")

#Add Test projects
IF(NOT $ENV{BYPASS_TESTS})
   MESSAGE(STATUS "Including Test Projects")
   ADD_SUBDIRECTORY("${COMMON_TEST_HOME}")
endif()