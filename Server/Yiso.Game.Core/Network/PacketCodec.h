#pragma once
#include "PacketHeader.h"
#include <google/protobuf/message.h>
#include <vector>
#include <cstdint>

namespace Yiso::Network
{
    class PacketCodec
    {
    public:
        // protobuf 메시지 -> [헤더 6바이트 + 페이로드] 바이트 배열
        static std::vector<uint8_t> Encode(PacketType type, const google::protobuf::Message& msg);
    };
}
