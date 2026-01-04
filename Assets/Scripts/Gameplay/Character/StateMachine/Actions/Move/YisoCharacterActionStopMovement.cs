using UnityEngine;

namespace Gameplay.Character.StateMachine.Actions.Move {
    public class YisoCharacterActionStopMovement: YisoCharacterAction {
        public override void PerformAction() {
            StateMachine.Owner?.Move(Vector2.zero);
        }
    }
}