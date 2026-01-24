using Gameplay.Character.Abilities;

namespace Gameplay.Character.StateMachine.Decisions {
    /// <summary>
    /// 현재 공격 중이 아닌지 확인하는 Decision.
    ///
    /// 사용 예:
    /// - 다른 Decision과 조합하여 "타겟을 잃었고 AND 공격 중이 아님" 조건 만들기
    /// - 공격 중에는 절대 상태 전이가 일어나지 않도록 할 때
    ///
    /// true: 공격 중이 아님
    /// false: 공격 중
    /// </summary>
    public class YisoCharacterDecisionIsNotAttacking : YisoCharacterDecision {
        private YisoAttackAbilityBase _attackAbility;

        public override void OnEnterState() {
            base.OnEnterState();

            // Ability 캐싱
            var abilityModule = StateMachine?.GetAbilityModule();
            _attackAbility = abilityModule?.GetAbility<YisoMeleeAttackAbility>();
        }

        public override bool Decide() {
            if (_attackAbility == null) return true;
            return !_attackAbility.IsAttacking();
        }
    }
}
