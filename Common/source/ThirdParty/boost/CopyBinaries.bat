@REM Run this after building boost to copy binaries to ThirdParty folders
SET BoostBinFolder=%ProgramFiles%\Boost\boost_1_76_0\bin.v2
SET MSVCVersion=14.2
SET threadingtype=threading-multi
@REM SET threadingtype=threadapi-win32\threading-multi

if [%1]==[] (
   SET TARGET_CONFIG=Win32
   SET DEBUG_PATH=debug\address-model-32\%threadingtype%
   SET RELEASE_PATH=release\address-model-32\debug-symbols-on\%threadingtype%
   SET ALT_DEBUG_PATH=debug\address-model-32\threadapi-win32\%threadingtype%
   SET ALT_RELEASE_PATH=\release\address-model-32\debug-symbols-on\threadapi-win32\%threadingtype%
) else (
   SET TARGET_CONFIG=x64
   SET DEBUG_PATH=debug\address-model-64\%threadingtype%
   SET ALT_DEBUG_PATH=debug\address-model-64\threadapi-win32\%threadingtype%
   SET RELEASE_PATH=release\address-model-64\debug-symbols-on\%threadingtype%
   SET ALT_RELEASE_PATH=release\address-model-64\debug-symbols-on\threadapi-win32\%threadingtype%
)
@ECHO TARGET_CONFIG=%TARGET_CONFIG%
@REM Change current directory to that of batch file
pushd "%~dp0" 

xcopy "%BoostBinFolder%\libs\date_time\build\msvc-%MSVCVersion%\%DEBUG_PATH%\*.dll" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\date_time\build\msvc-%MSVCVersion%\%DEBUG_PATH%\*.pdb" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\date_time\build\msvc-%MSVCVersion%\%DEBUG_PATH%\*.lib" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I

xcopy "%BoostBinFolder%\libs\date_time\build\msvc-%MSVCVersion%\%RELEASE_PATH%\*.dll" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\date_time\build\msvc-%MSVCVersion%\%RELEASE_PATH%\*.pdb" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\date_time\build\msvc-%MSVCVersion%\%RELEASE_PATH%\*.lib" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I

xcopy "%BoostBinFolder%\libs\system\build\msvc-%MSVCVersion%\%DEBUG_PATH%\*.dll" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\system\build\msvc-%MSVCVersion%\%DEBUG_PATH%\*.pdb" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\system\build\msvc-%MSVCVersion%\%DEBUG_PATH%\*.lib" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I

xcopy "%BoostBinFolder%\libs\system\build\msvc-%MSVCVersion%\%RELEASE_PATH%\*.dll" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\system\build\msvc-%MSVCVersion%\%RELEASE_PATH%\*.pdb" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\system\build\msvc-%MSVCVersion%\%RELEASE_PATH%\*.lib" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I

xcopy "%BoostBinFolder%\libs\atomic\build\msvc-%MSVCVersion%\%DEBUG_PATH%\*.dll" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\atomic\build\msvc-%MSVCVersion%\%DEBUG_PATH%\*.pdb" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\atomic\build\msvc-%MSVCVersion%\%DEBUG_PATH%\*.lib" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I

xcopy "%BoostBinFolder%\libs\atomic\build\msvc-%MSVCVersion%\%RELEASE_PATH%\*.dll" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\atomic\build\msvc-%MSVCVersion%\%RELEASE_PATH%\*.pdb" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\atomic\build\msvc-%MSVCVersion%\%RELEASE_PATH%\*.lib" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I

xcopy "%BoostBinFolder%\libs\chrono\build\msvc-%MSVCVersion%\%DEBUG_PATH%\*.dll" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\chrono\build\msvc-%MSVCVersion%\%DEBUG_PATH%\*.pdb" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\chrono\build\msvc-%MSVCVersion%\%DEBUG_PATH%\*.lib" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I

xcopy "%BoostBinFolder%\libs\chrono\build\msvc-%MSVCVersion%\%RELEASE_PATH%\*.dll" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\chrono\build\msvc-%MSVCVersion%\%RELEASE_PATH%\*.pdb" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\chrono\build\msvc-%MSVCVersion%\%RELEASE_PATH%\*.lib" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I

xcopy "%BoostBinFolder%\libs\filesystem\build\msvc-%MSVCVersion%\%DEBUG_PATH%\*.dll" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\filesystem\build\msvc-%MSVCVersion%\%DEBUG_PATH%\*.pdb" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\filesystem\build\msvc-%MSVCVersion%\%DEBUG_PATH%\*.lib" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I

xcopy "%BoostBinFolder%\libs\filesystem\build\msvc-%MSVCVersion%\%RELEASE_PATH%\*.dll" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\filesystem\build\msvc-%MSVCVersion%\%RELEASE_PATH%\*.pdb" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\filesystem\build\msvc-%MSVCVersion%\%RELEASE_PATH%\*.lib" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I

xcopy "%BoostBinFolder%\libs\log\build\msvc-%MSVCVersion%\%ALT_DEBUG_PATH%\*.dll" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\log\build\msvc-%MSVCVersion%\%ALT_DEBUG_PATH%\*.pdb" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\log\build\msvc-%MSVCVersion%\%ALT_DEBUG_PATH%\*.lib" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I

xcopy "%BoostBinFolder%\libs\log\build\msvc-%MSVCVersion%\%ALT_RELEASE_PATH%\*.dll" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\log\build\msvc-%MSVCVersion%\%ALT_RELEASE_PATH%\*.pdb" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\log\build\msvc-%MSVCVersion%\%ALT_RELEASE_PATH%\*.lib" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I

xcopy "%BoostBinFolder%\libs\thread\build\msvc-%MSVCVersion%\%ALT_DEBUG_PATH%\*.dll" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\thread\build\msvc-%MSVCVersion%\%ALT_DEBUG_PATH%\*.pdb" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\thread\build\msvc-%MSVCVersion%\%ALT_DEBUG_PATH%\*.lib" "..\lib\WinHost\%TARGET_CONFIG%\Debug" /Y /R /I

xcopy "%BoostBinFolder%\libs\thread\build\msvc-%MSVCVersion%\%ALT_RELEASE_PATH%\*.dll" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\thread\build\msvc-%MSVCVersion%\%ALT_RELEASE_PATH%\*.pdb" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\thread\build\msvc-%MSVCVersion%\%ALT_RELEASE_PATH%\*.lib" "..\lib\WinHost\%TARGET_CONFIG%\Release" /Y /R /I

@REM Restore current directory
popd