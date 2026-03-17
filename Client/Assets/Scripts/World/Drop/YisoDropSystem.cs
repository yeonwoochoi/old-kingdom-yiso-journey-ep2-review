using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 전리품 생성 규칙
    /// [책임]
    ///   - 확률 테이블 기반 드랍 처리
    ///   - 무한 도장 진행 시 드랍률 / 골드량 배율(Multiplier) 적용
    ///   - PoolingSystem으로 드랍 아이템 오브젝트 생성
    ///   - 드랍 아이템 자체 컴포넌트가 소멸 이벤트 발행 → 수신 처리
    /// [타입] Singleton (Pooler 사용, Unity lifecycle 불필요)
    /// </summary>
    public class YisoDropSystem : YisoSingleton<YisoDropSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
