@echo off
SET PROJECT_ROOT=%~dp0..
SET REPO_ROOT=%~dp0..\..
SET PROTOC=C:\vcpkg\installed\x64-windows\tools\protobuf\protoc.exe
SET UNITY_PROTO_OUT=%REPO_ROOT%\Client\Assets\Scripts\Network\Protocol

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

if %ERRORLEVEL% NEQ 0 (
    echo [Error] Protobuf generation failed!
    exit /b %ERRORLEVEL%
)

:: 3. Unity 프로젝트로 C# 파일 복사
echo [Protocol] Copying C# files to Unity...
if not exist "%UNITY_PROTO_OUT%" mkdir "%UNITY_PROTO_OUT%"
xcopy /Y "%PROJECT_ROOT%\Generated\CSharp\*.cs" "%UNITY_PROTO_OUT%\"

if %ERRORLEVEL% NEQ 0 (
    echo [Error] Copy to Unity failed!
    exit /b %ERRORLEVEL%
)

echo [Success] Protocol generated and copied to Unity successfully.
exit /b 0
