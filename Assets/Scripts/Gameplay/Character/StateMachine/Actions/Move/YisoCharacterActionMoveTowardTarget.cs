using UnityEngine;

namespace Gameplay.Character.StateMachine.Actions.Move {
    /// <summary>
    /// 메인 타겟을 향해 또는 반대 방향으로 이동하는 액션입니다.
    /// moveAway = true면 타겟에서 도망치는 행동을 합니다.
    /// </summary>
    public class YisoCharacterActionMoveTowardTarget: YisoCharacterAction {
        [Tooltip("True일 경우 타겟 반대 방향으로 이동합니다. (도망)")]
        [SerializeField] private bool moveAway = false;

        [Tooltip("타겟 슬롯 인덱스 (0 = Main Target)")]
        [SerializeField] private int targetIndex = 0;

        [Tooltip("타겟으로부터 유지할 거리 (이 거리 이내면 이동 멈춤)")]
        [SerializeField] private float stopDistance = 0.5f;

        public override void PerformAction() {
            if (!StateMachine.HasTarget(targetIndex) || StateMachine?.Owner == null) return;

            var distance = StateMachine.GetDistanceToTarget(targetIndex);

            // 이미 목표 거리 내에 있으면 이동하지 않음
            if (distance <= stopDistance) {
                StateMachine.Owner.Move(Vector2.zero);
                return;
            }

            // 타겟 방향 또는 반대 방향으로 이동
            var direction = StateMachine.GetDirectionToTarget(targetIndex);
            if (moveAway) {
                direction = -direction;
            }

            StateMachine.Owner.Move(direction);
        }
    }
}