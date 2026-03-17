using Core;
using Core.Singleton;

namespace Infra {
    /// <summary>
    /// [역할] 유저 계정 인증
    /// [책임]
    ///   - 게스트 / 구글 / 애플 / 스팀 로그인 처리
    ///   - UID 발급 및 세션 토큰 관리
    ///   - 로그인 성공 시 SaveSystem에 신호
    /// [타입] MonoSingleton (HTTP 인증 코루틴 필요)
    /// </summary>
    public class YisoAuthSystem : YisoMonoSingleton<YisoAuthSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
