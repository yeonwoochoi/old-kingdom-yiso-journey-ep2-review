using Core.Singleton;

namespace Core.Time {
    /// <summary>
    /// [역할] 게임 내 시간 제어 및 타이머
    /// [책임]
    ///   - TimeScale 제어 (Pause / Resume / SetScale)
    ///
    ///   - [Phase 3] 플레이타임 누적
    ///       캐릭터별 플레이타임은 TimeManager가 누적, 저장은 SaveManager 담당 (PlayerPrefs 직접 접근 금지)
    ///       YisoTickEvent 주기적 발행 → SaveManager가 구독해서 자동 중간저장
    ///
    ///   - [Phase 5] 게임 시간 흐름 관리
    ///       실제 N초 = 게임 1시간으로 환산 (REAL_SECONDS_PER_GAME_HOUR 참조)
    ///       TimeChangedEvent 발행 → EnvironmentManager 구독 (낮/밤 사이클 등)
    /// [타입] MonoBehaviour (Update, 코루틴 필요)
    /// </summary>
    public class YisoTimeManager : YisoMonoSingleton<YisoTimeManager> {
        private float _lastTimeScale = 1f;
        private const float REAL_SECONDS_PER_GAME_HOUR = 60f; // 실제 시간 1분 = 게임 시간 1시간

        public void Pause() {
            _lastTimeScale = UnityEngine.Time.timeScale;
            UnityEngine.Time.timeScale = 0;
        }

        public void Resume() {
            UnityEngine.Time.timeScale = _lastTimeScale;
        }

        public void SetScale(float scale) {
            _lastTimeScale = UnityEngine.Time.timeScale;
            UnityEngine.Time.timeScale = scale;
        }
    }
}
