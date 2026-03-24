using System.Collections;
using Core.Log;
using World.Scripting.Core;

namespace World.Scripting.API {
    /// <summary>
    /// 커맨드: EVENT("id")
    /// YisoEventManager에 string 기반 커스텀 이벤트를 발행한다.
    /// </summary>
    public class EventScriptAPI : IYisoScriptAPI {
        public void Register(YisoScriptRunner runner) {
            runner.RegisterCommand("EVENT", OnEvent);
        }

        // EVENT("SpawnGoblins")
        private IEnumerator OnEvent(string[] args) {
            if (args == null || args.Length == 0) {
                YisoLogger.Warn("[EventScriptAPI] EVENT 이벤트 ID 없음");
                yield break;
            }

            var eventId = args[0];
            // YisoScriptEvent로 발행 — 구독자는 OnEvent(YisoScriptEvent)로 수신
            Core.Event.YisoEventManager.TriggerEvent(new YisoScriptEvent { EventId = eventId });
            YisoLogger.Debug($"[EventScriptAPI] 이벤트 발행: {eventId}");
            yield break;
        }
    }

    /// <summary>
    /// .yiso EVENT("id") 커맨드가 발행하는 이벤트 구조체.
    /// 게임 시스템은 IYisoEventListener&lt;YisoScriptEvent&gt;를 구현해 수신.
    /// </summary>
    public struct YisoScriptEvent {
        public string EventId;
    }
}
