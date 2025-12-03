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

        public override void PerformAction(IYisoCharacterContext context) {
            var aiModule = context.GetModule<YisoCharacterAIModule>();
            var bb = context.GetModule<YisoCharacterBlackboardModule>();
            if (aiModule == null || bb == null) return;

            // SpawnPosition 가져오기
            var spawnPosition = bb.GetVector(spawnPositionKey, context.Transform.position);

            // 스폰 위치로 이동
            var targetPosition = (Vector2)spawnPosition;
            aiModule.SetDestination(targetPosition);
        }
    }
}