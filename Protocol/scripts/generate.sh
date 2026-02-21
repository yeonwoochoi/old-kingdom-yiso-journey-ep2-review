#!/bin/bash
# 현재 스크립트가 있는 경로의 상위 폴더를 root로 잡음
PROJECT_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROTOC=protoc

# 1. 기존 생성 파일 정리
rm -rf "$PROJECT_ROOT/Generated"
mkdir -p "$PROJECT_ROOT/Generated/Cpp"
mkdir -p "$PROJECT_ROOT/Generated/CSharp"

# 2. Protoc 실행 (cpp, csharp 둘 다 동시에)
echo "[Protocol] Generating Codes..."
$PROTOC -I="$PROJECT_ROOT/Schemas" \
        --cpp_out="$PROJECT_ROOT/Generated/Cpp" \
        --csharp_out="$PROJECT_ROOT/Generated/CSharp" \
        "$PROJECT_ROOT/Schemas/"*.proto

# 3. 결과 확인
if [ $? -ne 0 ]; then
    echo "[Error] Protobuf generation failed!"
    exit 1
fi

echo "[Success] Protocol generated successfully."
