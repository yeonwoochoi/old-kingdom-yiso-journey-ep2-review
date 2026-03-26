using System.Collections.Generic;
using World.Scripting.AST;

namespace World.Scripting.Core {
    /// <summary>
    /// [역할] 토큰 스트림 → AST 변환
    /// [패턴] Recursive Descent Parser
    /// </summary>
    public class YisoScriptParser {
        private List<YisoScriptToken> _tokens;
        private int _pos;

        public List<YisoScriptBlockNode> Parse(List<YisoScriptToken> tokens) {
            _tokens = tokens;
            _pos = 0;

            var blocks = new List<YisoScriptBlockNode>();
            while (Peek().Type != YisoScriptTokenType.Eof) {
                SkipNewlines();
                if (Peek().Type == YisoScriptTokenType.Eof) break;
                blocks.Add(ParseBlock());
            }

            return blocks;
        }

        #region Block Parser

        private YisoScriptBlockNode ParseBlock() {
            var t = Peek();
            return t.Type switch {
                YisoScriptTokenType.AtDialogue => ParseDialogueBlock(),
                YisoScriptTokenType.AtQuest => ParseQuestBlock(),
                YisoScriptTokenType.AtTrigger => ParseTriggerBlock(),
                YisoScriptTokenType.AtWave => ParseWaveBlock(),
                YisoScriptTokenType.AtCutscene => ParseCutsceneBlock(),
                _ => throw new YisoScriptException($"블록 선언(@dialogue 등) 필요, '{t.Value}' 발견", t.Line)
            };
        }

        private YisoScriptBlockNode ParseDialogueBlock() {
            Consume(); // @dialogue
            var id = Expect(YisoScriptTokenType.Identifier).Value;
            Expect(YisoScriptTokenType.Newline);
            Expect(YisoScriptTokenType.Indent);
            var body = ParseBody(BodyContext.Dialogue);
            return new YisoScriptBlockNode {BlockType = YisoScriptBlockType.Dialogue, Id = id, Body = body};
        }

        private YisoScriptBlockNode ParseQuestBlock() {
            Consume(); // @quest
            var id = Expect(YisoScriptTokenType.Identifier).Value;
            Expect(YisoScriptTokenType.Newline);
            Expect(YisoScriptTokenType.Indent);
            var body = ParseBody(BodyContext.Quest);
            return new YisoScriptBlockNode {BlockType = YisoScriptBlockType.Quest, Id = id, Body = body};
        }

        private YisoScriptBlockNode ParseTriggerBlock() {
            Consume(); // @trigger
            var id = Expect(YisoScriptTokenType.Identifier).Value;
            Expect(YisoScriptTokenType.Newline);
            Expect(YisoScriptTokenType.Indent);
            var body = ParseBody(BodyContext.Command);
            return new YisoScriptBlockNode {BlockType = YisoScriptBlockType.Trigger, Id = id, Body = body};
        }

        private YisoScriptBlockNode ParseWaveBlock() {
            Consume(); // @wave
            var id = Expect(YisoScriptTokenType.Identifier).Value;
            Expect(YisoScriptTokenType.Newline);
            Expect(YisoScriptTokenType.Indent);
            var body = ParseWaveBody();
            return new YisoScriptBlockNode {BlockType = YisoScriptBlockType.Wave, Id = id, Body = body};
        }

        private YisoScriptBlockNode ParseCutsceneBlock() {
            Consume(); // @cutscene
            var id = Expect(YisoScriptTokenType.Identifier).Value;
            Expect(YisoScriptTokenType.Newline);
            Expect(YisoScriptTokenType.Indent);
            var body = ParseBody(BodyContext.Cutscene);
            return new YisoScriptBlockNode {BlockType = YisoScriptBlockType.Cutscene, Id = id, Body = body};
        }

        #endregion

        #region Body Parser

        private enum BodyContext {
            Dialogue,
            Quest,
            Command,  // @trigger 바디
            Hook,     // ON_START / ON_COMPLETE 바디 — Command + REWARD 허용
            Cutscene
        }

        /// <summary>
        /// INDENT 소비 후 진입. DEDENT 소비 후 반환.
        /// </summary>
        private List<YisoScriptNode> ParseBody(BodyContext ctx) {
            var nodes = new List<YisoScriptNode>();

            while (Peek().Type != YisoScriptTokenType.Dedent && Peek().Type != YisoScriptTokenType.Eof) {
                SkipNewlines();
                if (Peek().Type == YisoScriptTokenType.Dedent || Peek().Type == YisoScriptTokenType.Eof) break;

                var t = Peek();

                // END - 현재 body 조기 종료 (분기 케이스 종료 신호)
                if (t.Type == YisoScriptTokenType.End) {
                    Consume();
                    SkipNewlines();
                    break;
                }

                // 연속 ? 블록 -> 하나의 BranchNode로 묶음 (Quest는 분기 없음)
                if (t.Type == YisoScriptTokenType.Question) {
                    if (ctx == BodyContext.Quest)
                        throw new YisoScriptException("@quest 블록에는 분기(?)를 사용할 수 없습니다.", t.Line);
                    nodes.Add(ParseBranch(ctx));
                    continue;
                }

                YisoScriptNode node;
                switch (ctx) {
                    case BodyContext.Dialogue:
                        if (t.Type == YisoScriptTokenType.Speaker) node = ParseDialogueLine();
                        else if (t.Type == YisoScriptTokenType.Set) node = ParseSetFlag();
                        else if (t.Type == YisoScriptTokenType.GiveQuest || t.Type == YisoScriptTokenType.UnlockQuest) node = ParseCommandNode();
                        else throw new YisoScriptException($"{ctx}에는 {t.Type}이 올 수 없습니다.", t.Line);
                        break;
                    case BodyContext.Quest:
                        if (t.Type == YisoScriptTokenType.Title) node = ParseQuestTitle();
                        else if (t.Type == YisoScriptTokenType.Desc) node = ParseQuestDesc();
                        else if (t.Type == YisoScriptTokenType.Objective) node = ParseObjective();
                        else if (t.Type == YisoScriptTokenType.OnStart) node = ParseHook(YisoHookType.OnStart);
                        else if (t.Type == YisoScriptTokenType.OnComplete) node = ParseHook(YisoHookType.OnComplete);
                        else if (t.Type == YisoScriptTokenType.Reward) node = ParseReward();
                        else throw new YisoScriptException($"{ctx}에는 {t.Type}이 올 수 없습니다.", t.Line);
                        break;
                    case BodyContext.Command:
                        // @trigger 바디: CAMERA, SOUND, INPUT, WAIT, EVENT, GIVE_QUEST, UNLOCK_QUEST, CUTSCENE, SET
                        if (t.Type == YisoScriptTokenType.Set) node = ParseSetFlag();
                        else if (IsCommandToken(t.Type) || t.Type == YisoScriptTokenType.Cutscene) node = ParseCommandNode();
                        else throw new YisoScriptException($"{ctx}에는 {t.Type}이 올 수 없습니다.", t.Line);
                        break;
                    case BodyContext.Hook:
                        // ON_START / ON_COMPLETE 바디: Command 범위 + REWARD (CUTSCENE 제외)
                        if (t.Type == YisoScriptTokenType.Set) node = ParseSetFlag();
                        else if (t.Type == YisoScriptTokenType.Reward) node = ParseReward();
                        else if (IsCommandToken(t.Type)) node = ParseCommandNode();
                        else throw new YisoScriptException($"{ctx}에는 {t.Type}이 올 수 없습니다.", t.Line);
                        break;
                    case BodyContext.Cutscene:
                        // cutscene 바디: Command 허용 범위 + 대화라인. CUTSCENE 재귀 호출 불허.
                        if (t.Type == YisoScriptTokenType.Speaker) node = ParseDialogueLine();
                        else if (t.Type == YisoScriptTokenType.Set) node = ParseSetFlag();
                        else if (IsCommandToken(t.Type)) node = ParseCommandNode();
                        else throw new YisoScriptException($"{ctx}에는 {t.Type}이 올 수 없습니다.", t.Line);
                        break;
                    default:
                        throw new YisoScriptException($"{t.Type} 잘못된 타입입니다.", t.Line);
                }

                nodes.Add(node);
            }

            // DEDENT 소비
            if (Peek().Type == YisoScriptTokenType.Dedent) Consume();

            return nodes;
        }

        private YisoDialogueLineNode ParseDialogueLine() {
            var speaker = Expect(YisoScriptTokenType.Speaker).Value;
            var node = new YisoDialogueLineNode {Speaker = speaker};

            if (Peek().Type == YisoScriptTokenType.LocaleKey)
                node.LocaleKey = Consume().Value;
            else
                node.RawText = Expect(YisoScriptTokenType.DialogueText).Value; // Lexer가 DialogueText로 emit

            SkipNewlines();
            return node;
        }

        /// <summary>
        /// 연속된 ? 케이스들을 하나의 BranchNode로 묶는다
        /// </summary>
        private YisoBranchNode ParseBranch(BodyContext parentCtx) {
            var branch = new YisoBranchNode();

            while (Peek().Type == YisoScriptTokenType.Question) {
                Consume(); // ?
                var condition = ParseCondition();
                Expect(YisoScriptTokenType.Newline);
                Expect(YisoScriptTokenType.Indent);
                var body = ParseBody(parentCtx);

                branch.Cases.Add(new YisoBranchCase {Condition = condition, Body = body});
                SkipNewlines();
            }

            return branch;
        }

        /// <summary>
        /// 조건 표현식 파싱. HasQuest("id"), HasFlag("key"), HasItem("id"), flag.key 또는 default.
        /// </summary>
        private YisoBranchCondition ParseCondition() {
            if (Peek().Type == YisoScriptTokenType.Default) {
                Consume();
                return new YisoBranchCondition {FuncName = "default"};
            }

            var funcName = Expect(YisoScriptTokenType.Identifier).Value;

            // flag.key 형태 — dot notation 이어붙임
            while (Peek().Type == YisoScriptTokenType.Dot) {
                Consume(); // .
                funcName += "." + Expect(YisoScriptTokenType.Identifier).Value;
            }

            // HasQuest("id") 형태 — 인자 파싱
            var arg = "";
            if (Peek().Type == YisoScriptTokenType.LParen) {
                Consume(); // (
                if (Peek().Type != YisoScriptTokenType.RParen)
                    arg = Consume().Value;
                Expect(YisoScriptTokenType.RParen);
            }

            return new YisoBranchCondition {FuncName = funcName, Argument = arg};
        }

        // SET flag.key = value
        private YisoSetFlagNode ParseSetFlag() {
            Consume(); // SET
            var keyParts = new List<string> {Expect(YisoScriptTokenType.Identifier).Value};
            while (Peek().Type == YisoScriptTokenType.Dot) {
                Consume(); // .
                keyParts.Add(Expect(YisoScriptTokenType.Identifier).Value);
            }

            Expect(YisoScriptTokenType.Equals);
            var value = Consume().Value; // Bool / String / Number / Identifier
            SkipNewlines();
            return new YisoSetFlagNode {Key = string.Join(".", keyParts), Value = value};
        }

        /// <summary>
        /// CAMERA.X / SOUND.X / INPUT.X 형태 및 일반 커맨드 파싱.
        /// </summary>
        private YisoCommandNode ParseCommandNode() {
            var t = Peek();
            string name;

            // dot notation 커맨드 (CAMERA / SOUND / INPUT)
            if (t.Type == YisoScriptTokenType.Camera || t.Type == YisoScriptTokenType.Sound || t.Type == YisoScriptTokenType.Input) {
                var prefix = Consume().Value; // "CAMERA" / "SOUND" / "INPUT"
                Expect(YisoScriptTokenType.Dot);
                var method = Expect(YisoScriptTokenType.Identifier).Value;
                name = $"{prefix}.{method}";
            }
            else {
                // 일반 키워드 또는 Identifier
                name = Consume().Value;
            }

            // 인자 파싱 ( arg, arg, ... )
            var args = new List<string>();
            if (Peek().Type == YisoScriptTokenType.LParen) {
                Consume(); // (
                while (Peek().Type != YisoScriptTokenType.RParen && Peek().Type != YisoScriptTokenType.Eof) {
                    if (Peek().Type == YisoScriptTokenType.Comma) {
                        Consume();
                        continue;
                    }

                    args.Add(Consume().Value);
                }

                Expect(YisoScriptTokenType.RParen);
            }

            SkipNewlines();
            return new YisoCommandNode {Name = name, Args = args.ToArray()};
        }

        // TITLE "텍스트" 또는 TITLE "@loc:key"
        private YisoQuestTitleNode ParseQuestTitle() {
            Consume(); // TITLE
            var node = new YisoQuestTitleNode();
            var val = Expect(YisoScriptTokenType.String).Value;
            if (val.StartsWith("@loc:")) node.LocaleKey = val[5..];
            else node.RawText = val;
            SkipNewlines();
            return node;
        }

        // DESC "텍스트"
        private YisoQuestDescNode ParseQuestDesc() {
            Consume(); // DESC
            var node = new YisoQuestDescNode();
            var val = Expect(YisoScriptTokenType.String).Value;
            if (val.StartsWith("@loc:")) node.LocaleKey = val[5..];
            else node.RawText = val;
            SkipNewlines();
            return node;
        }

        // OBJECTIVE kill("target") count(n) label("text")
        private YisoObjectiveNode ParseObjective() {
            Consume(); // OBJECTIVE
            var node = new YisoObjectiveNode();

            var typeTok = Consume();
            node.ObjType = typeTok.Type switch {
                YisoScriptTokenType.Kill => YisoObjectiveType.Kill,
                YisoScriptTokenType.Talk => YisoObjectiveType.Talk,
                YisoScriptTokenType.Reach => YisoObjectiveType.Reach,
                _ => throw new YisoScriptException($"알 수 없는 objective 타입: {typeTok.Value}", typeTok.Line)
            };

            // 필수 target 인자
            Expect(YisoScriptTokenType.LParen);
            node.Target = Expect(YisoScriptTokenType.String).Value;
            Expect(YisoScriptTokenType.RParen);

            // 선택 수식어: count(n) label("text")
            while (Peek().Type == YisoScriptTokenType.Count || Peek().Type == YisoScriptTokenType.Label) {
                var mod = Consume();
                Expect(YisoScriptTokenType.LParen);
                var val = Consume().Value;
                Expect(YisoScriptTokenType.RParen);

                if (mod.Type == YisoScriptTokenType.Count) {
                    node.Count = int.Parse(val);
                }
                else {
                    if (val.StartsWith("@loc:")) node.LabelLocaleKey = val[5..];
                    else node.Label = val;
                }
            }

            SkipNewlines();
            return node;
        }

        // ON_START / ON_COMPLETE { body }
        private YisoHookNode ParseHook(YisoHookType hookType) {
            Consume(); // ON_START or ON_COMPLETE
            Expect(YisoScriptTokenType.Newline);
            Expect(YisoScriptTokenType.Indent);
            var body = ParseBody(BodyContext.Hook);
            return new YisoHookNode {HookType = hookType, Body = body};
        }

        // REWARD exp(500) gold(200)
        private YisoRewardNode ParseReward() {
            Consume(); // REWARD
            var node = new YisoRewardNode();

            while (Peek().Type == YisoScriptTokenType.Exp || Peek().Type == YisoScriptTokenType.Gold) {
                var mod = Consume();
                Expect(YisoScriptTokenType.LParen);
                var val = int.Parse(Expect(YisoScriptTokenType.Number).Value);
                Expect(YisoScriptTokenType.RParen);

                if (mod.Type == YisoScriptTokenType.Exp) node.Exp = val;
                else node.Gold = val;
            }

            SkipNewlines();
            return node;
        }

        #endregion

        #region Wave Body Parser

        // ParseWaveBlock에서 INDENT 소비 후 진입. DEDENT 소비 후 반환.
        private List<YisoScriptNode> ParseWaveBody() {
            var nodes = new List<YisoScriptNode>();

            while (Peek().Type != YisoScriptTokenType.Dedent && Peek().Type != YisoScriptTokenType.Eof) {
                SkipNewlines();
                if (Peek().Type == YisoScriptTokenType.Dedent || Peek().Type == YisoScriptTokenType.Eof)
                    break;
                if (Peek().Type != YisoScriptTokenType.Wave)
                    throw new YisoScriptException($"WAVE(n) 필요, '{Peek().Value}' 발견", Peek().Line);
                nodes.Add(ParseWaveEntry());
            }

            if (Peek().Type == YisoScriptTokenType.Dedent) Consume();
            return nodes;
        }

        // WAVE(n)
        private YisoWaveNode ParseWaveEntry() {
            Consume(); // WAVE
            Expect(YisoScriptTokenType.LParen);
            var idx = int.Parse(Expect(YisoScriptTokenType.Number).Value);
            Expect(YisoScriptTokenType.RParen);
            Expect(YisoScriptTokenType.Newline);
            Expect(YisoScriptTokenType.Indent);

            var spawns = new List<YisoSpawnNode>();
            while (Peek().Type != YisoScriptTokenType.Dedent && Peek().Type != YisoScriptTokenType.Eof) {
                SkipNewlines();
                if (Peek().Type == YisoScriptTokenType.Dedent)
                    break;
                if (Peek().Type == YisoScriptTokenType.Spawn)
                    spawns.Add(ParseSpawnEntry());
            }

            if (Peek().Type == YisoScriptTokenType.Dedent) Consume();
            return new YisoWaveNode {WaveIndex = idx, Spawns = spawns};
        }

        // SPAWN "goblin" count(5) interval(1.0)
        private YisoSpawnNode ParseSpawnEntry() {
            Consume(); // SPAWN
            var entityId = Expect(YisoScriptTokenType.String).Value;
            var node = new YisoSpawnNode {EntityId = entityId};

            while (Peek().Type == YisoScriptTokenType.Count || Peek().Type == YisoScriptTokenType.Interval) {
                var mod = Consume();
                Expect(YisoScriptTokenType.LParen);
                var val = Consume().Value;
                Expect(YisoScriptTokenType.RParen);

                if (mod.Type == YisoScriptTokenType.Count)
                    node.Count = int.Parse(val);
                if (mod.Type == YisoScriptTokenType.Interval)
                    node.Interval = float.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
            }

            SkipNewlines();
            return node;
        }

        #endregion

        #region Helper

        /// <summary>
        /// Command / Cutscene 컨텍스트에서 허용되는 커맨드 토큰 여부.
        /// CUTSCENE 재귀 호출은 Command 컨텍스트에서만 허용하므로 여기엔 포함하지 않는다.
        /// </summary>
        private static bool IsCommandToken(YisoScriptTokenType type) => type switch {
            YisoScriptTokenType.Camera     => true,
            YisoScriptTokenType.Sound      => true,
            YisoScriptTokenType.Input      => true,
            YisoScriptTokenType.Wait       => true,
            YisoScriptTokenType.Event      => true,
            YisoScriptTokenType.GiveQuest  => true,
            YisoScriptTokenType.UnlockQuest => true,
            _ => false
        };

        private YisoScriptToken Peek(int offset = 0) {
            var idx = _pos + offset;
            return idx < _tokens.Count
                ? _tokens[idx]
                : new YisoScriptToken(YisoScriptTokenType.Eof, "", 0);
        }

        private YisoScriptToken Consume() => _tokens[_pos++];

        private YisoScriptToken Expect(YisoScriptTokenType type) {
            var t = Peek();
            return t.Type != type
                ? throw new YisoScriptException($"'{type}' 필요, '{t.Type}({t.Value})' 발견", t.Line)
                : Consume();
        }

        private void SkipNewlines() {
            while (Peek().Type == YisoScriptTokenType.Newline)
                Consume();
        }

        #endregion
    }
}