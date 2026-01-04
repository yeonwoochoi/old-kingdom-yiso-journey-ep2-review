using Gameplay.Character.Core.Modules;
using Gameplay.Character.StateMachine.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Decisions {
    /// <summary>
    /// 스폰 위치로부터의 거리를 비교하는 Decision.
    /// AIModule에서 스폰 위치를 가져와 현재 위치와 비교합니다.
    /// </summary>
    public class YisoCharacterDecisionDistanceToSpawn: YisoCharacterDecision {
        [Title("Settings")]
        [EnumToggleButtons]
        [SerializeField] private ComparisonMode comparison = ComparisonMode.GreaterThan;

        [SerializeField, Min(0f)] private float distance = 10f;

        // 부동소수점 비교 오차 허용 범위 (거리 제곱 기준)
        private const float SqrTolerance = 0.01f;

        public override bool Decide() {
            if (StateMachine.Owner == null) return false;

            var spawnPosition = StateMachine.SpawnPosition;
            var currentPosition = StateMachine.Owner.Transform.position;

            var currentDistSqr = YisoStateMachineUtils.GetDistanceSqr(currentPosition, spawnPosition);
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
