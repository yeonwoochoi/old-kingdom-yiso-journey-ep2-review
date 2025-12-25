using UnityEngine;

namespace Gameplay.Character.StateMachine.Actions.Move {
    public class YisoCharacterActionMoveTowardTarget: YisoCharacterAction {
        [Tooltip("True일 경우 타겟 반대 방향으로 이동합니다. (도망)")]
        [SerializeField] private bool moveAway = false;
        
        public override void PerformAction() { }
    }
}