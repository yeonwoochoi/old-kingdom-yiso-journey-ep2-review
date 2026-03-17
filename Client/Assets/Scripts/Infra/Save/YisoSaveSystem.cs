using Core;
using Core.Singleton;

namespace Infra {
    /// <summary>
    /// [역할] 유저 플레이 데이터 저장 및 복구 — 이원화 저장 로직 핵심
    /// [책임]
    ///   - 일반 저장: 위치 + 퀘스트 진행도 포함 전체 저장
    ///   - 후퇴 저장: 레벨 / 장비 / 골드만 저장, 챕터 퀘스트·위치 파기
    ///   - AuthSystem 로그인 성공 후 유저 데이터 로드
    /// [타입] MonoSingleton (파일 I/O 또는 네트워크 저장 코루틴 필요)
    /// </summary>
    public class YisoSaveSystem : YisoMonoSingleton<YisoSaveSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
