using Gameplay.Character.Abilities;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Actions.Attack {
    /// <summary>
    /// AI가 공격을 실행하도록 트리거하는 액션입니다.
    /// YisoMeleeAttackAbility의 TriggerAttack()을 호출합니다.
    /// </summary>
    public class YisoCharacterActionAttack: YisoCharacterAction {
        [Tooltip("공격 실행 간격 (초 단위, 0이면 매 프레임 시도)")]
        [SerializeField] private float attackInterval = 0.5f;

        private float _nextAttackTime = 0f;

        public override void OnEnterState() {
            base.OnEnterState();
            // 상태 진입 시 즉시 첫 번째 공격 시도 가능
            _nextAttackTime = 0f;
        }

        public override void PerformAction() {
            // 아직 공격 시간이 아니면 리턴
            if (Time.time < _nextAttackTime) return;

            var abilityModule = StateMachine?.GetAbilityModule();
            if (abilityModule == null) return;

            // MeleeAttackAbility 가져오기
            var attackAbility = abilityModule.GetAbility<YisoMeleeAttackAbility>();
            if (attackAbility == null) {
                Debug.LogWarning("[AttackAction] YisoMeleeAttackAbility를 찾을 수 없습니다.");
                return;
            }

            // 공격 트리거
            attackAbility.TriggerAttack();

            // 다음 공격 시간 업데이트
            _nextAttackTime = Time.time + attackInterval;
        }
    }
}