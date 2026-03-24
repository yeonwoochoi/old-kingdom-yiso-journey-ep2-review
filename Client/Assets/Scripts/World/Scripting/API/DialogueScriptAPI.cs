using System.Collections;
using Core.Log;
using Core.Localization;
using World.Scripting.Core;

namespace World.Scripting.API {
    /// <summary>
    /// 커맨드: DIALOGUE (Runner가 DialogueLineNode를 커맨드로 변환해 호출)
    /// Args[0] = speaker ID, Args[1] = 텍스트 또는 "@loc:key"
    ///
    /// TODO: Phase 6 — UIManager(대화창) 구현 후 실제 연결
    /// 현재는 로그로만 출력하고 즉시 반환 (플레이어 입력 대기 없음)
    /// </summary>
    public class DialogueScriptAPI : IYisoScriptAPI {
        public void Register(YisoScriptRunner runner) {
            runner.RegisterCommand("DIALOGUE",   OnDialogue);
            runner.RegisterCommand("GIVE_QUEST", OnGiveQuest);
        }

        private IEnumerator OnDialogue(string[] args) {
            if (args == null || args.Length < 2) yield break;

            var speaker = args[0];
            var rawText = args[1];

            // @loc:key → LocalizationManager 조회
            var text = rawText.StartsWith("@loc:")
                ? YisoLocalizationManager.Instance.Get(rawText[5..])
                : rawText;

            // TODO: UIManager.ShowDialogue(speaker, text) 호출 후 입력 대기
            YisoLogger.Debug($"[DialogueScriptAPI] [{speaker}]: {text}");
            yield break;
        }

        // GIVE_QUEST("id")
        private IEnumerator OnGiveQuest(string[] args) {
            if (args == null || args.Length == 0) yield break;
            // TODO: Phase 6 — QuestSystem.GiveQuest(args[0]) 연결
            YisoLogger.Debug($"[DialogueScriptAPI] GIVE_QUEST: {args[0]}");
            yield break;
        }
    }
}
