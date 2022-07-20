copy Debug\libprotobufd.lib %COMMON_THIRDPARTY_HOME%\lib\WinHost\Win32\Debug\libprotobufd.lib
copy Debug\libprotobufd.pdb %COMMON_THIRDPARTY_HOME%\lib\WinHost\Win32\Debug\libprotobufd.pdb
copy Debug\libprotobufd.dll %COMMON_THIRDPARTY_HOME%\lib\WinHost\Win32\Debug\libprotobufd.dll
copy Debug\libprotobuf-lited.lib %COMMON_THIRDPARTY_HOME%\lib\WinHost\Win32\Debug\libprotobuf-lited.lib
copy Debug\libprotobuf-lited.pdb %COMMON_THIRDPARTY_HOME%\lib\WinHost\Win32\Debug\libprotobuf-lited.pdb
copy Debug\libprotobuf-lited.dll %COMMON_THIRDPARTY_HOME%\lib\WinHost\Win32\Debug\libprotobuf-lited.dll

copy Release\libprotobuf.lib %COMMON_THIRDPARTY_HOME%\lib\WinHost\Win32\Release\libprotobuf.lib
copy Release\libprotobuf.dll %COMMON_THIRDPARTY_HOME%\lib\WinHost\Win32\Release\libprotobuf.dll
copy Release\libprotobuf-lite.lib %COMMON_THIRDPARTY_HOME%\lib\WinHost\Win32\Release\libprotobuf-lite.lib
copy Release\libprotobuf-lite.dll %COMMON_THIRDPARTY_HOME%\lib\WinHost\Win32\Release\libprotobuf-lite.dll
copy Release\libprotoc.lib %COMMON_THIRDPARTY_HOME%\lib\WinHost\Win32\Release\libprotoc.lib
copy Release\libprotoc.dll %COMMON_THIRDPARTY_HOME%\lib\WinHost\Win32\Release\libprotoc.dll
copy Release\protoc.exe %COMMON_THIRDPARTY_HOME%\lib\WinHost\Win32\Release\protoc.exe

robocopy install\include\google %COMMON_THIRDPARTY_HOME%\protobuf\google /E
