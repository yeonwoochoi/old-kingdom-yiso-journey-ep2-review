using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 캐릭터 / 몬스터 능력치 관리
    /// [책임]
    ///   - 경험치 누적, 레벨업, 레벨업 테이블 기반 스탯 자동 상승
    ///   - 장비 강화 포함 최종 스탯 합산
    /// [타입] Singleton (이벤트 발생 시 계산, Unity lifecycle 불필요)
    /// </summary>
    public class YisoStatSystem : YisoSingleton<YisoStatSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
