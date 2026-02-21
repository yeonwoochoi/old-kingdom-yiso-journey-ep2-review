#include "ChatHandler.h"
#include "Network/PacketCodec.h"
#include "game_packet.pb.h"
#include <iostream>

namespace Yiso::Game
{
    ChatHandler::ChatHandler(Network::YisoSessionManager& manager)
        : session_manager_(manager)
    {
    }

    void ChatHandler::OnConnected(Network::YisoSession::SessionId id)
    {
        std::cout << "[Chat] Session " << id << " connected\n";

        yiso::game::S2C_Chat msg;
        msg.set_session_id(0);
        msg.set_message("Session " + std::to_string(id) + " joined.");
        session_manager_.Broadcast(Network::PacketCodec::Encode(Network::PacketType::S2C_CHAT, msg));
    }

    void ChatHandler::OnDisconnected(Network::YisoSession::SessionId id)
    {
        std::cout << "[Chat] Session " << id << " disconnected\n";

        yiso::game::S2C_Chat msg;
        msg.set_session_id(0);
        msg.set_message("Session " + std::to_string(id) + " left.");
        session_manager_.Broadcast(Network::PacketCodec::Encode(Network::PacketType::S2C_CHAT, msg));
    }

    void ChatHandler::OnRecv(Network::YisoSession::SessionId id, Network::PacketType type, const uint8_t* data, uint32_t size)
    {
        if (type != Network::PacketType::C2S_CHAT)
            return;

        yiso::game::C2S_Chat req;
        if (!req.ParseFromArray(data, static_cast<int>(size)))
        {
            std::cerr << "[Chat] ParseFromArray failed (session=" << id << ")\n";
            return;
        }

        std::cout << "[Chat] " << id << ": " << req.message() << "\n";

        yiso::game::S2C_Chat resp;
        resp.set_session_id(id);
        resp.set_message(req.message());
        session_manager_.Broadcast(Network::PacketCodec::Encode(Network::PacketType::S2C_CHAT, resp));
    }
}
