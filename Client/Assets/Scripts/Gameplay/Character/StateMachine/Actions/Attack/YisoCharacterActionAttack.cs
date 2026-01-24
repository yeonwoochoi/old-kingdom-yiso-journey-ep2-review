using Gameplay.Character.Abilities;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils;

namespace Gameplay.Character.StateMachine.Actions.Attack {
    /// <summary>
    /// AI가 공격을 실행하도록 트리거하는 액션입니다.
    /// YisoMeleeAttackAbility의 TriggerAttack()을 호출합니다.
    ///
    /// loopAttack = true: 상태에 머무는 동안 일정 간격으로 반복 공격
    /// loopAttack = false: 한 번 공격 후 대기 (Decision에서 공격 완료 확인 후 전이)
    /// </summary>
    public class YisoCharacterActionAttack: YisoCharacterAction {
        [Title("Attack Settings")]
        [Tooltip("true: 반복 공격 / false: 단일 공격 후 대기")]
        [SerializeField] private bool loopAttack = true;

        // 랜덤 인터벌 사용 여부 토글
        [SerializeField, ShowIf("loopAttack")]
        [LabelText("Use Random Interval")]
        [Indent]
        private bool isRandomInterval = false;

        // 고정 시간 (랜덤이 아닐 때만 표시)
        [Tooltip("반복 공격 시 공격 간 대기 시간")]
        [SerializeField, ShowIf("@loopAttack && !isRandomInterval"), Min(0f)]
        [Indent]
        private float attackLoopInterval = 0.5f;

        // 랜덤 최소 시간 (랜덤일 때만 표시)
        [SerializeField, ShowIf("@loopAttack && isRandomInterval"), Min(0f)]
        [Indent]
        private float minAttackInterval = 0.5f;

        // 랜덤 최대 시간 (랜덤일 때만 표시)
        [SerializeField, ShowIf("@loopAttack && isRandomInterval"), Min(0f)]
        [Indent]
        private float maxAttackInterval = 1.5f;

        private YisoMeleeAttackAbility _attackAbility;
        private float _nextAttackTime = 0f;
        private bool _hasAttacked = false; // 단일 공격 모드에서 공격 완료 여부

        /// <summary>
        /// 현재 공격이 진행 중인지 여부를 반환합니다.
        /// Decision에서 공격 완료를 확인할 때 사용합니다.
        /// </summary>
        public bool IsAttacking => _attackAbility != null && _attackAbility.IsAttacking();

        /// <summary>
        /// 단일 공격 모드에서 공격이 완료되었는지 여부를 반환합니다.
        /// (공격을 시도했고, 현재 공격 중이 아님)
        /// </summary>
        public bool IsAttackFinished => !loopAttack && _hasAttacked && !IsAttacking;

        public override void OnEnterState() {
            base.OnEnterState();

            // Ability 캐싱
            var abilityModule = StateMachine?.GetAbilityModule();
            _attackAbility = abilityModule?.GetAbility<YisoMeleeAttackAbility>();

            if (_attackAbility == null) {
                YisoLogger.LogWarning("YisoMeleeAttackAbility를 찾을 수 없습니다.");
            }

            // 상태 초기화
            _nextAttackTime = 0f;
            _hasAttacked = false;
        }

        public override void PerformAction() {
            if (_attackAbility == null) return;

            // 단일 공격 모드: 이미 공격했으면 대기
            if (!loopAttack && _hasAttacked) {
                // 공격 완료 대기 중 (Decision에서 IsAttackFinished로 확인)
                return;
            }

            // 현재 공격 중이면 대기 (애니메이션 완료 대기)
            if (_attackAbility.IsAttacking()) return;

            // 반복 공격 모드: 인터벌 체크
            if (loopAttack && Time.time < _nextAttackTime) return;

            // 공격 트리거
            _attackAbility.TriggerAttack();
            _hasAttacked = true;

            // 다음 공격 시간 업데이트 (반복 모드일 때만 의미있음)
            if (loopAttack) {
                float waitTime;
                
                // 랜덤 여부에 따라 대기 시간 계산
                if (isRandomInterval) {
                    // Min이 Max보다 클 경우 안전하게 처리 (자동 스왑은 안 되므로 Min 기준)
                    var min = minAttackInterval;
                    var max = Mathf.Max(minAttackInterval, maxAttackInterval);
                    waitTime = Random.Range(min, max);
                } else {
                    waitTime = attackLoopInterval;
                }

                _nextAttackTime = Time.time + waitTime;
            }
        }

        public override void OnExitState() {
            base.OnExitState();
            _hasAttacked = false;
        }
    }
}