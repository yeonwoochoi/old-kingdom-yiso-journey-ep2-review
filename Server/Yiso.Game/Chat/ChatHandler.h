#pragma once
#include "Network/YisoSession.h"
#include "Network/YisoSessionManager.h"

namespace Yiso::Game
{
    class ChatHandler
    {
    public:
        explicit ChatHandler(Network::YisoSessionManager& manager);

        void OnConnected(Network::YisoSession::SessionId id);
        void OnDisconnected(Network::YisoSession::SessionId id);
        void OnRecv(Network::YisoSession::SessionId id, Network::PacketType type, const uint8_t* data, uint32_t size);

    private:
        Network::YisoSessionManager& session_manager_;
    };
}
