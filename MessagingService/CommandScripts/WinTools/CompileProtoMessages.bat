:: Create the source files from the .proto file
echo off

SET ProtoHome=%APP_SRC_HOME%\_ProtoFiles
SET ProtoFileName=CommonMessages
SET DLL_EXPORT_DECL=COMMONMESSAGES_API
SET CodeHome="%APP_PRODUCT_HOME%\CommonMessages"
SET CodeHome_CSharp="%APP_SRC_HOME%\CSharp\Product\CommonMessages"

call %COMMON_SCRIPT_HOME%\CompileProto.bat
