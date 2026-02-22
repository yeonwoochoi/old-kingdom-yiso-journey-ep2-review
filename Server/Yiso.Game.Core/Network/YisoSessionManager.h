#pragma once
#include "YisoSession.h"
#include <map>
#include <memory>
#include <mutex>
#include <vector>

namespace Yiso::Network
{
    class YisoSessionManager
    {
    public:
        using SessionId = YisoSession::SessionId;

        void AddSession(std::shared_ptr<YisoSession> session);
        void RemoveSession(SessionId id);
        void Broadcast(std::vector<uint8_t> frame); // 모든 세션에 전송
        void Send(SessionId id, std::vector<uint8_t> frame); // 특정 세션에만 전송
        void DisconnectAll();

    private:
        std::mutex mutex_;
        std::map<SessionId, std::shared_ptr<YisoSession>> sessions_;
    };
}
