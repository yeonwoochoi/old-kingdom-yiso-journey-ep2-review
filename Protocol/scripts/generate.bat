@echo off
:: 현재 스크립트가 있는 경로의 상위 폴더를 root로 잡음 (.git 있는 폴더)
SET PROJECT_ROOT=%~dp0..
SET PROTOC=C:\vcpkg\installed\x64-windows\tools\protobuf\protoc.exe

:: 1. 기존 생성 파일 정리
if exist "%PROJECT_ROOT%\Generated" rmdir /s /q "%PROJECT_ROOT%\Generated"
mkdir "%PROJECT_ROOT%\Generated\Cpp"
mkdir "%PROJECT_ROOT%\Generated\CSharp"

:: 2. Protoc 실행 (cpp, csharp 둘 다 동시에)
echo [Protocol] Generating Codes...
%PROTOC% -I="%PROJECT_ROOT%\Schemas" ^
         --cpp_out="%PROJECT_ROOT%\Generated\Cpp" ^
         --csharp_out="%PROJECT_ROOT%\Generated\CSharp" ^
         "%PROJECT_ROOT%\Schemas\*.proto"
        
:: 3. 결과 확인
if %ERRORLEVEL% NEQ 0 (
    echo [Error] Protobuf generation failed!
    exit /b %ERRORLEVEL%
)

echo [Success] Protocol generated successfully.
exit /b 0