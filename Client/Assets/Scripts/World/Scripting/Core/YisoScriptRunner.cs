using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Core.Log;
using UnityEngine;
using World.Scripting.AST;

namespace World.Scripting.Core {
    /// <summary>
    /// [역할] AST를 Coroutine 기반으로 순차 실행
    /// [책임]
    ///   - 노드 타입별 실행 분기
    ///   - 커맨드 핸들러 테이블 관리 (API들이 RegisterCommand로 등록)
    ///   - 분기 조건 평가 → ScriptContext 위임
    /// [타입] YisoBehaviour (MonoBehaviour) — StartCoroutine 필요
    /// </summary>
    public class YisoScriptRunner : YisoBehaviour {
        private readonly Dictionary<string, Func<string[], IEnumerator>> _handlers = new();

        // ─── 커맨드 등록 ─────────────────────────────────────────────

        public void RegisterCommand(string name, Func<string[], IEnumerator> handler) {
            if (_handlers.ContainsKey(name))
                YisoLogger.Warn($"[ScriptRunner] 커맨드 재등록: {name}");
            _handlers[name] = handler;
        }

        // ─── 실행 진입점 ─────────────────────────────────────────────

        public void Run(YisoScriptBlockNode block, YisoScriptContext context) {
            StartCoroutine(ExecuteBlock(block.Body, context));
        }

        /// <summary>
        /// 외부(ScriptingManager의 CUTSCENE 핸들러 등)에서 블록 body를
        /// yield return으로 이어 실행할 때 사용.
        /// </summary>
        public IEnumerator RunBlock(List<YisoScriptNode> nodes, YisoScriptContext context) {
            return ExecuteBlock(nodes, context);
        }

        // ─── 노드 순회 ────────────────────────────────────────────────

        private IEnumerator ExecuteBlock(List<YisoScriptNode> nodes, YisoScriptContext context) {
            foreach (var node in nodes)
                yield return StartCoroutine(ExecuteNode(node, context));
        }

        private IEnumerator ExecuteNode(YisoScriptNode node, YisoScriptContext context) {
            switch (node) {
                case YisoDialogueLineNode d:
                    yield return StartCoroutine(ExecuteDialogue(d, context));
                    break;

                case YisoBranchNode b:
                    yield return StartCoroutine(ExecuteBranch(b, context));
                    break;

                case YisoCommandNode c:
                    yield return StartCoroutine(ExecuteCommand(c));
                    break;

                case YisoSetFlagNode s:
                    context.SetFlag(s.Key, s.Value);
                    break;

                case YisoWaveNode w:
                    yield return StartCoroutine(ExecuteWave(w));
                    break;

                case YisoHookNode h:
                    yield return StartCoroutine(ExecuteBlock(h.Body, context));
                    break;

                case YisoRewardNode r:
                    yield return StartCoroutine(ExecuteCommand(new YisoCommandNode {
                        Name = "REWARD",
                        Args = new[] { r.Exp.ToString(), r.Gold.ToString() }
                    }));
                    break;
            }
        }

        // ─── 개별 노드 실행 ───────────────────────────────────────────

        private IEnumerator ExecuteDialogue(YisoDialogueLineNode node, YisoScriptContext context) {
            if (!_handlers.TryGetValue("DIALOGUE", out var handler)) {
                YisoLogger.Warn("[ScriptRunner] DIALOGUE 핸들러 미등록");
                yield break;
            }
            // @loc:key 형태로 전달 — DialogueScriptAPI에서 LocalizationManager 조회
            var textArg = node.IsLocaleKey ? $"@loc:{node.LocaleKey}" : node.RawText;
            yield return StartCoroutine(handler(new[] { node.Speaker, textArg }));
        }

        private IEnumerator ExecuteBranch(YisoBranchNode node, YisoScriptContext context) {
            foreach (var branchCase in node.Cases) {
                if (!context.Evaluate(branchCase.Condition)) continue;
                yield return StartCoroutine(ExecuteBlock(branchCase.Body, context));
                yield break; // 첫 번째 매칭 케이스만 실행
            }
        }

        private IEnumerator ExecuteCommand(YisoCommandNode node) {
            if (_handlers.TryGetValue(node.Name, out var handler)) {
                yield return StartCoroutine(handler(node.Args));
            } else {
                YisoLogger.Warn($"[ScriptRunner] 등록되지 않은 커맨드: {node.Name}");
            }
        }

        private IEnumerator ExecuteWave(YisoWaveNode node) {
            foreach (var spawn in node.Spawns) {
                if (_handlers.TryGetValue("SPAWN", out var spawnHandler)) {
                    yield return StartCoroutine(spawnHandler(new[] {
                        spawn.EntityId,
                        spawn.Count.ToString(),
                        spawn.Interval.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    }));
                }
                if (spawn.Interval > 0f)
                    yield return new WaitForSeconds(spawn.Interval);
            }
            // 웨이브 클리어 대기 — SpawnScriptAPI가 등록
            if (_handlers.TryGetValue("WAVE_WAIT", out var waitHandler))
                yield return StartCoroutine(waitHandler(null));
        }
    }
}
