using System.Collections;
using Core.Log;
using World.Scripting.Core;

namespace World.Scripting.API {
    /// <summary>
    /// @trigger 블록에서 사용할 TriggerSystem 전용 커맨드 예약 슬롯.
    ///
    /// TODO: Phase 5 — TriggerSystem 구현 후 커맨드 추가
    /// 현재는 커맨드 미등록 (EVENT / CAMERA / SOUND는 각 전용 API가 담당)
    /// </summary>
    public class TriggerScriptAPI : IYisoScriptAPI {
        public void Register(YisoScriptRunner runner) {
            runner.RegisterCommand("LOCK_DOOR", OnLockDoor);
            runner.RegisterCommand("UNLOCK_DOOR", OnUnlockDoor);
        }

        // LOCK_DOOR()
        private IEnumerator OnLockDoor(string[] args) {
            // TODO: TriggerSystem / MapSystem 연결
            YisoLogger.Debug("[TriggerScriptAPI] LOCK_DOOR");
            yield break;
        }

        // UNLOCK_DOOR()
        private IEnumerator OnUnlockDoor(string[] args) {
            YisoLogger.Debug("[TriggerScriptAPI] UNLOCK_DOOR");
            yield break;
        }
    }
}