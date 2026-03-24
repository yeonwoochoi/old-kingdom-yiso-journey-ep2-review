using System.Collections;
using Core.Input;
using Core.Log;
using World.Scripting.Core;

namespace World.Scripting.API {
    /// <summary>
    /// 커맨드: INPUT.Enable / INPUT.Disable
    /// YisoInputManager에 Enable/Disable 메서드가 추가되면 실제 연결.
    /// </summary>
    public class InputScriptAPI : IYisoScriptAPI {
        private readonly YisoInputManager _input;

        public InputScriptAPI(YisoInputManager input) {
            _input = input;
        }

        public void Register(YisoScriptRunner runner) {
            runner.RegisterCommand("INPUT.Enable",  OnEnable);
            runner.RegisterCommand("INPUT.Disable", OnDisable);
        }

        // INPUT.Enable()
        private IEnumerator OnEnable(string[] args) {
            // TODO: YisoInputManager.Enable() 구현 후 연결
            YisoLogger.Debug("[InputScriptAPI] INPUT.Enable");
            yield break;
        }

        // INPUT.Disable()
        private IEnumerator OnDisable(string[] args) {
            // TODO: YisoInputManager.Disable() 구현 후 연결
            YisoLogger.Debug("[InputScriptAPI] INPUT.Disable");
            yield break;
        }
    }
}
