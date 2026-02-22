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
        std::lock_guard lock(mutex_); // lock 획득
        auto it = sessions_.find(id);
        if (it != sessions_.end())
            it->second->Send(std::move(frame)); // lock을 잡은 채로 외부 함수 호출
        
        // 왜 문제인가?
        // 데드락 위험: Send() 내부에서 다른 lock을 잡거나, Send()가 트리거하는 콜백에서 SessionManager의 다른 메서드(RemoveSession 등...)를 호출하면 같은 mutex_를 다시 잡으려고 함 -> 데드락 발생
        // 성능 저하: 세션이 100개일때 100번의 Send 호출 그동안 lock을 계속 잡고있기 때문에, 그 사이에 AddSession이나 RemoveSession 등 다른 메서드들이 호출되면 모두 대기하게 됨
        // 실제 발생할 수 있을것 같은 예시: 채팅 메시지 Broadcast 도중, 한 클라이언트가 연결을 끊으면 on_disconnect_ 콜백이 RemoveSession을 호출함 -> RemoveSession에서 mutex_를 잡고있음 -> 데드락 발생 가능성
        
        // 수정은 어떻게?
        // 세션 목록을 복사한 후 lock을 풀고 Send 호출
        std::vector<std::shared_ptr<YisoSession>> snapshot;
        {
            std::lock_guard lock(mutex_);
            snapshot.reserve(sessions_.size());
            for (auto& [id, session] : sessions_)
            {
                snapshot.push_back(session);
            }
        }
        
        // lock 해제도니 상태에서 전송
        for (auto& session : snapshot)
            session->Send(frame);
        // shard_ptr을 복사했으므로 전송 도중 세션이 map에서 제거되더라도 객체는 안전하게 유지 된다.
        
    }
}
