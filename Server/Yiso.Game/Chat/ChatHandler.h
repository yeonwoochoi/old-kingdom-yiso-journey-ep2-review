#pragma once
#include "Network/YisoSession.h"
#include "Network/YisoSessionManager.h"
#include "ChatRoomManager.h"

namespace Yiso::Game
{
    class ChatHandler
    {
    public:
        using SessionId = Network::YisoSession::SessionId;
        explicit ChatHandler(Network::YisoSessionManager& manager);

        void OnConnected(SessionId id);
        void OnDisconnected(SessionId id);
        void OnRecv(SessionId id, Network::PacketType type, const uint8_t* data, uint32_t size);

    private:
        void HandleChat(SessionId id, const uint8_t* data, uint32_t size);
        void HandleWhisper(SessionId id, const uint8_t* data, uint32_t size);
        void HandleCreateRoom(SessionId id, const uint8_t* data, uint32_t size);
        void HandleDeleteRoom(SessionId id, const uint8_t* data, uint32_t size);
        void HandleJoinRoom(SessionId id, const uint8_t* data, uint32_t size);
        void HandleLeaveRoom(SessionId id, const uint8_t* data, uint32_t size);
        void HandleRoomChat(SessionId id, const uint8_t* data, uint32_t size);

        Network::YisoSessionManager& session_manager_;
        ChatRoomManager room_manager_;
    };
}
