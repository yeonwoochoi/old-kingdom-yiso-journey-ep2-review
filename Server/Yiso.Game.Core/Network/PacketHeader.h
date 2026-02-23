#pragma once
#include <cstdint>

namespace Yiso::Network
{
    enum class PacketType : uint16_t
    {
        UNKNOWN = 0,

        // Client -> Server
        C2S_CHAT = 1,
        C2S_WHISPER = 2,
        C2S_CREATE_ROOM = 3,
        C2S_DELETE_ROOM = 4,
        C2S_JOIN_ROOM = 5,
        C2S_LEAVE_ROOM = 6,
        C2S_ROOM_CHAT = 7,

        // Server -> Client
        S2C_CHAT = 1001,
        S2C_WHISPER = 1002,
        S2C_CREATE_ROOM = 1003,
        S2C_DELETE_ROOM = 1004,
        S2C_JOIN_ROOM = 1005,
        S2C_LEAVE_ROOM = 1006,
        S2C_ROOM_CHAT = 1007,
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

    inline bool IsValidPacketType(uint16_t type)
    {
        switch (static_cast<PacketType>(type))
        {
        case PacketType::C2S_CHAT:
        case PacketType::C2S_CREATE_ROOM:
        case PacketType::C2S_DELETE_ROOM:
        case PacketType::C2S_JOIN_ROOM:
        case PacketType::C2S_LEAVE_ROOM:
        case PacketType::C2S_ROOM_CHAT:
        case PacketType::C2S_WHISPER:
            return true;
        default:
            return false;
        }
    }

    constexpr uint32_t HEADER_SIZE = sizeof(PacketHeader); // 6 bytes
    constexpr uint32_t MAX_PACKET_SIZE = 64 * 1024; // 64kb
}
