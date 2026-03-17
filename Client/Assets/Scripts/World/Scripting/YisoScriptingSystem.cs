using Core;
using Core.Singleton;

namespace World.Scripting {
    /// <summary>
    /// [역할] .yiso 스크립트 파일을 런타임에 파싱·실행하는 콘텐츠 스크립팅 엔진
    /// [책임]
    ///   - .yiso 파일 Lexing → Parsing → AST 생성
    ///   - 코루틴 기반 블록 실행 (순서 보장, WAIT 지원)
    ///   - 런타임 변수·플래그 상태 유지 (ScriptContext)
    ///   - 각 게임 시스템 호출 브릿지 (IScriptAPI) 제공
    /// [타입] MonoSingleton (파싱 및 실행 코루틴 필요)
    /// [빌드] Dev: StreamingAssets/ 직접 읽기 (핫리로드) / Release: Addressables 번들
    /// </summary>
    public class YisoScriptingSystem : YisoMonoSingleton<YisoScriptingSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
