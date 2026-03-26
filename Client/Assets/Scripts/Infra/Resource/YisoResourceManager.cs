using Core;
using Core.Singleton;

namespace Infra.Resource {
    /// <summary>
    /// [역할] 에셋 로드 및 메모리 관리
    /// [책임]
    ///   - Addressables: 챕터 진입 시 맵 데이터, 보스 리소스 동적 로드
    ///   - Built-in: 빌드에 포함된 에셋 동기/비동기 로드
    ///   - 챕터 이탈 시 해당 에셋 메모리 해제
    /// [타입] MonoSingleton (GameApp의 직속 자식 프리팹. 다른 매니저 로드에 사용)
    /// [설계] DontDestroyOnLoad. 하위 레이어를 알 필요 없음.
    /// </summary>
    public class YisoResourceManager : YisoMonoSingleton<YisoResourceManager> {
    }
}
