using System.Collections.Generic;

namespace World.Scripting.AST {
    // ─── 기본 노드 ────────────────────────────────────────────────────
    public abstract class YisoScriptNode { }

    // ─── 블록 ─────────────────────────────────────────────────────────
    public enum YisoScriptBlockType { Dialogue, Quest, Trigger, Wave, Cutscene }

    public class YisoScriptBlockNode : YisoScriptNode {
        public YisoScriptBlockType  BlockType { get; set; }
        public string               Id        { get; set; }
        public List<YisoScriptNode> Body      { get; set; } = new();
    }

    // ─── 대화 ─────────────────────────────────────────────────────────
    public class YisoDialogueLineNode : YisoScriptNode {
        public string Speaker   { get; set; }
        public string RawText   { get; set; }  // 직접 텍스트
        public string LocaleKey { get; set; }  // @loc:key 형태
        public bool   IsLocaleKey => !string.IsNullOrEmpty(LocaleKey);
    }

    // ─── 분기 ─────────────────────────────────────────────────────────
    /// <summary>
    /// 조건 표현식. FuncName이 "default"이면 기본 분기.
    /// </summary>
    public class YisoBranchCondition {
        public string FuncName { get; set; }   // "HasQuest", "HasFlag", "HasItem", "default"
        public string Argument { get; set; }   // 함수 인자 (없으면 빈 문자열)
        public bool   IsDefault => FuncName == "default";
    }

    public class YisoBranchCase {
        public YisoBranchCondition  Condition { get; set; }
        public List<YisoScriptNode> Body      { get; set; } = new();
    }

    /// <summary>
    /// 연속된 ? 블록을 하나로 묶은 분기 노드.
    /// Runner가 순서대로 조건을 평가해 처음 true인 케이스를 실행한다.
    /// </summary>
    public class YisoBranchNode : YisoScriptNode {
        public List<YisoBranchCase> Cases { get; set; } = new();
    }

    // ─── 커맨드 ───────────────────────────────────────────────────────
    /// <summary>
    /// CAMERA.MoveTo, EVENT, GIVE_QUEST, WAIT 등 모든 커맨드.
    /// Args는 문자열 배열로 통일 — Runner/API에서 타입 변환.
    /// </summary>
    public class YisoCommandNode : YisoScriptNode {
        public string   Name { get; set; }
        public string[] Args { get; set; }
    }

    // ─── 플래그 ───────────────────────────────────────────────────────
    public class YisoSetFlagNode : YisoScriptNode {
        public string Key   { get; set; }  // "flag.elder_met"
        public string Value { get; set; }  // "true" / "false" / 문자열
    }

    // ─── 퀘스트 ───────────────────────────────────────────────────────
    public class YisoQuestTitleNode : YisoScriptNode {
        public string RawText   { get; set; }
        public string LocaleKey { get; set; }
        public bool   IsLocaleKey => !string.IsNullOrEmpty(LocaleKey);
    }

    public class YisoQuestDescNode : YisoScriptNode {
        public string RawText   { get; set; }
        public string LocaleKey { get; set; }
        public bool   IsLocaleKey => !string.IsNullOrEmpty(LocaleKey);
    }

    public enum YisoObjectiveType { Kill, Talk, Reach }

    public class YisoObjectiveNode : YisoScriptNode {
        public YisoObjectiveType ObjType        { get; set; }
        public string            Target         { get; set; }
        public int               Count          { get; set; } = 1;
        public string            Label          { get; set; }
        public string            LabelLocaleKey { get; set; }
        public bool              IsLocaleLabel  => !string.IsNullOrEmpty(LabelLocaleKey);
    }

    public enum YisoHookType { OnStart, OnComplete }

    public class YisoHookNode : YisoScriptNode {
        public YisoHookType        HookType { get; set; }
        public List<YisoScriptNode> Body    { get; set; } = new();
    }

    public class YisoRewardNode : YisoScriptNode {
        public int Exp  { get; set; }
        public int Gold { get; set; }
    }

    // ─── 웨이브 ───────────────────────────────────────────────────────
    public class YisoWaveNode : YisoScriptNode {
        public int                 WaveIndex { get; set; }
        public List<YisoSpawnNode> Spawns    { get; set; } = new();
    }

    public class YisoSpawnNode : YisoScriptNode {
        public string EntityId { get; set; }
        public int    Count    { get; set; } = 1;
        public float  Interval { get; set; }
    }
}
