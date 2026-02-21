#pragma once
#include <cstdint>

namespace Yiso::Network
{
    enum class PacketType : uint16_t
    {
        UNKNOWN = 0,

        // Client -> Server
        C2S_MOVE  = 1,
        C2S_CHAT  = 2,

        // Server -> Client
        S2C_CHAT  = 100,
    };
    
    // 패킷 프레임 포맷:
    // [ body_size: 4 bytes (uint32) ][ packet_type: 2 bytes (uint16) ][ payload: body_size bytes ]
#pragma pack(push, 1)
    struct PacketHeader
    {   
        uint32_t body_size; // protobuf의 payload 크기 (헤더 제외)
        uint16_t type; // PacketType
    };
#pragma pack(pop)
    
    constexpr uint32_t HEADER_SIZE = sizeof(PacketHeader);  // 6 bytes
}
