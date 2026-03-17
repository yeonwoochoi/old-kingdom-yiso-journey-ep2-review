using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 몬스터 / NPC / 특수 오브젝트 스폰 관리
    /// [책임]
    ///   - 필드 몬스터 쿨타임 리스폰
    ///   - NPC 스폰
    ///   - 보스 처치 후 포탈 생성
    ///   - 무한 도장 특수 목표 스폰
    /// [타입] MonoSingleton (Update에서 쿨타임 체크)
    /// </summary>
    public class YisoSpawnSystem : YisoMonoSingleton<YisoSpawnSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
