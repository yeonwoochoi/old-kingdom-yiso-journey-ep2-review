using UnityEngine;

namespace Gameplay.Character.StateMachine.Actions.Orientation {
    public class YisoCharacterFaceTowardTarget: YisoCharacterAction {
        [Tooltip("타겟 슬롯 인덱스 (0 = Main Target)")]
        [SerializeField] private int targetIndex = 0;

        public override void PerformAction() {
            if (!StateMachine.HasTarget(targetIndex) || StateMachine?.Owner == null) return;

            var direction = StateMachine.GetDirectionToTarget(targetIndex);
            if (direction.sqrMagnitude < 0.01f) return;

            StateMachine.Owner.Face(direction);
        }
    }
}