using Core;
using Core.Singleton;

namespace Infra {
    /// <summary>
    /// [역할] 에셋 로드 및 메모리 관리
    /// [책임]
    ///   - Addressables: 챕터 진입 시 맵 데이터, 보스 리소스 동적 로드
    ///   - Built-in: 빌드에 포함된 에셋 동기/비동기 로드
    ///   - 챕터 이탈 시 해당 에셋 메모리 해제
    /// [타입] Singleton (Addressables async는 Task 기반, Unity lifecycle 불필요)
    /// [설계] DontDestroyOnLoad. Layer 4 시스템이 전역 접근. 하위 레이어를 알 필요 없음.
    /// </summary>
    public class YisoResourceSystem : YisoSingleton<YisoResourceSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
