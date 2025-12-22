using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using Gameplay.Character.Data;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Decisions {
    /// <summary>
    /// 스폰 위치로부터 일정 거리 이상 벗어났는지 확인하는 Decision.
    /// 적이 플레이어를 추격하다가 너무 멀리 가면 스폰 위치로 복귀하도록 하는 데 사용.
    /// true: 스폰 위치로부터 distanceThreshold보다 멀리 벗어남 (복귀 필요)
    /// false: 스폰 위치 근처에 있음 (정상 범위 내)
    /// </summary>
    [CreateAssetMenu(fileName = "SO_FSM_Decision_Common_DistanceFromSpawn", menuName = "Yiso/State Machine/Decision/DistanceFromSpawn")]
    public class YisoCharacterDecisionDistanceFromSpawnSO : YisoCharacterDecisionSO {
        [Header("Settings")]
        [Tooltip("스폰 위치로부터 이 거리보다 멀어지면 true 반환 (복귀 신호)")]
        [SerializeField] private float distanceThreshold = 10f; // 범위 임계값

        [Header("Blackboard Keys")]
        [SerializeField] private YisoBlackboardKeySO spawnPositionKey; // 스폰 위치를 저장한 키

        public override bool Decide(IYisoCharacterContext context) {
            var bb = context.GetModule<YisoCharacterBlackboardModule>();
            if (bb == null) {
                // Blackboard가 없으면 false 반환 (안전하게 처리)
                return false;
            }

            // Blackboard에서 스폰 위치 가져오기
            // 값이 없으면 현재 위치를 기본값으로 사용 (false 반환을 위해)
            var spawnPosition = bb.GetVector(spawnPositionKey, context.Transform.position);

            // 현재 위치와 스폰 위치 간 거리 계산
            var currentPosition = (Vector2)context.Transform.position;
            var spawnPosition2D = (Vector2)spawnPosition;
            var distance = Vector2.Distance(currentPosition, spawnPosition2D);

            // 거리가 threshold보다 크면 true (범위 벗어남 → 복귀 필요)
            return distance > distanceThreshold;
        }
    }
}