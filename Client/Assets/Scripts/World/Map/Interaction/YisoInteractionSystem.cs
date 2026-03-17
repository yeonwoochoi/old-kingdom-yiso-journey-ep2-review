using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 플레이어와 월드 오브젝트 간 상호작용 처리
    /// [책임]
    ///   - NPC 근접 감지 → 대화 트리거 → ScriptingSystem에 @dialogue 블록 실행 요청
    ///   - 포탈 입장 처리
    ///   - 드랍 아이템 루팅
    /// [타입] MonoSingleton (근접 감지, 트리거 이벤트 함수 사용)
    /// </summary>
    public class YisoInteractionSystem : YisoMonoSingleton<YisoInteractionSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
