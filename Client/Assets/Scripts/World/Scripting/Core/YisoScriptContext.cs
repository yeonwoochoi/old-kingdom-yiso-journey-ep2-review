using System;
using System.Collections.Generic;
using World.Scripting.AST;

namespace World.Scripting.Core {
    /// <summary>
    /// [역할] 스크립트 한 번 실행 동안의 런타임 상태 보관
    /// [책임]
    ///   - SET/GET 로컬 플래그 관리 (스크립트 실행 범위 내 임시)
    ///   - 분기 조건 평가 (HasQuest / HasFlag / HasItem → 외부 시스템에 위임)
    /// [타입] 순수 C# 클래스 — 생성 비용 없음, 실행마다 new
    /// </summary>
    public class YisoScriptContext {
        private readonly Dictionary<string, string> _flags = new();

        // 외부 게임 시스템 쿼리 — ScriptingManager가 주입
        // Phase 3 (SaveSystem), Phase 6 (QuestSystem, InventorySystem) 구현 시 실제 연결
        public Func<string, bool> HasQuestQuery { get; set; }
        public Func<string, bool> HasFlagQuery  { get; set; }
        public Func<string, bool> HasItemQuery  { get; set; }

        // ─── 로컬 플래그 ─────────────────────────────────────────────

        public void SetFlag(string key, string value) {
            _flags[key] = value;
        }

        public string GetFlag(string key) {
            return _flags.TryGetValue(key, out var v) ? v : null;
        }

        public bool GetFlagBool(string key) {
            var v = GetFlag(key);
            return v != null && (v == "true" || v == "1");
        }

        // ─── 분기 조건 평가 ───────────────────────────────────────────

        public bool Evaluate(YisoBranchCondition condition) {
            if (condition.IsDefault) return true;

            return condition.FuncName switch {
                "HasQuest" => HasQuestQuery?.Invoke(condition.Argument) ?? false,
                "HasFlag"  => HasFlagQuery?.Invoke(condition.Argument)  ?? false,
                "HasItem"  => HasItemQuery?.Invoke(condition.Argument)  ?? false,
                // 로컬 플래그 직접 참조: GET flag.key
                _ when condition.FuncName.StartsWith("flag.") =>
                    GetFlagBool(condition.FuncName),
                _ => false,
            };
        }
    }
}
