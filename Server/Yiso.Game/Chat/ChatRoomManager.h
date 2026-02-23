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

        RoomId CreateRoom(SessionId creator, const std::string& name);
        bool RemoveRoom(RoomId id);
        bool JoinRoom(RoomId id, SessionId session);
        bool LeaveRoom(RoomId id, SessionId session);
        void RemoveSession(SessionId session); // disconnect 시 모든 방에서 제거 (강제 종료된 경우엔 유령 유저 생기니까)
        std::vector<SessionId> GetMembers(RoomId id) const;
        bool IsCreator(RoomId id, SessionId session) const;

    private:
        struct Room
        {
            std::string name;
            SessionId creator;
            std::unordered_set<SessionId> members;
        };

        Room* FindRoom(RoomId id);
        const Room* FindRoom(RoomId id) const;

        mutable std::mutex mutex_;
        std::map<RoomId, Room> rooms_;
        std::atomic<uint32_t> next_id_{1};
    };
}
