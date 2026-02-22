#include "YisoSessionManager.h"

namespace Yiso::Network
{
    void YisoSessionManager::AddSession(std::shared_ptr<YisoSession> session)
    {
        std::lock_guard lock(mutex_);
        sessions_[session->GetId()] = std::move(session);
    }

    void YisoSessionManager::RemoveSession(SessionId id)
    {
        std::lock_guard lock(mutex_);
        sessions_.erase(id);
    }

    void YisoSessionManager::Broadcast(std::vector<uint8_t> frame)
    {
        std::lock_guard lock(mutex_);
        for (auto& [id, session] : sessions_)
            session->Send(frame);
    }

    void YisoSessionManager::Send(SessionId id, std::vector<uint8_t> frame)
    {
        std::lock_guard lock(mutex_);
        auto it = sessions_.find(id);
        if (it != sessions_.end())
            it->second->Send(std::move(frame));
    }

    void YisoSessionManager::DisconnectAll()
    {
        for (auto& [id, session] : sessions_)
        {
            session->Disconnect();
        }
    }
}
