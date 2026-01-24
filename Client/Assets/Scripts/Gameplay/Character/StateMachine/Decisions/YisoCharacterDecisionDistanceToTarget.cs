using Gameplay.Character.StateMachine.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Decisions {
    public class YisoCharacterDecisionDistanceToTarget : YisoCharacterDecision {
        [Title("Settings")]
        [EnumToggleButtons]
        [SerializeField]
        private ComparisonMode comparison = ComparisonMode.LessOrEqual;

        [SerializeField, Min(0f)] private float distance = 5f;

        [Title("Target Selection")] [SerializeField]
        private bool isMainTarget = true;

        [SerializeField] [ShowIf("@!isMainTarget")] [MinValue(1), MaxValue("@MaxSlotIndex")]
        private int targetSlotNumber = 1;

        // 부동소수점 비교 오차 허용 범위 (거리 제곱 기준)
        private const float SqrTolerance = 0.01f;

        public override bool Decide() {
            if (StateMachine == null) return false;

            var targetIndex = isMainTarget ? 0 : targetSlotNumber;
            if (!StateMachine.HasTarget(targetIndex)) return false;

            var target = StateMachine.GetTarget(targetIndex);
            var currentPos = StateMachine.GetCurrentPosition();
            var currentDistSqr = YisoStateMachineUtils.GetDistanceSqr(currentPos, target.position);
            var thresholdSqr = distance * distance;

            switch (comparison) {
                case ComparisonMode.LessThan:
                    return currentDistSqr < thresholdSqr;
                case ComparisonMode.LessOrEqual:
                    return currentDistSqr <= thresholdSqr;
                case ComparisonMode.Equal:
                    return Mathf.Abs(currentDistSqr - thresholdSqr) < SqrTolerance;
                case ComparisonMode.GreaterOrEqual:
                    return currentDistSqr >= thresholdSqr;
                case ComparisonMode.GreaterThan:
                    return currentDistSqr > thresholdSqr;
                case ComparisonMode.NotEqual:
                    return Mathf.Abs(currentDistSqr - thresholdSqr) >= SqrTolerance;
                default:
                    return false;
            }
        }
    }
}