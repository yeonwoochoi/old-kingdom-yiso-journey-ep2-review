using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 전체 맵 구조 및 네비게이션 관리
    /// [책임]
    ///   - 중심 마을과 방사형 필드 연결 노드 관리
    ///   - 미니맵 / 월드맵 UI에 지형·위치 데이터 제공
    ///   - 안전지대(마을) 판별
    ///   - CameraSystem에 맵 Boundary 설정
    /// [타입] MonoSingleton
    /// </summary>
    public class YisoMapSystem : YisoMonoSingleton<YisoMapSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
