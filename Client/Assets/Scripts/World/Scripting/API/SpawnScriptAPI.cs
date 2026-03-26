using System.Collections;
using Core.Log;
using World.Scripting.Core;

namespace World.Scripting.API {
    /// <summary>
    /// 커맨드: SPAWN / WAVE_WAIT
    /// TODO: Phase 5 — SpawnSystem 구현 후 실제 연결
    /// WAVE_WAIT: 현재 웨이브의 모든 적 처치 대기 — SpawnSystem이 완료 시그널 제공
    /// </summary>
    public class SpawnScriptAPI : IYisoScriptAPI {
        public void Register(YisoScriptRunner runner) {
            runner.RegisterCommand("SPAWN", OnSpawn);
            runner.RegisterCommand("WAVE_WAIT", OnWaveWait);
        }

        // SPAWN — Runner가 YisoSpawnNode를 변환해 호출
        // Args[0] = entityId, Args[1] = count, Args[2] = interval
        private IEnumerator OnSpawn(string[] args) {
            if (args == null || args.Length == 0) yield break;
            var entityId = args[0];
            var count = args.Length > 1 ? args[1] : "1";
            // TODO: SpawnSystem.Spawn(entityId, count)
            YisoLogger.Debug($"[SpawnScriptAPI] SPAWN {entityId} x{count}");
            yield break;
        }

        // WAVE_WAIT — 웨이브 내 모든 적 처치까지 대기
        private IEnumerator OnWaveWait(string[] args) {
            // TODO: yield return new WaitUntil(() => SpawnSystem.IsWaveCleared())
            YisoLogger.Debug("[SpawnScriptAPI] WAVE_WAIT (stub — 즉시 반환)");
            yield break;
        }
    }
}