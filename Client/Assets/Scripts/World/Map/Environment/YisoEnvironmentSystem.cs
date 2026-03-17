using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 챕터 환경 연출 관리
    /// [책임]
    ///   - 챕터 컨셉 조명 설정 및 전환 (lerp)
    ///   - 날씨 파티클 (비 / 눈) 제어
    ///   - 환경음 (새소리 / 바람소리) 재생
    ///   - TimeSystem의 게임 시간 이벤트 구독 → 낮/밤 보간 처리
    /// [타입] MonoSingleton (Update에서 조명 lerp 처리)
    /// </summary>
    public class YisoEnvironmentSystem : YisoMonoSingleton<YisoEnvironmentSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
