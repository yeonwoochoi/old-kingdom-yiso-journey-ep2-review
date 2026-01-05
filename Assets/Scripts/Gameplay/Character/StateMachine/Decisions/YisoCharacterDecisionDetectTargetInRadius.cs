using Gameplay.Character.StateMachine.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Decisions {
    /// <summary>
    /// 반경 내에서 타겟을 감지하는 Decision.
    /// true: 타겟을 감지함 (지정된 슬롯에 저장됨)
    /// false: 타겟을 감지하지 못함
    /// </summary>
    public class YisoCharacterDecisionDetectTargetInRadius: YisoCharacterDecision {
        [Title("Detection Settings")]
        [SerializeField, Min(0f)]
        [Tooltip("타겟 감지 반경")]
        private float detectionRadius = 5f;

        [SerializeField]
        [Tooltip("타겟 레이어 마스크")]
        private LayerMask targetLayer;

        [Title("Target Selection")]
        [SerializeField] private bool isMainTarget = true;

        [SerializeField]
        [ShowIf("@!isMainTarget")]
        [MinValue(1), MaxValue("@MaxSlotIndex")]
        private int targetSlotNumber = 1;

        public override bool Decide() {
            if (StateMachine == null) return false;

            var origin = (Vector2)StateMachine.Owner.Transform.position;
            var detectedTarget = YisoStateMachineUtils.FindClosestTarget(origin, detectionRadius, targetLayer);
            var targetIndex = isMainTarget ? 0 : targetSlotNumber;

            if (detectedTarget != null) {
                StateMachine.SetTarget(targetIndex, detectedTarget);
                return true;
            }

            return false;
        }

#if UNITY_EDITOR
        // Unity Editor에서 감지 범위 시각화
        private void OnDrawGizmosSelected() {
            if (StateMachine == null || StateMachine.Owner == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(StateMachine.Owner.Transform.position, detectionRadius);
        }
#endif
    }
}
