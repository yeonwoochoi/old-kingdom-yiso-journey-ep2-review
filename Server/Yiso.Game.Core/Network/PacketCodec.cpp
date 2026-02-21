#include "PacketCodec.h"
#include "string"
#include <google/protobuf/message.h>

namespace Yiso::Network
{
    std::vector<uint8_t> PacketCodec::Encode(PacketType type, const google::protobuf::Message& msg)
    {
        std::string payload = msg.SerializeAsString();

        PacketHeader header;
        header.body_size = static_cast<uint32_t>(payload.size());
        header.type = static_cast<uint16_t>(type);

        std::vector<uint8_t> frame(HEADER_SIZE + payload.size());
        std::memcpy(frame.data(), &header, HEADER_SIZE);
        std::memcpy(frame.data() + HEADER_SIZE, payload.data(), payload.size());

        return frame;
    }
}
