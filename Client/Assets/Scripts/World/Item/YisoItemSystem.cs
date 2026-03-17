using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 게임 내 아이템 정적 데이터 관리
    /// [책임]
    ///   - 아이템 ID / 종류 / 능력치 / 아이콘 / 설명 정적 테이블 제공
    /// [타입] Singleton (오직 데이터, Unity lifecycle 불필요)
    /// </summary>
    public class YisoItemSystem : YisoSingleton<YisoItemSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
