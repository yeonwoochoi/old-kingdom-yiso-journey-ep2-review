using System.Collections;
using Core;
using Core.Input;
using Core.Log;
using Core.Singleton;
using Core.Sound;
using UnityEngine;
using World.Scripting.API;
using World.Scripting.AST;
using World.Scripting.Core;

namespace World.Scripting {
    /// <summary>
    /// [역할] ScriptingSystem 진입점
    /// [책임]
    ///   - 초기화: ScriptAsset 로드, Runner 생성, 모든 API 등록
    ///   - Execute(blockId): 외부에서 스크립트 블록 실행 요청
    ///   - WAIT / CUTSCENE 등 내장 커맨드 등록
    /// [타입] MonoSingleton
    /// [초기화 시점] Phase 5 이후 SceneGameMap 씬에서 활성화
    /// </summary>
    public class YisoScriptingManager : YisoMonoSingleton<YisoScriptingManager> {
        private YisoScriptRunner _runner;
        private YisoScriptAsset _asset;

        protected override void Awake() {
            base.Awake();

            _runner = GetOrAddComponent<YisoScriptRunner>();
            _asset = new YisoScriptAsset();
            _asset.LoadAll();

            RegisterBuiltinCommands();
            RegisterAPIs();

            YisoLogger.Info("[ScriptingManager] 초기화 완료");
        }

        #region Public API

        /// <summary>
        /// blockId에 해당하는 .yiso 블록을 실행한다.
        /// </summary>
        public void Execute(string blockId) {
            var block = _asset.GetBlock(blockId);
            if (block == null) {
                YisoLogger.Error($"[ScriptingManager] 블록 없음: {blockId}");
                return;
            }

            var context = CreateContext();
            _runner.Run(block, context);
        }

        /// <summary>
        /// 실행 완료를 기다려야 할 때 코루틴으로 실행.
        /// </summary>
        public IEnumerator ExecuteAsync(string blockId) {
            var block = _asset.GetBlock(blockId);
            if (block == null) {
                YisoLogger.Error($"[ScriptingManager] 블록 없음: {blockId}");
                yield break;
            }

            var context = CreateContext();
            yield return StartCoroutine(_runner.RunBlock(block.Body, context));
        }

        #endregion

        #region Context 생성

        private YisoScriptContext CreateContext() {
            return new YisoScriptContext {
                // Phase 3: SaveSystem 연결 시 HasFlagQuery 교체
                HasFlagQuery = _ => false,
                // Phase 6: QuestSystem 연결 시 HasQuestQuery 교체
                HasQuestQuery = _ => false,
                // Phase 6: InventorySystem 연결 시 HasItemQuery 교체
                HasItemQuery = _ => false,
            };
        }

        #endregion

        #region 내장 커맨드 등록

        private void RegisterBuiltinCommands() {
            // WAIT(seconds)
            _runner.RegisterCommand("WAIT", args => {
                if (args == null || args.Length == 0) return EmptyCoroutine();
                var seconds = float.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture);
                return WaitCoroutine(seconds);
            });

            // CUTSCENE("blockId") — 다른 블록을 인라인으로 실행
            _runner.RegisterCommand("CUTSCENE", args => {
                if (args == null || args.Length == 0) return EmptyCoroutine();
                var block = _asset.GetBlock(args[0]);
                if (block == null) {
                    YisoLogger.Error($"[ScriptingManager] CUTSCENE 블록 없음: {args[0]}");
                    return EmptyCoroutine();
                }
                return _runner.RunBlock(block.Body, CreateContext());
            });
        }

        private IEnumerator WaitCoroutine(float seconds) {
            yield return new WaitForSeconds(seconds);
        }

        private IEnumerator EmptyCoroutine() {
            yield break;
        }

        #endregion

        #region API 등록

        private void RegisterAPIs() {
            // 즉시 연결 가능한 API
            new CameraScriptAPI(YisoCameraManager.Instance).Register(_runner);
            new SoundScriptAPI(YisoSoundManager.Instance).Register(_runner);
            new InputScriptAPI(YisoInputManager.Instance).Register(_runner);
            new EventScriptAPI().Register(_runner);

            // Stub API (각 시스템 구현 시 교체)
            new DialogueScriptAPI().Register(_runner); // Phase 6
            new QuestScriptAPI().Register(_runner); // Phase 6
            new SpawnScriptAPI().Register(_runner); // Phase 5
            new TriggerScriptAPI().Register(_runner); // Phase 5
        }

        #endregion
    }
}