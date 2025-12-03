using Gameplay.Character.Abilities;
using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Actions {
    /// <summary>
    /// 근접 공격(Melee Attack) 액션.
    /// YisoMeleeAttackAbility의 TriggerAttack()을 호출합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_FSM_Action_Common_AttackMelee", menuName = "Yiso/State Machine/Action/AttackMelee")]
    public class YisoCharacterActionAttackMeleeSO: YisoCharacterActionSO {
        public override void PerformAction(IYisoCharacterContext context) {
            // AbilityModule에서 YisoMeleeAttackAbility 가져오기
            var abilityModule = context.GetModule<YisoCharacterAbilityModule>();
            if (abilityModule == null) {
                Debug.LogWarning("[YisoCharacterActionAttackMeleeSO] YisoCharacterAbilityModule을 찾을 수 없습니다.");
                return;
            }

            var meleeAttackAbility = abilityModule.GetAbility<YisoMeleeAttackAbility>();
            if (meleeAttackAbility == null) {
                Debug.LogWarning("[YisoCharacterActionAttackMeleeSO] YisoMeleeAttackAbility를 찾을 수 없습니다.");
                return;
            }

            // 공격 트리거 (AI용)
            meleeAttackAbility.TriggerAttack();
        }
    }
}