using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 일시적 상태 이상 및 버프 관리
    /// [책임]
    ///   - 출혈, 기절, 슬로우 디버프 적용 / 해제
    ///   - 무한 도장 일시 능력치 버프
    ///   - 지속 시간 추적 및 만료 처리
    /// [타입] MonoSingleton (Update에서 지속시간 체크)
    /// </summary>
    public class YisoBuffSystem : YisoMonoSingleton<YisoBuffSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
