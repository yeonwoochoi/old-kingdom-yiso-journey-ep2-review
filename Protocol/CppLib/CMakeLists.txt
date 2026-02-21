cmake_minimum_required(VERSION 3.10)
project(YisoProtocol)

# Protobuf 패키지 찾기
find_package(Protobuf REQUIRED)

# Generated 폴더의 소스 파일들 지정
file(GLOB PROTO_SRCS "../Generated/Cpp/*.pb.cc")
file(GLOB PROTO_HDRS "../Generated/Cpp/*.pb.h")

# 정적 라이브러리(Static Library) 생성
add_library(YisoProtocol STATIC ${PROTO_SRCS} ${PROTO_HDRS})

# Include 경로 설정 (외부에서 이 라이브러리를 쓸 때 헤더 경로 자동 포함)
target_include_directories(YisoProtocol PUBLIC 
    "${CMAKE_CURRENT_SOURCE_DIR}/../Generated/Cpp"
)

# Protobuf 라이브러리 링크
target_link_libraries(YisoProtocol PUBLIC protobuf::libprotobuf)