using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using Gameplay.Character.Data;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Decisions {
    /// <summary>
    /// 타겟까지의 거리를 체크하는 Decision.
    /// true: 거리가 threshold 이하 (타겟이 범위 내에 있음)
    /// false: 거리가 threshold 초과 (타겟이 범위 밖에 있음)
    /// </summary>
    [CreateAssetMenu(fileName = "SO_FSM_Decision_Common_DistanceToTarget", menuName = "Yiso/State Machine/Decision/DistanceToTarget")]
    public class YisoCharacterDecisionDistanceToTargetSO: YisoCharacterDecisionSO {
        [Header("Settings")]
        [SerializeField] private float distanceThreshold = 1.5f; // 거리 임계값

        [Header("Blackboard Keys")]
        [SerializeField] private YisoBlackboardKeySO targetKey; // 타겟 (Transform 또는 GameObject)

        public override bool Decide(IYisoCharacterContext context) {
            var bb = context.GetModule<YisoCharacterBlackboardModule>();
            if (bb == null) return false; // Blackboard가 없으면 false

            // Transform 또는 GameObject로 타겟 가져오기
            var target = bb.GetObject<Transform>(targetKey);
            if (target == null) {
                var targetGameObject = bb.GetObject<GameObject>(targetKey);
                if (targetGameObject != null) {
                    target = targetGameObject.transform;
                }
            }

            // 타겟이 없으면 false
            if (target == null) return false;

            // 거리 계산
            var currentPosition = (Vector2)context.Transform.position;
            var targetPosition = (Vector2)target.position;
            var distance = Vector2.Distance(currentPosition, targetPosition);

            // 거리가 threshold 이하면 true
            return distance <= distanceThreshold;
        }
    }
}