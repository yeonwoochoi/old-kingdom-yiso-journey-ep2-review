#include "ChatHandler.h"
#include "Network/PacketCodec.h"
#include "game_packet.pb.h"
#include <spdlog/spdlog.h>

namespace Yiso::Game
{
    ChatHandler::ChatHandler(Network::YisoSessionManager& manager)
        : session_manager_(manager)
    {
    }

    void ChatHandler::OnConnected(Network::YisoSession::SessionId id)
    {
        spdlog::info("[Chat] Session {} connected", id);

        yiso::game::S2C_Chat msg;
        msg.set_session_id(0);
        msg.set_message("Session " + std::to_string(id) + " joined.");
        session_manager_.Broadcast(Network::PacketCodec::Encode(Network::PacketType::S2C_CHAT, msg));
    }

    void ChatHandler::OnDisconnected(Network::YisoSession::SessionId id)
    {
        spdlog::info("[Chat] Session {} disconnected", id);

        auto changes = room_manager_.RemoveSession(id);
        for (auto& change : changes)
        {
            yiso::game::S2C_LeaveRoom resp;
            resp.set_room_id(change.room_id);
            resp.set_left_session(id);
            resp.set_new_owner(change.new_owner);
            auto frame = Network::PacketCodec::Encode(Network::PacketType::S2C_LEAVE_ROOM, resp);
            for (auto memberId : change.members)
                session_manager_.Send(memberId, frame);
        }

        yiso::game::S2C_Chat msg;
        msg.set_session_id(0);
        msg.set_message("Session " + std::to_string(id) + " left.");
        session_manager_.Broadcast(Network::PacketCodec::Encode(Network::PacketType::S2C_CHAT, msg));
    }

    void ChatHandler::OnRecv(Network::YisoSession::SessionId id, Network::PacketType type, const uint8_t* data, uint32_t size)
    {
        switch (type)
        {
        case Network::PacketType::C2S_CHAT:
            HandleChat(id, data, size);
            break;
        case Network::PacketType::C2S_WHISPER:
            HandleWhisper(id, data, size);
            break;
        case Network::PacketType::C2S_CREATE_ROOM:
            HandleCreateRoom(id, data, size);
            break;
        case Network::PacketType::C2S_DELETE_ROOM:
            HandleDeleteRoom(id, data, size);
            break;
        case Network::PacketType::C2S_JOIN_ROOM:
            HandleJoinRoom(id, data, size);
            break;
        case Network::PacketType::C2S_LEAVE_ROOM:
            HandleLeaveRoom(id, data, size);
            break;
        case Network::PacketType::C2S_ROOM_CHAT:
            HandleRoomChat(id, data, size);
            break;
        }
    }

    void ChatHandler::HandleChat(Network::YisoSession::SessionId id, const uint8_t* data, uint32_t size)
    {
        yiso::game::C2S_Chat req;
        if (!req.ParseFromArray(data, static_cast<int>(size)))
        {
            spdlog::warn("[Chat] ParseFromArray failed (session={})", id);
            return;
        }

        spdlog::info("[Chat] {} : {}", id, req.message());

        yiso::game::S2C_Chat resp;
        resp.set_session_id(id);
        resp.set_message(req.message());
        session_manager_.Broadcast(Network::PacketCodec::Encode(Network::PacketType::S2C_CHAT, resp));
    }

    void ChatHandler::HandleWhisper(Network::YisoSession::SessionId id, const uint8_t* data, uint32_t size)
    {
        yiso::game::C2S_Whisper req;
        if (!req.ParseFromArray(data, static_cast<int>(size)))
        {
            spdlog::warn("[Chat] Whisper ParseFromArray failed (session={})", id);
            return;
        }

        yiso::game::S2C_Whisper resp;
        resp.set_from_session_id(id);

        if (!session_manager_.HasSession(req.target_session_id()))
        {
            spdlog::warn("[Chat] target session={} is not found", req.target_session_id());
            resp.set_message("해당 유저가 없습니다.");
            session_manager_.Send(id, Network::PacketCodec::Encode(Network::PacketType::S2C_WHISPER, resp));
            return;
        }
        
        resp.set_message(req.message());   
        session_manager_.Send(req.target_session_id(), Network::PacketCodec::Encode(Network::PacketType::S2C_WHISPER, resp));
    }

    void ChatHandler::HandleCreateRoom(Network::YisoSession::SessionId id, const uint8_t* data, uint32_t size)
    {
        yiso::game::C2S_CreateRoom req;
        if (!req.ParseFromArray(data, static_cast<int>(size)))
        {
            spdlog::warn("[Chat] CreateRoom ParseFromArray failed (session={})", id);
            return;
        }

        ChatRoomManager::RoomId roomId = room_manager_.CreateRoom(id, req.room_name());

        spdlog::info("[Chat] Room {} ('{}') created by session {}", roomId, req.room_name(), id);

        yiso::game::S2C_CreateRoom resp;
        resp.set_room_id(roomId);
        resp.set_room_name(req.room_name());
        resp.set_success(true);
        session_manager_.Send(id, Network::PacketCodec::Encode(Network::PacketType::S2C_CREATE_ROOM, resp));
    }

    void ChatHandler::HandleDeleteRoom(Network::YisoSession::SessionId id, const uint8_t* data, uint32_t size)
    {
        yiso::game::C2S_DeleteRoom req;
        if (!req.ParseFromArray(data, static_cast<int>(size)))
        {
            spdlog::warn("[Chat] DeleteRoom ParseFromArray failed (session={})", id);
            return;
        }

        ChatRoomManager::RoomId roomId = req.room_id();
        auto result = room_manager_.TryRemoveRoom(roomId, id);

        if (!result.success)
        {
            yiso::game::S2C_DeleteRoom resp;
            resp.set_room_id(roomId);
            resp.set_success(false);
            resp.set_error(result.error);
            session_manager_.Send(id, Network::PacketCodec::Encode(Network::PacketType::S2C_DELETE_ROOM, resp));
            return;
        }

        spdlog::info("[Chat] Room {} deleted by session {}", roomId, id);

        yiso::game::S2C_DeleteRoom resp;
        resp.set_room_id(roomId);
        resp.set_success(true);
        auto frame = Network::PacketCodec::Encode(Network::PacketType::S2C_DELETE_ROOM, resp);
        for (auto memberId : result.members)
            session_manager_.Send(memberId, frame);
    }

    void ChatHandler::HandleJoinRoom(Network::YisoSession::SessionId id, const uint8_t* data, uint32_t size)
    {
        yiso::game::C2S_JoinRoom req;
        if (!req.ParseFromArray(data, static_cast<int>(size)))
        {
            spdlog::warn("[Chat] JoinRoom ParseFromArray failed (session={})", id);
            return;
        }

        ChatRoomManager::RoomId roomId = req.room_id();
        auto result = room_manager_.TryJoinRoom(roomId, id);

        if (!result.success)
        {
            yiso::game::S2C_JoinRoom resp;
            resp.set_room_id(roomId);
            resp.set_success(false);
            resp.set_error(result.error);
            session_manager_.Send(id, Network::PacketCodec::Encode(Network::PacketType::S2C_JOIN_ROOM, resp));
            return;
        }

        spdlog::info("[Chat] Session {} joined room {}", id, roomId);

        yiso::game::S2C_JoinRoom resp;
        resp.set_room_id(roomId);
        resp.set_joined_session(id);
        resp.set_success(true);
        auto frame = Network::PacketCodec::Encode(Network::PacketType::S2C_JOIN_ROOM, resp);
        for (auto memberId : result.members)
            session_manager_.Send(memberId, frame);
    }

    void ChatHandler::HandleLeaveRoom(Network::YisoSession::SessionId id, const uint8_t* data, uint32_t size)
    {
        yiso::game::C2S_LeaveRoom req;
        if (!req.ParseFromArray(data, static_cast<int>(size)))
        {
            spdlog::warn("[Chat] LeaveRoom ParseFromArray failed (session={})", id);
            return;
        }

        ChatRoomManager::RoomId roomId = req.room_id();
        auto result = room_manager_.TryLeaveRoom(roomId, id);

        if (!result.success)
        {
            spdlog::warn("[Chat] Session {} failed to leave room {}: {}", id, roomId, result.error);
            return;
        }

        spdlog::info("[Chat] Session {} left room {}", id, roomId);

        yiso::game::S2C_LeaveRoom resp;
        resp.set_room_id(roomId);
        resp.set_left_session(id);
        resp.set_new_owner(result.new_owner);
        auto frame = Network::PacketCodec::Encode(Network::PacketType::S2C_LEAVE_ROOM, resp);
        for (auto memberId : result.members)
            session_manager_.Send(memberId, frame);
    }

    void ChatHandler::HandleRoomChat(Network::YisoSession::SessionId id, const uint8_t* data, uint32_t size)
    {
        yiso::game::C2S_RoomChat req;
        if (!req.ParseFromArray(data, static_cast<int>(size)))
        {
            spdlog::warn("[Chat] RoomChat ParseFromArray failed (session={})", id);
            return;
        }

        ChatRoomManager::RoomId roomId = req.room_id();
        auto members = room_manager_.GetMembers(roomId);

        if (members.empty())
        {
            spdlog::warn("[Chat] Session {} sent chat to non-existent room {}", id, roomId);
            return;
        }

        if (std::find(members.begin(), members.end(), id) == members.end())
        {
            spdlog::warn("[Chat] Session {} is not a member of room {}", id, roomId);
            return;
        }

        spdlog::info("[Chat] Room {} | {} : {}", roomId, id, req.message());

        yiso::game::S2C_RoomChat resp;
        resp.set_room_id(roomId);
        resp.set_from_session_id(id);
        resp.set_message(req.message());
        auto frame = Network::PacketCodec::Encode(Network::PacketType::S2C_ROOM_CHAT, resp);
        for (auto memberId : members)
            session_manager_.Send(memberId, frame);
    }
}
