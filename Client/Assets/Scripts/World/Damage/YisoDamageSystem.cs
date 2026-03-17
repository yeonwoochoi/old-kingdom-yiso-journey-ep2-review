using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 최종 데미지 산출 및 적용
    /// [책임]
    ///   - 공격력 / 방어력 / 크리티컬 확률 → 데미지 계산
    ///   - HP 차감
    ///   - 데미지 텍스트 팝업 요청 (UIManager)
    ///   - 사망 처리
    /// [타입] Singleton (이벤트 기반, Unity lifecycle 불필요)
    /// </summary>
    public class YisoDamageSystem : YisoSingleton<YisoDamageSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
