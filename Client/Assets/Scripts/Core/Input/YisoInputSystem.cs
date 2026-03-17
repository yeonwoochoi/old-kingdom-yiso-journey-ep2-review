using Core.Singleton;

namespace Core.Input {
    /// <summary>
    /// [역할] 유저 입력 → 게임 명령 변환
    /// [책임]
    ///   - 2D 탑다운 이동 입력 처리
    ///   - 스킬 단축키, UI 클릭/터치, 귀환 주문서 숏컷
    ///   - 컷씬 재생 중 입력 차단 (Enable / Disable)
    /// [타입] MonoSingleton (Update에서 입력 감지)
    /// </summary>
    public class YisoInputSystem : YisoMonoSingleton<YisoInputSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
