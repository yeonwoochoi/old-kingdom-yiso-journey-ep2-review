using Gameplay.Character.StateMachine.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Decisions {
    /// <summary>
    /// 시야각(Cone of Vision) 내에서 타겟을 감지하는 Decision.
    /// 거리, 각도, 장애물을 모두 고려하여 타겟을 감지.
    ///
    /// 동작 방식:
    /// 1. 타겟이 없으면 → 시야각 내에서 새로운 타겟 탐색 + 슬롯에 저장
    /// 2. 타겟이 있으면 → 해당 타겟이 여전히 시야각 내에 있는지 확인
    ///
    /// true: 시야각 내에 타겟이 있음
    /// false: 시야각 내에 타겟이 없음
    /// </summary>
    public class YisoCharacterDecisionDetectTargetConeOfVision: YisoCharacterDecision {
        [Title("Vision Settings")]
        [SerializeField, Range(0f, 360f)]
        [Tooltip("시야각 (예: 90도)")]
        private float viewAngle = 90f;

        [SerializeField, Min(0f)]
        [Tooltip("시야 거리")]
        private float viewDistance = 10f;

        [SerializeField]
        [Tooltip("장애물 레이어 (벽 등)")]
        private LayerMask obstacleMask;

        [Title("Target Settings")]
        [SerializeField]
        [Tooltip("타겟 레이어 마스크 (새로운 타겟 탐색 시 사용)")]
        private LayerMask targetLayer;

        [SerializeField]
        [Tooltip("타겟 슬롯 번호 (0 = Main Target)")]
        [MinValue(0), MaxValue("@MaxSlotIndex")]
        private int targetSlotNumber = 0;

        [SerializeField]
        [Tooltip("타겟이 있어도 더 가까운 타겟으로 교체할지 여부")]
        private bool allowTargetRefresh = false;

        public override bool Decide() {
            if (StateMachine == null) return false;

            var ownerTransform = StateMachine.Owner.Transform;
            var currentTarget = StateMachine.GetTarget(targetSlotNumber);

            // 타겟 재탐색이 허용되거나 타겟이 없으면 새로 찾기
            if (allowTargetRefresh || currentTarget == null) {
                var newTarget = FindClosestTargetInSight(ownerTransform);

                if (newTarget != null) {
                    StateMachine.SetTarget(targetSlotNumber, newTarget);
                    return true;
                }

                // 타겟을 못 찾았는데 기존 타겟도 없으면 false
                if (currentTarget == null) return false;
            }

            // 기존 타겟이 여전히 시야각 내에 있는지 확인
            return IsTargetInSight(ownerTransform, currentTarget);
        }

        private Transform FindClosestTargetInSight(Transform ownerTransform) {
            var origin = (Vector2)ownerTransform.position;

            // 반경 내에서 가장 가까운 타겟 찾기
            var detectedTarget = YisoStateMachineUtils.FindClosestTarget(origin, viewDistance, targetLayer);

            if (detectedTarget == null) return null;

            // 찾은 타겟이 시야각 내에 있는지 확인
            var isInSight = YisoStateMachineUtils.IsTargetInSight(
                ownerTransform,
                detectedTarget,
                viewAngle,
                viewDistance,
                obstacleMask
            );

            return isInSight ? detectedTarget : null;
        }

        private bool IsTargetInSight(Transform ownerTransform, Transform target) {
            return YisoStateMachineUtils.IsTargetInSight(
                ownerTransform,
                target,
                viewAngle,
                viewDistance,
                obstacleMask
            );
        }

#if UNITY_EDITOR
        // Unity Editor에서 시야각 시각화
        private void OnDrawGizmosSelected() {
            if (StateMachine == null || StateMachine.Owner == null) return;

            var ownerTransform = StateMachine.Owner.Transform;
            var position = ownerTransform.position;
            var forward = ownerTransform.right; // 2D Top-Down에서 정면 방향 (transform.right)

            // 시야 거리 원
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(position, viewDistance);

            // 시야각 표시
            var halfAngle = viewAngle * 0.5f;

            // 왼쪽 경계선
            var leftBoundary = Quaternion.Euler(0, 0, halfAngle) * forward;
            Gizmos.color = Color.green;
            Gizmos.DrawRay(position, leftBoundary * viewDistance);

            // 오른쪽 경계선
            var rightBoundary = Quaternion.Euler(0, 0, -halfAngle) * forward;
            Gizmos.DrawRay(position, rightBoundary * viewDistance);

            // 정면 방향
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(position, forward * viewDistance);
        }
#endif
    }
}
