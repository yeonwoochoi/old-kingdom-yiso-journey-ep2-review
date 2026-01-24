using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Decisions {
    /// <summary>
    /// 지정된 타겟 슬롯이 null인지 확인하는 Decision.
    /// true: 타겟이 null (타겟 없음)
    /// false: 타겟이 존재함
    /// </summary>
    public class YisoCharacterDecisionTargetIsNull: YisoCharacterDecision {
        [Title("Target Selection")]
        [SerializeField] private bool isMainTarget = true;

        [SerializeField]
        [ShowIf("@!isMainTarget")]
        [MinValue(1), MaxValue("@MaxSlotIndex")]
        private int targetSlotNumber = 1;

        public override bool Decide() {
            if (StateMachine == null) return true;

            var targetIndex = isMainTarget ? 0 : targetSlotNumber;
            return !StateMachine.HasTarget(targetIndex);
        }
    }
}
