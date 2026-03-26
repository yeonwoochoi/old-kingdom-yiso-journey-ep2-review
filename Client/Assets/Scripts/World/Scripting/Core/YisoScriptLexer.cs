using System;
using System.Collections.Generic;
using System.Text;

namespace World.Scripting.Core {
    /// <summary>
    /// [역할] .yiso 텍스트 → 토큰 스트림 변환
    /// [처리 순서] 줄 단위 → 들여쓰기(INDENT/DEDENT) → 줄 내용 토크나이징
    /// </summary>
    public class YisoScriptLexer {
        private static readonly Dictionary<string, YisoScriptTokenType> Keywords = new() {
            {"WAIT", YisoScriptTokenType.Wait},
            {"END", YisoScriptTokenType.End},
            {"default", YisoScriptTokenType.Default},
            {"SET", YisoScriptTokenType.Set},
            {"GET", YisoScriptTokenType.Get},
            {"EVENT", YisoScriptTokenType.Event},
            {"GIVE_QUEST", YisoScriptTokenType.GiveQuest},
            {"UNLOCK_QUEST", YisoScriptTokenType.UnlockQuest},
            {"TITLE", YisoScriptTokenType.Title},
            {"DESC", YisoScriptTokenType.Desc},
            {"OBJECTIVE", YisoScriptTokenType.Objective},
            {"ON_START", YisoScriptTokenType.OnStart},
            {"ON_COMPLETE", YisoScriptTokenType.OnComplete},
            {"REWARD", YisoScriptTokenType.Reward},
            {"kill", YisoScriptTokenType.Kill},
            {"talk", YisoScriptTokenType.Talk},
            {"reach", YisoScriptTokenType.Reach},
            {"count", YisoScriptTokenType.Count},
            {"label", YisoScriptTokenType.Label},
            {"exp", YisoScriptTokenType.Exp},
            {"gold", YisoScriptTokenType.Gold},
            {"interval", YisoScriptTokenType.Interval},
            {"CAMERA", YisoScriptTokenType.Camera},
            {"SOUND", YisoScriptTokenType.Sound},
            {"INPUT", YisoScriptTokenType.Input},
            {"CUTSCENE", YisoScriptTokenType.Cutscene},
            {"WAVE", YisoScriptTokenType.Wave},
            {"SPAWN", YisoScriptTokenType.Spawn},
            {"true", YisoScriptTokenType.Bool},
            {"false", YisoScriptTokenType.Bool},
        };

        public List<YisoScriptToken> Tokenize(string input) {
            var tokens = new List<YisoScriptToken>();
            var lines = input.Replace("\r\n", "\n").Split("\n");
            var indentStack = new Stack<int>();
            indentStack.Push(0);

            for (var lineIdx = 0; lineIdx < lines.Length; lineIdx++) {
                var rawLine = lines[lineIdx];
                var lineNum = lineIdx + 1;

                // 빈줄, 주석 스킵
                var trimmed = rawLine.TrimStart();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//")) {
                    continue;
                }

                // indent, dedent 처리
                var indent = rawLine.Length - trimmed.Length;
                if (indent > indentStack.Peek()) {
                    indentStack.Push(indent);
                    tokens.Add(new YisoScriptToken(YisoScriptTokenType.Indent, "", lineNum));
                }
                else {
                    while (indent < indentStack.Peek()) {
                        indentStack.Pop();
                        tokens.Add(new YisoScriptToken(YisoScriptTokenType.Dedent, "", lineNum));
                    }

                    if (indent != indentStack.Peek()) {
                        throw new YisoScriptException("잘못된 들여쓰기 - 이전 레벨과 맞지 않음", lineNum);
                    }
                }
                
                // 줄 내용 tokenizing
                TokenizeLine(trimmed, lineNum, tokens);
                tokens.Add(new YisoScriptToken(YisoScriptTokenType.Newline, "", lineNum));
            }

            // 남은 dedent 처리 + eof 추가
            var finalLine = lines.Length;
            while (indentStack.Peek() > 0) {
                indentStack.Pop();
                tokens.Add(new YisoScriptToken(YisoScriptTokenType.Dedent, "", finalLine));
            }
            tokens.Add(new YisoScriptToken(YisoScriptTokenType.Eof, "", finalLine));
            
            return tokens;
        }

        /// <summary>
        /// 줄 내용 처리
        /// 맞는 tokenizer 사용
        /// </summary>
        /// <param name="line"></param>
        /// <param name="lineNum"></param>
        /// <param name="tokens"></param>
        private void TokenizeLine(string line, int lineNum, List<YisoScriptToken> tokens) {
            if (line.StartsWith("@")) TokenizeBlockDecl(line, lineNum, tokens);
            else if (line.StartsWith("[")) TokenizeDialogueLine(line, lineNum, tokens);
            else if (line.StartsWith("?")) TokenizeBranch(line, lineNum, tokens);
            else TokenizeCommand(line, lineNum, tokens);
        }

        // @dialogue <id>
        private void TokenizeBlockDecl(string line, int lineNum, List<YisoScriptToken> tokens) {
            var spaceIdx = line.IndexOfAny(new[] {' ', '\t'});
            var keyword = spaceIdx == -1 ? line : line[..spaceIdx];
            var rest = spaceIdx == -1 ? "" : line[(spaceIdx + 1)..].Trim();
            
            var type = keyword switch {
                "@dialogue" => YisoScriptTokenType.AtDialogue,
                "@quest"    => YisoScriptTokenType.AtQuest,
                "@trigger"  => YisoScriptTokenType.AtTrigger,
                "@wave"     => YisoScriptTokenType.AtWave,
                "@cutscene" => YisoScriptTokenType.AtCutscene,
                _ => throw new YisoScriptException($"알 수 없는 블록 타입: {keyword}", lineNum)
            };
            
            tokens.Add(new YisoScriptToken(type, keyword, lineNum));
            if (!string.IsNullOrEmpty(rest)) {
                tokens.Add(new YisoScriptToken(YisoScriptTokenType.Identifier, rest, lineNum));
            }
        }

        // [화자]: 텍스트  또는  [화자]: @loc:key
        private void TokenizeDialogueLine(string line, int lineNum, List<YisoScriptToken> tokens) {
            var closeBracket = line.IndexOf(']');
            if (closeBracket == -1) 
                throw new YisoScriptException("']' 누락", lineNum);

            var speaker = line[1..closeBracket].Trim();
            tokens.Add(new YisoScriptToken(YisoScriptTokenType.Speaker, speaker, lineNum));
            
            var colonIdx = line.IndexOf(':');
            if (colonIdx == -1)
                throw new YisoScriptException("':' 누락", lineNum);
            
            var text = line[(colonIdx + 1)..].Trim();
            if (text.StartsWith("@loc:")) {
                tokens.Add(new YisoScriptToken(YisoScriptTokenType.LocaleKey, text[5..], lineNum));
            }
            else {
                tokens.Add(new YisoScriptToken(YisoScriptTokenType.DialogueText, text, lineNum));
            }
        }

        // ? 조건 또는 ? default
        private void TokenizeBranch(string line, int lineNum, List<YisoScriptToken> tokens) {
            tokens.Add(new YisoScriptToken(YisoScriptTokenType.Question, "?", lineNum));
            var rest = line[1..].Trim();
            if (rest == "default") {
                tokens.Add(new YisoScriptToken(YisoScriptTokenType.Default, "default", lineNum));
            }
            else {
                TokenizeCommand(rest, lineNum, tokens);
            }
        }

        // 커맨드/키워드 줄: 문자 단위 토크나이징
        private void TokenizeCommand(string line, int lineNum, List<YisoScriptToken> tokens) {
            var i = 0;
            while (i < line.Length) {
                var c = line[i];

                if (c == ' ' || c == '\t') { i++; continue; }
                
                // 인라인 주석 처리
                if (c == '/' && i+1 < line.Length && line[i+1] == '/') break;
                
                // 문자열 리터럴
                if (c == '"') {
                    i++;
                    var sb = new StringBuilder();
                    while (i < line.Length && line[i] != '"') sb.Append(line[i++]);
                    if (i < line.Length) i++;
                    tokens.Add(new YisoScriptToken(YisoScriptTokenType.String, sb.ToString(), lineNum));
                    continue;
                }
                
                // 숫자 (음수 포함)
                if (char.IsDigit(c) || (c == '-' && i + 1 < line.Length && char.IsDigit(line[i + 1]))) {
                    var start = i;
                    var useDecimalPoint = false;

                    if (c == '-') i++;

                    if (!char.IsDigit(line[i]))
                        throw new YisoScriptException("- 뒤에는 숫자가 와야함", lineNum);

                    while (i < line.Length && (char.IsDigit(line[i]) || line[i] == '.')) {
                        if (line[i] == '.') {
                            if (useDecimalPoint)
                                throw new YisoScriptException("소수점 중복 사용", lineNum);

                            if (i + 1 >= line.Length)
                                throw new YisoScriptException("소수점으로 끝나면 안됨", lineNum);

                            if (!char.IsDigit(line[i - 1]) || !char.IsDigit(line[i + 1]))
                                throw new YisoScriptException("소수점 양 옆에는 숫자가 와야함", lineNum);

                            useDecimalPoint = true;
                        }
                        i++;
                    }

                    tokens.Add(new YisoScriptToken(YisoScriptTokenType.Number, line[start..i], lineNum));
                    continue;
                }
                
                // 식별자 / 키워드
                if (char.IsLetter(c) || c == '_') {
                    var start = i;
                    while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '_')) i++;
                    var word = line[start..i];

                    if (Keywords.TryGetValue(word, out var kwType)) {
                        tokens.Add(new YisoScriptToken(kwType, word, lineNum));
                    }
                    else {
                        tokens.Add(new YisoScriptToken(YisoScriptTokenType.Identifier, word, lineNum));
                    }
                    continue;
                }
                
                // 단일 문자 토큰
                YisoScriptTokenType singleType;
                switch (c) {
                    case '(': singleType = YisoScriptTokenType.LParen;  break;
                    case ')': singleType = YisoScriptTokenType.RParen;  break;
                    case ',': singleType = YisoScriptTokenType.Comma;   break;
                    case '=': singleType = YisoScriptTokenType.Equals;  break;
                    case ':': singleType = YisoScriptTokenType.Colon;   break;
                    case '.': singleType = YisoScriptTokenType.Dot;     break;
                    default:
                        throw new YisoScriptException($"알 수 없는 문자: '{c}'", lineNum);
                }
                tokens.Add(new YisoScriptToken(singleType, c.ToString(), lineNum));
                i++;
            }
        }
    }
}