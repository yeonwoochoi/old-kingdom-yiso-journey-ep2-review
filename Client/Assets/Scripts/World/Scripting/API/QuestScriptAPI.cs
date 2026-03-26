using System.Collections;
using Core.Log;
using World.Scripting.Core;

namespace World.Scripting.API {
    /// <summary>
    /// 커맨드: UNLOCK_QUEST / REWARD
    /// TODO: Phase 6 — QuestSystem 구현 후 실제 연결
    /// </summary>
    public class QuestScriptAPI : IYisoScriptAPI {
        public void Register(YisoScriptRunner runner) {
            runner.RegisterCommand("UNLOCK_QUEST", OnUnlockQuest);
            runner.RegisterCommand("REWARD", OnReward);
        }

        // UNLOCK_QUEST("chapter1_main")
        private IEnumerator OnUnlockQuest(string[] args) {
            if (args == null || args.Length == 0) yield break;
            // TODO: QuestSystem.UnlockQuest(args[0])
            YisoLogger.Debug($"[QuestScriptAPI] UNLOCK_QUEST: {args[0]}");
            yield break;
        }

        // REWARD exp(n) gold(n) — Runner가 YisoRewardNode를 CommandNode로 변환해 호출
        // Args[0] = exp, Args[1] = gold
        private IEnumerator OnReward(string[] args) {
            if (args == null || args.Length < 2) yield break;
            var exp = args[0];
            var gold = args[1];
            // TODO: PlayerSystem.AddExp / AddGold 연결
            YisoLogger.Debug($"[QuestScriptAPI] REWARD exp={exp} gold={gold}");
            yield break;
        }
    }
}