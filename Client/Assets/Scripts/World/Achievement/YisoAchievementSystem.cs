using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 계정 단위 누적 업적 관리
    /// [책임]
    ///   - 몬스터 누적 처치, 보스 클리어 횟수 영구 기록
    ///   - 업적 달성 보상 처리
    /// [타입] Singleton (데이터 관리, Unity lifecycle 불필요)
    /// </summary>
    public class YisoAchievementSystem : YisoSingleton<YisoAchievementSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
