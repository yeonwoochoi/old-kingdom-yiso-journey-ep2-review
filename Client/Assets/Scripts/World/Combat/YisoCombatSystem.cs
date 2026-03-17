using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 전투 흐름 및 타겟팅 제어
    /// [책임]
    ///   - 공격자 / 피격자 유효 판정
    ///   - 적 어그로 목록 관리
    /// [타입] Singleton (이벤트 기반, Unity lifecycle 불필요)
    /// </summary>
    public class YisoCombatSystem : YisoSingleton<YisoCombatSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
