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

    bool ChatRoomManager::RemoveRoom(RoomId id)
    {
        std::lock_guard lock(mutex_);
        return rooms_.erase(id) > 0;
    }

    bool ChatRoomManager::JoinRoom(RoomId id, SessionId session)
    {
        std::lock_guard lock(mutex_);

        Room* room = FindRoom(id);
        if (!room) return false;
        if (room->members.count(session) > 0) return false;

        room->members.insert(session);
        return true;
    }

    bool ChatRoomManager::LeaveRoom(RoomId id, SessionId session)
    {
        std::lock_guard lock(mutex_);

        Room* room = FindRoom(id);
        if (!room) return false;

        return room->members.erase(session) > 0;
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

    bool ChatRoomManager::IsCreator(RoomId id, SessionId session) const
    {
        std::lock_guard lock(mutex_);
        const Room* room = FindRoom(id);
        return room && room->creator == session;
    }
}
