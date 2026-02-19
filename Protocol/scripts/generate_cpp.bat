@echo off
setlocal

REM ============================================================
REM  .proto → C++ 코드 생성 스크립트
REM  생성 위치: Server/Yiso.Game.Packet/Generated/
REM ============================================================

set SCRIPT_DIR=%~dp0
set PROTO_DIR=%SCRIPT_DIR%..\Protos
set CPP_OUT=%SCRIPT_DIR%..\..\Server\Yiso.Game.Packet\Generated

REM Generated 폴더 초기화
if exist "%CPP_OUT%" rmdir /s /q "%CPP_OUT%"
mkdir "%CPP_OUT%"

REM protoc 실행
protoc --proto_path="%PROTO_DIR%" --cpp_out="%CPP_OUT%" "%PROTO_DIR%\*.proto"

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] protoc 실행 실패. protoc가 PATH에 있는지 확인하세요.
    exit /b 1
)

echo [OK] C++ 코드 생성 완료: %CPP_OUT%
