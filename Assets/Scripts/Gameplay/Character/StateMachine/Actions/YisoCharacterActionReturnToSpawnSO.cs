using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using Gameplay.Character.Data;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Actions {
    /// <summary>
    /// 스폰 위치로 복귀하는 액션.
    /// Blackboard의 SpawnPosition 키에 저장된 위치로 이동합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_FSM_Action_Common_ReturnToSpawn", menuName = "Yiso/State Machine/Action/ReturnToSpawn")]
    public class YisoCharacterActionReturnToSpawnSO: YisoCharacterActionSO {
        [Header("Blackboard Keys")]
        [SerializeField] private YisoBlackboardKeySO spawnPositionKey; // 스폰 위치 (Vector3)
        [SerializeField] private YisoBlackboardKeySO targetKey; // 타겟 키 (초기화용)

        public override void PerformAction(IYisoCharacterContext context) {
            if (spawnPositionKey == null) {
                Debug.LogError($"[ReturnToSpawn] Spawn Position Key is not assigned.", this);
                return;
            }

            var aiModule = context.GetModule<YisoCharacterAIModule>();
            var bb = context.GetModule<YisoCharacterBlackboardModule>();
            if (aiModule == null || bb == null) {
                Debug.LogWarning($"[ReturnToSpawn] AIModule or Blackboard is null!");
                return;
            }

            // Target 초기화 (복귀 중엔 타겟을 추적하지 않음)
            if (targetKey != null) {
                bb.SetObject(targetKey, null);
                Debug.Log($"[ReturnToSpawn] Target cleared from Blackboard");
            }

            // SpawnPosition 가져오기
            var spawnPosition = bb.GetVector(spawnPositionKey, context.Transform.position);
            var currentPosition = context.Transform.position;

            // 디버깅: spawnPosition이 defaultValue(현재 위치)인지 확인
            if (Vector3.Distance(spawnPosition, currentPosition) < 0.01f) {
                Debug.LogWarning($"[ReturnToSpawn] SpawnPosition equals current position! " +
                                 $"Blackboard may not have been initialized. Current: {currentPosition}, Spawn: {spawnPosition}");
            } else {
                Debug.Log($"[ReturnToSpawn] Setting destination to spawn position. Current: {currentPosition}, Spawn: {spawnPosition}");
            }

            // 스폰 위치로 이동
            var targetPosition = (Vector2)spawnPosition;
            aiModule.SetDestination(targetPosition);

            // 추가 디버깅: Destination이 제대로 설정되었는지 확인
            Debug.Log($"[ReturnToSpawn] AIModule.Destination set to: {aiModule.Destination}, PathDirection will be: {((Vector2)context.Transform.position - targetPosition).normalized * -1f}");
        }
    }
}