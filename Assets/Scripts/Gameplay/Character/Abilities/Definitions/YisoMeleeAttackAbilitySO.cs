using UnityEngine;

namespace Gameplay.Character.Abilities.Definitions {
    /// <summary>
    /// 근접 공격 Ability의 설정을 정의하는 ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Ability_MeleeAttack", menuName = "Yiso/Abilities/Melee Attack")]
    public class YisoMeleeAttackAbilitySO : YisoAbilitySO {
        [Header("Input Settings")]
        [Tooltip("연속 입력 모드: true = 버튼을 누르고 있는 동안 연속 공격, false = 버튼을 누를 때마다 한 번씩 공격")]
        public bool continuousPressAttack = false;

        [Header("Attack Settings")]
        [Tooltip("콤보 공격 사용 여부")]
        public bool useComboAttacks = false;
        
        public override IYisoCharacterAbility CreateAbility() {
            return new YisoMeleeAttackAbility(this);
        }
    }
}
