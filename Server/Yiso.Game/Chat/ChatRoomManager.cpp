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
        newRoom.owner = creator;
        newRoom.name = name;
        newRoom.members.insert(creator);
        newRoom.join_order.push_back(creator);

        rooms_.insert(std::make_pair(id, std::move(newRoom)));

        return id;
    }

    ChatRoomManager::RoomOperatorResult ChatRoomManager::TryRemoveRoom(RoomId id, SessionId requester)
    {
        std::lock_guard lock(mutex_);

        const Room* room = FindRoom(id);
        if (!room)
            return { false, "존재하지 않는 방입니다." };

        if (room->owner != requester)
            return { false, "권한이 없습니다." };

        SessionId owner = room->owner;
        std::vector<SessionId> members(room->members.begin(), room->members.end());
        rooms_.erase(id);

        return { true, {}, std::move(members), owner };
    }

    ChatRoomManager::RoomOperatorResult ChatRoomManager::TryJoinRoom(RoomId id, SessionId session)
    {
        std::lock_guard lock(mutex_);

        Room* room = FindRoom(id);
        if (!room)
            return { false, "존재하지 않는 방입니다." };
        if (room->members.count(session) > 0)
            return { false, "이미 입장한 방입니다." };

        room->members.insert(session);
        room->join_order.push_back(session);
        
        // 입장 후 멤버 목록 (입장자 포함)
        std::vector<SessionId> members(room->members.begin(), room->members.end());
        return { true, {}, std::move(members), room->owner };
    }

    ChatRoomManager::RoomOperatorResult ChatRoomManager::TryLeaveRoom(RoomId id, SessionId session)
    {
        std::lock_guard lock(mutex_);

        Room* room = FindRoom(id);
        if (!room)
            return { false, "존재하지 않는 방입니다." };
        if (room->members.count(session) == 0)
            return { false, "해당 방의 멤버가 아닙니다." };
        
        if (room->members.size() == 1)
        {
            std::vector<SessionId> members(room->members.begin(), room->members.end());
            SessionId owner = room->owner;
            rooms_.erase(id);

            return { true, {}, std::move(members), owner };
        }

        // 퇴장 전 스냅샷
        std::vector<SessionId> members(
            room->members.begin(),
            room->members.end()
        );

        // 일단 멤버 제거
        room->members.erase(session);

        // join_order 에서 제거
        auto it = std::find(room->join_order.begin(), room->join_order.end(), session);
        if (it != room->join_order.end())
            room->join_order.erase(it);

        // 방장이 나간 경우 그 다음으로 먼저 들어어온 사람에게 방장 위임
        if (room->owner == session)
        {
            room->owner = room->join_order.front();
        }
        
        return { true, {}, std::move(members), room->owner };
    }

    std::vector<ChatRoomManager::RoomChangeInfo> ChatRoomManager::RemoveSession(SessionId session)
    {
        std::lock_guard lock(mutex_);
        std::vector<RoomChangeInfo> changes;
        std::vector<RoomId> emptyRooms;

        for (auto& [id, room] : rooms_)
        {
            if (room.members.count(session) == 0)
                continue;

            room.members.erase(session);

            auto it = std::find(room.join_order.begin(), room.join_order.end(), session);
            if (it != room.join_order.end())
                room.join_order.erase(it);

            if (room.join_order.empty())
            {
                emptyRooms.push_back(id);
                continue;
            }

            if (session == room.owner)
                room.owner = room.join_order.front();

            std::vector<SessionId> remaining(room.members.begin(), room.members.end());
            changes.push_back({ id, room.owner, std::move(remaining) });
        }

        for (auto roomId : emptyRooms)
            rooms_.erase(roomId);

        return changes;
    }

    std::vector<ChatRoomManager::SessionId> ChatRoomManager::GetMembers(RoomId id) const
    {
        std::lock_guard lock(mutex_);

        const Room* room = FindRoom(id);
        if (!room) return {};

        return std::vector<SessionId>(room->members.begin(), room->members.end());
    }
}
