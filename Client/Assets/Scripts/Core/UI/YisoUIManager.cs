using Core.Singleton;

namespace Core.UI {
    /// <summary>
    /// [역할] 전체 UI 프레임워크
    /// [책임]
    ///   - 팝업 스택 관리 (Z-Order)
    ///   - UIManager / HUDManager 생성 및 관리
    ///   - 경고 팝업, 배경 블러 처리
    /// [타입] MonoSingleton (Canvas 계층, Update에서 UI 상태 관리)
    /// </summary>
    public class YisoUIManager : YisoMonoSingleton<YisoUIManager> {
        public const float FADE_DURATION = 2f;
    }
}
