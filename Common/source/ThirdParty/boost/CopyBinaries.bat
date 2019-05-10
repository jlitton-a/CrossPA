@REM Run this after building boost to copy binaries to ThirdParty folders
SET BoostBinFolder=%ProgramFiles%\boost\boost_1_68_0\bin.v2
REM SET BoostBinFolder=%ProgramFiles%\boost\boost_1_68_0\bin.v2
SET MSVCVersion=14.1
SET threadingtype=threading-multi
REM SET threadingtype=threadapi-win32\threading-multi

@REM Change current directory to that of batch file
pushd "%~dp0" 

xcopy "%BoostBinFolder%\libs\date_time\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.dll" "..\lib\WinHost\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\date_time\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.pdb" "..\lib\WinHost\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\date_time\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.lib" "..\lib\WinHost\Debug" /Y /R /I

xcopy "%BoostBinFolder%\libs\date_time\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.dll" "..\lib\WinHost\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\date_time\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.pdb" "..\lib\WinHost\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\date_time\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.lib" "..\lib\WinHost\Release" /Y /R /I

xcopy "%BoostBinFolder%\libs\system\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.dll" "..\lib\WinHost\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\system\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.pdb" "..\lib\WinHost\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\system\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.lib" "..\lib\WinHost\Debug" /Y /R /I

xcopy "%BoostBinFolder%\libs\system\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.dll" "..\lib\WinHost\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\system\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.pdb" "..\lib\WinHost\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\system\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.lib" "..\lib\WinHost\Release" /Y /R /I

xcopy "%BoostBinFolder%\libs\atomic\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.dll" "..\lib\WinHost\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\atomic\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.pdb" "..\lib\WinHost\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\atomic\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.lib" "..\lib\WinHost\Debug" /Y /R /I

xcopy "%BoostBinFolder%\libs\atomic\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.dll" "..\lib\WinHost\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\atomic\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.pdb" "..\lib\WinHost\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\atomic\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.lib" "..\lib\WinHost\Release" /Y /R /I

xcopy "%BoostBinFolder%\libs\chrono\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.dll" "..\lib\WinHost\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\chrono\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.pdb" "..\lib\WinHost\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\chrono\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.lib" "..\lib\WinHost\Debug" /Y /R /I

xcopy "%BoostBinFolder%\libs\chrono\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.dll" "..\lib\WinHost\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\chrono\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.pdb" "..\lib\WinHost\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\chrono\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.lib" "..\lib\WinHost\Release" /Y /R /I

xcopy "%BoostBinFolder%\libs\filesystem\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.dll" "..\lib\WinHost\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\filesystem\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.pdb" "..\lib\WinHost\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\filesystem\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.lib" "..\lib\WinHost\Debug" /Y /R /I

xcopy "%BoostBinFolder%\libs\filesystem\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.dll" "..\lib\WinHost\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\filesystem\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.pdb" "..\lib\WinHost\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\filesystem\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.lib" "..\lib\WinHost\Release" /Y /R /I

xcopy "%BoostBinFolder%\libs\log\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.dll" "..\lib\WinHost\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\log\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.pdb" "..\lib\WinHost\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\log\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.lib" "..\lib\WinHost\Debug" /Y /R /I

xcopy "%BoostBinFolder%\libs\log\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.dll" "..\lib\WinHost\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\log\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.pdb" "..\lib\WinHost\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\log\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.lib" "..\lib\WinHost\Release" /Y /R /I

xcopy "%BoostBinFolder%\libs\thread\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.dll" "..\lib\WinHost\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\thread\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.pdb" "..\lib\WinHost\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\thread\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.lib" "..\lib\WinHost\Debug" /Y /R /I

xcopy "%BoostBinFolder%\libs\thread\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.dll" "..\lib\WinHost\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\thread\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.pdb" "..\lib\WinHost\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\thread\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.lib" "..\lib\WinHost\Release" /Y /R /I

xcopy "%BoostBinFolder%\libs\regex\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.dll" "..\lib\WinHost\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\regex\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.pdb" "..\lib\WinHost\Debug" /Y /R /I
xcopy "%BoostBinFolder%\libs\regex\build\msvc-%MSVCVersion%\debug\%threadingtype%\*.lib" "..\lib\WinHost\Debug" /Y /R /I

xcopy "%BoostBinFolder%\libs\regex\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.dll" "..\lib\WinHost\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\regex\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.pdb" "..\lib\WinHost\Release" /Y /R /I
xcopy "%BoostBinFolder%\libs\regex\build\msvc-%MSVCVersion%\release\debug-symbols-on\%threadingtype%\*.lib" "..\lib\WinHost\Release" /Y /R /I

@REM Restore current directory
popd