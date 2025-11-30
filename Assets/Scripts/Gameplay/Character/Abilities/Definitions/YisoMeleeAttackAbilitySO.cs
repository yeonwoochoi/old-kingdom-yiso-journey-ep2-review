using UnityEngine;

namespace Gameplay.Character.Abilities.Definitions {
    /// <summary>
    /// 근접 공격 Ability의 설정을 정의하는 ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Ability_MeleeAttack", menuName = "Yiso/Abilities/Melee Attack")]
    public class YisoMeleeAttackAbilitySO : YisoAbilitySO {
        [Header("Input Settings")]
        [Tooltip("공격 입력 버튼 이름 (InputSystem의 Action 이름)")]
        public string attackInputName = "Attack";

        [Header("Attack Settings")]
        [Tooltip("콤보 공격 사용 여부")]
        public bool useComboAttacks = false;

        [Tooltip("공격 중 이동 가능 여부")]
        public bool canMoveWhileAttacking = false;

        public override IYisoCharacterAbility CreateAbility() {
            return new YisoMeleeAttackAbility(this);
        }
    }
}
