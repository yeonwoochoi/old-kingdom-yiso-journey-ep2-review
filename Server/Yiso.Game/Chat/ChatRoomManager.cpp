#include "ChatRoomManager.h"

namespace Yiso::Game
{
    // 헬퍼 클래스
    ChatRoomManager::Room* ChatRoomManager::FindRoom(RoomId id)
    {
        auto it = rooms_.find(id);
        return it != rooms_.end() ? &it->second : nullptr;
    }

    const ChatRoomManager::Room* ChatRoomManager::FindRoom(RoomId id) const
    {
        auto it = rooms_.find(id);
        return it != rooms_.end() ? &it->second : nullptr;
    }

    ChatRoomManager::RoomId ChatRoomManager::CreateRoom(SessionId creator, const std::string& name)
    {
        std::lock_guard lock(mutex_);

        RoomId id = next_id_++;

        Room newRoom;
        newRoom.creator = creator;
        newRoom.name = name;
        newRoom.members.insert(creator);

        rooms_.insert(std::make_pair(id, std::move(newRoom)));

        return id;
    }

    ChatRoomManager::RoomOpResult ChatRoomManager::TryRemoveRoom(RoomId id, SessionId requester)
    {
        std::lock_guard lock(mutex_);

        const Room* room = FindRoom(id);
        if (!room)
            return { false, "존재하지 않는 방입니다." };

        if (room->creator != requester)
            return { false, "권한이 없습니다." };

        std::vector<SessionId> members(room->members.begin(), room->members.end());
        rooms_.erase(id);

        return { true, {}, std::move(members) };
    }

    ChatRoomManager::RoomOpResult ChatRoomManager::TryJoinRoom(RoomId id, SessionId session)
    {
        std::lock_guard lock(mutex_);

        Room* room = FindRoom(id);
        if (!room)
            return { false, "존재하지 않는 방입니다." };
        if (room->members.count(session) > 0)
            return { false, "이미 입장한 방입니다." };

        room->members.insert(session);

        // 입장 후 멤버 목록 (입장자 포함)
        std::vector<SessionId> members(room->members.begin(), room->members.end());
        return { true, {}, std::move(members) };
    }

    ChatRoomManager::RoomOpResult ChatRoomManager::TryLeaveRoom(RoomId id, SessionId session)
    {
        std::lock_guard lock(mutex_);

        Room* room = FindRoom(id);
        if (!room)
            return { false, "존재하지 않는 방입니다." };
        if (room->members.count(session) == 0)
            return { false, "해당 방의 멤버가 아닙니다." };

        // 퇴장 전 멤버 목록 (퇴장자 포함 -> 퇴장자에게도 알림)
        std::vector<SessionId> members(room->members.begin(), room->members.end());
        room->members.erase(session);

        return { true, {}, std::move(members) };
    }

    void ChatRoomManager::RemoveSession(SessionId session)
    {
        std::lock_guard lock(mutex_);
        for (auto& [_, room] : rooms_)
            room.members.erase(session);
    }

    std::vector<ChatRoomManager::SessionId> ChatRoomManager::GetMembers(RoomId id) const
    {
        std::lock_guard lock(mutex_);

        const Room* room = FindRoom(id);
        if (!room) return {};

        return std::vector<SessionId>(room->members.begin(), room->members.end());
    }

}
