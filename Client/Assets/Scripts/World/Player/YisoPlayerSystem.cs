using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 플레이어 / 적 / NPC Entity 생성 및 제어
    /// [책임]
    ///   - Entity 인스턴스 생성, 초기화, 생명주기 관리
    ///   - 하위 모듈(s)의 Update를 대신 구동
    ///   - Player 생성 후 CameraSystem.SetTarget() 호출
    /// [타입] MonoSingleton
    /// </summary>
    public class YisoPlayerSystem : YisoMonoSingleton<YisoPlayerSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
