using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 전투 시각 효과
    /// [책임]
    ///   - 타격 / 스킬 이펙트 재생 (PoolingSystem 연동)
    ///   - 보스 스킬 전조(장판) 마커 렌더링
    ///   - Transform / offset 파라미터로 위치 지정
    /// [타입] Singleton (Pooler + 파라미터 처리, Unity lifecycle 불필요)
    /// </summary>
    public class YisoEffectSystem : YisoSingleton<YisoEffectSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
