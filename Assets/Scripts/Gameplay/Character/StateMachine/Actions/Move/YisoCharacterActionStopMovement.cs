using UnityEngine;

namespace Gameplay.Character.StateMachine.Actions.Move {
    public class YisoCharacterActionStopMovement: YisoCharacterAction {
        public override void PerformAction() {
            MovementAbility?.SetMovementInput(Vector2.zero);
        }
    }
}