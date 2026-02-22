#include "YisoSessionManager.h"
#include <spdlog/spdlog.h>

namespace Yiso::Network
{
    void YisoSessionManager::AddSession(std::shared_ptr<YisoSession> session)
    {
        std::lock_guard lock(mutex_);
        spdlog::info("[SessionManager] 세션 추가 id={}", session->GetId());
        sessions_[session->GetId()] = std::move(session);
    }

    void YisoSessionManager::RemoveSession(SessionId id)
    {
        std::lock_guard lock(mutex_);
        spdlog::info("[SessionManager] 세션 제거 id={}", id);
        sessions_.erase(id);
    }

    void YisoSessionManager::Broadcast(std::vector<uint8_t> frame)
    {
        std::vector<std::shared_ptr<YisoSession>> snapshot;
        {
            std::lock_guard lock(mutex_);
            for (auto& [id, session] : sessions_)
                snapshot.push_back(session);
        }
        spdlog::debug("[SessionManager] Broadcast {} 세션", snapshot.size());
        for (auto& session : snapshot)
            session->Send(frame);
    }

    void YisoSessionManager::Send(SessionId id, std::vector<uint8_t> frame)
    {
        std::shared_ptr<YisoSession> target;
        {
            std::lock_guard lock(mutex_);
            auto it = sessions_.find(id);
            if (it != sessions_.end())
                target = it->second;
        }
        if (target)
            target->Send(std::move(frame));
        else
            spdlog::warn("[SessionManager] 존재하지 않는 세션 id={} 에 전송 시도", id);
    }

    void YisoSessionManager::DisconnectAll()
    {
        std::vector<std::shared_ptr<YisoSession>> snapshot;
        {
            std::lock_guard lock(mutex_);
            for (auto& [id, session] : sessions_)
                snapshot.push_back(session);
        }

        spdlog::info("[SessionManager] 전체 세션 종료 ({}개)", snapshot.size());
        for (auto& session : snapshot)
            session->Disconnect();
    }
}
