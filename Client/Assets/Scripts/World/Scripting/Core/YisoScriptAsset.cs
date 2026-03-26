using System.Collections.Generic;
using System.IO;
using Core.Log;
using UnityEngine;
using World.Scripting.AST;

namespace World.Scripting.Core {
    /// <summary>
    /// [역할] .yiso 파일 로드 + 파싱 결과 캐싱
    /// [책임]
    ///   - 빌드 타입에 따른 로딩 경로 분기
    ///     · Editor/Dev: StreamingAssets/Scripts/ 직접 읽기 (핫리로드)
    ///     · Release:    Addressables 번들 (TODO: Phase 9에서 구현)
    ///   - 모든 .yiso 파일의 블록을 ID 기준으로 인덱싱
    /// </summary>
    public class YisoScriptAsset {
        private readonly Dictionary<string, YisoScriptBlockNode> _blocks = new();

        private readonly YisoScriptLexer _lexer = new();
        private readonly YisoScriptParser _parser = new();

        /// <summary>
        /// StreamingAssets/Scripts/ 하위 모든 .yiso 파일을 로드해 블록 인덱스 구축
        /// ScriptingManager.Awake()에서 한 번 호출
        /// </summary>
        public void LoadAll() {
            _blocks.Clear();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            LoadFromStreamingAssets();
#else
            // TODO: Phase 9 — Addressables 번들에서 로드
            YisoLogger.Warn("[ScriptAsset] Release 빌드 로딩 미구현 — StreamingAssets 폴백");
            LoadFromStreamingAssets();
#endif
        }

        private void LoadFromStreamingAssets() {
            var root = Path.Combine(Application.streamingAssetsPath, "Scripts");
            if (!Directory.Exists(root)) {
                YisoLogger.Warn($"[ScriptAsset] 스크립트 폴더 없음: {root}");
                return;
            }

            foreach (var file in Directory.GetFiles(root, "*.yiso", SearchOption.AllDirectories)) {
                try {
                    var text = File.ReadAllText(file, System.Text.Encoding.UTF8);
                    var tokens = _lexer.Tokenize(text);
                    var parsed = _parser.Parse(tokens);

                    foreach (var block in parsed) {
                        if (!_blocks.TryAdd(block.Id, block))
                            YisoLogger.Warn($"[ScriptAsset] 중복 블록 ID: {block.Id} ({file})");
                    }

                    YisoLogger.Debug($"[ScriptAsset] 로드 완료: {Path.GetFileName(file)} ({parsed.Count}개 블록)");
                }
                catch (YisoScriptException e) {
                    YisoLogger.Error($"[ScriptAsset] 파싱 실패: {Path.GetFileName(file)} — {e.Message}");
                }
            }

            YisoLogger.Info($"[ScriptAsset] 총 {_blocks.Count}개 블록 로드 완료");
        }

        public YisoScriptBlockNode GetBlock(string id) {
            if (_blocks.TryGetValue(id, out var block)) return block;
            YisoLogger.Warn($"[ScriptAsset] 블록 없음: {id}");
            return null;
        }

        public bool HasBlock(string id) => _blocks.ContainsKey(id);
    }
}