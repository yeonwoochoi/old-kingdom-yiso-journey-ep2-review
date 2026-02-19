# ============================================================
#  .proto → C#, C++ 코드 일괄 생성 스크립트
#  사용법:
#    .\generate.ps1           # C# + C++ 모두 생성
#    .\generate.ps1 -CsOnly   # C# 만 생성
#    .\generate.ps1 -CppOnly  # C++ 만 생성
# ============================================================

param(
    [switch]$CsOnly,
    [switch]$CppOnly
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProtosDir = (Resolve-Path (Join-Path $ScriptDir "..\Protos")).Path
$CsOutDir  = Join-Path $ScriptDir "..\Yiso.Protocol\Generated"
$CppOutDir = Join-Path $ScriptDir "..\..\Server\Yiso.Game.Packet\Generated"

# 경로 정규화
$CsOutDir  = [System.IO.Path]::GetFullPath($CsOutDir)
$CppOutDir = [System.IO.Path]::GetFullPath($CppOutDir)

# protoc 확인 (PATH → 알려진 설치 경로 순으로 탐색)
$Protoc = "protoc"
if (-not (Get-Command $Protoc -ErrorAction SilentlyContinue)) {
    $KnownPaths = @("C:\Protobuf\bin\protoc.exe")
    $Found = $KnownPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($Found) {
        $Protoc = $Found
    } else {
        Write-Error "[ERROR] protoc를 찾을 수 없습니다. PATH에 protoc가 있는지 확인하세요."
        exit 1
    }
}

$ProtoFiles = (Get-ChildItem -Path $ProtosDir -Filter "*.proto").FullName
if (-not $ProtoFiles) {
    Write-Error "[ERROR] $ProtosDir 에서 .proto 파일을 찾을 수 없습니다."
    exit 1
}

function Generate-Cs {
    Write-Host "[C#] 코드 생성 중..."

    if (Test-Path $CsOutDir) { Remove-Item -Recurse -Force $CsOutDir }
    New-Item -ItemType Directory -Force -Path $CsOutDir | Out-Null

    & $Protoc --proto_path="$ProtosDir" --csharp_out="$CsOutDir" $ProtoFiles
    if ($LASTEXITCODE -ne 0) {
        Write-Error "[ERROR] C# 코드 생성 실패"
        exit 1
    }
    Write-Host "[C#] 생성 완료: $CsOutDir"
}

function Generate-Cpp {
    Write-Host "[C++] 코드 생성 중..."

    if (Test-Path $CppOutDir) { Remove-Item -Recurse -Force $CppOutDir }
    New-Item -ItemType Directory -Force -Path $CppOutDir | Out-Null

    & $Protoc --proto_path="$ProtosDir" --cpp_out="$CppOutDir" $ProtoFiles
    if ($LASTEXITCODE -ne 0) {
        Write-Error "[ERROR] C++ 코드 생성 실패"
        exit 1
    }
    Write-Host "[C++] 생성 완료: $CppOutDir"
}

if ($CsOnly) {
    Generate-Cs
} elseif ($CppOnly) {
    Generate-Cpp
} else {
    Generate-Cs
    Generate-Cpp
    Write-Host ""
    Write-Host "[완료] C# + C++ 코드 생성 완료"
}
