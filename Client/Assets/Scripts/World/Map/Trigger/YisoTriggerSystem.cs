using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 구역 기반 트리거 처리
    /// [책임]
    ///   - 보스방 진입 감지 (문 닫힘)
    ///   - 함정 발동
    ///   - 특정 지역 도달 시 퀘스트 업데이트
    ///   - Area 기반 CameraSystem Zoom / Boundary 변경 요청
    /// [타입] MonoSingleton (OnTriggerEnter 등 이벤트 함수 사용)
    /// </summary>
    public class YisoTriggerSystem : YisoMonoSingleton<YisoTriggerSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
