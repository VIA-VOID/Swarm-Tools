@echo off
setlocal

pushd %~dp0

set PROJ_PATH=%CD%\..
set CS_PROJ=%PROJ_PATH%\PacketProtobuf.csproj
set SERVER_PATH=..\..\..\SwarmServer\GameServer
set DUMMY_CLIENT_PATH=..\..\..\SwarmServer\DummyClient

echo [INFO] Current Directory: %CD%
echo [INFO] Project Directory: %CS_PROJ%

:: 자동 패킷 핸들러 생성기 실행
echo [INFO] Running C# PacketGenerator ...
dotnet run --project "%CS_PROJ%" -- ./

IF ERRORLEVEL 1 (
    echo [ERROR] Packet generator failed!
    PAUSE
)

:: .proto -> C++ 파일 생성
protoc -I=./ --cpp_out=./ ./Protocol.proto
protoc -I=./ --cpp_out=./ ./Struct.proto
protoc -I=./ --cpp_out=./ ./Enum.proto

IF ERRORLEVEL 1 (
    echo [ERROR] protoc failed!
    PAUSE
)

:: 생성된 파일 복사
echo [INFO] Copying generated files to SwarmServer...
XCOPY /Y Enum.pb.h "%SERVER_PATH%\Protocol"
XCOPY /Y Enum.pb.cc "%SERVER_PATH%\Protocol"
XCOPY /Y Struct.pb.h "%SERVER_PATH%\Protocol"
XCOPY /Y Struct.pb.cc "%SERVER_PATH%\Protocol"
XCOPY /Y Protocol.pb.h "%SERVER_PATH%\Protocol"
XCOPY /Y Protocol.pb.cc "%SERVER_PATH%\Protocol"
XCOPY /Y PacketHandler.cpp "%SERVER_PATH%\Packet"
XCOPY /Y PacketId.h "%SERVER_PATH%\Packet"

echo [INFO] Copying generated files to DummyClient...

XCOPY /Y Enum.pb.h "%DUMMY_CLIENT_PATH%\Protocol"
XCOPY /Y Enum.pb.cc "%DUMMY_CLIENT_PATH%\Protocol"
XCOPY /Y Struct.pb.h "%DUMMY_CLIENT_PATH%\Protocol"
XCOPY /Y Struct.pb.cc "%DUMMY_CLIENT_PATH%\Protocol"
XCOPY /Y Protocol.pb.h "%DUMMY_CLIENT_PATH%\Protocol"
XCOPY /Y Protocol.pb.cc "%DUMMY_CLIENT_PATH%\Protocol"
XCOPY /Y PacketHandler.cpp "%DUMMY_CLIENT_PATH%\Packet"
XCOPY /Y PacketId.h "%DUMMY_CLIENT_PATH%\Packet"

:: 생성된 파일 정리
echo [INFO] Cleaning up temporary files...
DEL /Q /F *.pb.h
DEL /Q /F *.pb.cc
DEL /Q /F *.h
DEL /Q /F *.cpp

:: 완료
echo [SUCCESS] All generation completed.
PAUSE