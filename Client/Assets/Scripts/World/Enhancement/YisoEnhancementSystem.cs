using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 대장장이 장비 강화
    /// [책임]
    ///   - 골드 기반 강화 비용 산출 (복잡한 재료 없음)
    ///   - 성공률 계산 및 결과 처리
    ///   - 성공 시 StatSystem에 스탯 상승 통보
    /// [타입] MonoSingleton (강화 흐름 코루틴 필요)
    /// </summary>
    public class YisoEnhancementSystem : YisoMonoSingleton<YisoEnhancementSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
