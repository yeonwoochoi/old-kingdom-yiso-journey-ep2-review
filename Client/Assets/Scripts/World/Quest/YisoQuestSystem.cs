using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 메인 / 서브 / 특수(무한 도장) 퀘스트 진행도 관리
    /// [책임]
    ///   - 퀘스트 수주 / 달성 / 보상 처리
    ///   - 중간 맵 후퇴 시 해당 챕터 퀘스트 '수주 전'으로 롤백
    ///   - 무한 도장 세션 퀘스트 발동 / 종료
    ///   - ScriptingSystem @quest 블록 데이터 제공
    /// [타입] Singleton (이벤트 기반 처리, Unity lifecycle 불필요)
    /// </summary>
    public class YisoQuestSystem : YisoSingleton<YisoQuestSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
