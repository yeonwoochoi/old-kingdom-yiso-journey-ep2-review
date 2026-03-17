using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 스킬 획득, 장착, 실행 관리
    /// [책임]
    ///   - 보스 처치 시 고유 스킬 해금
    ///   - 장착 스킬 Ability 실행
    ///   - 쿨타임 / 자원 관리
    /// [타입] MonoSingleton (Update에서 쿨타임 처리)
    /// </summary>
    public class YisoSkillSystem : YisoMonoSingleton<YisoSkillSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
