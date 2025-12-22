using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using Gameplay.Character.Data;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Actions {
    /// <summary>
    /// 배회(Patrol) 이동 액션.
    /// SpawnPosition 주변의 랜덤한 위치로 이동합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_FSM_Action_Common_MovePatrol", menuName = "Yiso/State Machine/Action/MovePatrol")]
    public class YisoCharacterActionMovePatrolSO: YisoCharacterActionSO {
        [Header("Patrol Settings")]
        [SerializeField] private float patrolRadius = 3f; // 스폰 위치 기준 배회 반경

        [Header("Blackboard Keys")]
        [SerializeField] private YisoBlackboardKeySO spawnPositionKey; // 스폰 위치 (Vector3)

        public override void PerformAction(IYisoCharacterContext context) {
            var aiModule = context.GetModule<YisoCharacterAIModule>();
            var bb = context.GetModule<YisoCharacterBlackboardModule>();
            if (aiModule == null || bb == null) return;

            // SpawnPosition 가져오기
            var spawnPosition = bb.GetVector(spawnPositionKey, context.Transform.position);

            // 스폰 위치 주변의 랜덤한 위치 계산
            var randomOffset = Random.insideUnitCircle * patrolRadius;
            var targetPosition = (Vector2)spawnPosition + randomOffset;

            // AI 모듈에 목표 지점 설정
            aiModule.SetDestination(targetPosition);
        }
    }
}