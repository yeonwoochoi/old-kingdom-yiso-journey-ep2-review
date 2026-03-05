#pragma once
#include "Network/YisoSession.h"
#include <atomic>
#include <map>
#include <mutex>
#include <string>
#include <unordered_set>
#include <vector>

namespace Yiso::Game
{
    class ChatRoomManager
    {
    public:
        using RoomId = uint32_t;
        using SessionId = Network::YisoSession::SessionId;

        struct RoomOperatorResult
        {
            bool success;
            std::string error;
            std::vector<SessionId> members;
            SessionId new_owner;
        };

        struct RoomChangeInfo
        {
            RoomId room_id;
            SessionId new_owner;
            std::vector<SessionId> members; // 남은 멤버 (알림 대상)
        };

        RoomId CreateRoom(SessionId creator, const std::string& name);
        RoomOperatorResult TryRemoveRoom(RoomId id, SessionId requester);
        RoomOperatorResult TryJoinRoom(RoomId id, SessionId session);
        RoomOperatorResult TryLeaveRoom(RoomId id, SessionId session);
        std::vector<RoomChangeInfo> RemoveSession(SessionId session); // disconnect 시 모든 방에서 제거
        std::vector<SessionId> GetMembers(RoomId id) const;

    private:
        struct Room
        {
            std::string name;
            SessionId owner;
            std::unordered_set<SessionId> members;
            std::deque<SessionId> join_order; 
        };

        Room* FindRoom(RoomId id);
        const Room* FindRoom(RoomId id) const;

        mutable std::mutex mutex_;
        std::map<RoomId, Room> rooms_;
        std::atomic<uint32_t> next_id_{1};
    };
}
