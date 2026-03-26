using Core.Singleton;

namespace World.Map {
    /// <summary>
    /// [역할] 전역 맵 상태 레지스트리 (전역 싱글톤) -> 즉 현재 상태 + 다른 시스템과 상호작용
    /// [책임]
    ///   - 현재 맵 ID / MapType 보유 및 제공 (미니맵, UIManager, CameraManager 참조용)
    ///   - 안전지대(마을 / 베이스 캠프) 여부 판별 -> 전투 시스템이 쿼리
    ///   - CameraManager에 맵 Boundary 설정값 제공
    ///   - 미니맵 / 월드맵 UI에 현재 위치 데이터 제공
    /// [타입] MonoSingleton (전역 상태 보유, DontDestroyOnLoad)
    /// [규칙]
    ///   - 맵 오브젝트 로드 / 언로드는 FieldMapLoader 책임. 여기선 하지 않음.
    ///   - 상태 업데이트는 SceneField.OnMapDataReceived()에서만 호출.
    /// </summary>
    public class YisoMapManager : YisoMonoSingleton<YisoMapManager> {
        
    }
}
