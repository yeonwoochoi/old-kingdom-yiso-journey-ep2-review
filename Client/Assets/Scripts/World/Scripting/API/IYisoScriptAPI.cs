using World.Scripting.Core;

namespace World.Scripting.API {
    /// <summary>
    /// 각 ScriptAPI가 구현하는 인터페이스.
    /// Register() 호출 시 자신이 담당하는 커맨드 핸들러를 Runner에 등록한다.
    /// </summary>
    public interface IYisoScriptAPI {
        void Register(YisoScriptRunner runner);
    }
}
