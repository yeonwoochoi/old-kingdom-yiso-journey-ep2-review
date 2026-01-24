using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Decisions {
    public class YisoCharacterDecisionTargetIsAlive: YisoCharacterDecision {
        [Title("Target Selection")]
        [SerializeField] private bool isMainTarget = true;

        [SerializeField]
        [ShowIf("@!isMainTarget")]
        [MinValue(1), MaxValue("@MaxSlotIndex")]
        private int targetSlotNumber = 1;

        public override bool Decide() {
            if (StateMachine == null) return true;

            var targetIndex = isMainTarget ? 0 : targetSlotNumber;
            var targetContext = StateMachine.GetTargetContext(targetIndex);

            if (targetContext != null) {
                return !targetContext.IsDead();
            }
            
            return false;
        }
    }
}
