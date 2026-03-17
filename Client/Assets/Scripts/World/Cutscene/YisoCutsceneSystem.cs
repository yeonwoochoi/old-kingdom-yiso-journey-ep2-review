using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 연출 및 시네마틱 재생
    /// [책임]
    ///   - 보스 조우 인트로, 챕터 엔딩 연출
    ///   - 컷씬 재생 중 InputSystem 차단
    ///   - CameraSystem Public API 직접 호출 (MoveToPosition, ZoomTo, ReleaseControl)
    ///   - ScriptingSystem @cutscene 블록 실행 위임
    /// [타입] MonoSingleton (연출 시퀀스 코루틴 필요)
    /// </summary>
    public class YisoCutsceneSystem : YisoMonoSingleton<YisoCutsceneSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
