using Gameplay.Character.Abilities.Definitions;
using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using UnityEngine;

namespace Gameplay.Character.Abilities {
    /// <summary>
    /// Pull 방식 이동 시스템: Ability가 능동적으로 입력 데이터를 조회하여 이동 로직을 수행합니다.
    /// - Player: InputModule.MoveInput을 Context.MovementVector를 통해 조회
    /// - AI: AIModule.PathDirection을 Context.MovementVector를 통해 조회
    /// </summary>
    public class YisoMovementAbility: YisoCharacterAbilityBase {
        private readonly YisoMovementAbilitySO _settings;

        private Vector2 _currentInput;
        private Vector2 _lerpedInput;
        private float _currentAcceleration;

        private float _speedMultiplier = 1f;
        private float _multiplierEndTime = -1f;

        private YisoCharacterInputModule _inputModule;

        public Vector2 FinalMovementInput { get; private set; }

        public YisoMovementAbility(YisoMovementAbilitySO settings) {
            _settings = settings;
        }

        public override void Initialize(IYisoCharacterContext context) {
            base.Initialize(context);
            _inputModule = context.GetModule<YisoCharacterInputModule>();
        }

        public override void PreProcessAbility() {
            base.PreProcessAbility();
            _currentInput = _inputModule?.MoveInput ?? Vector2.zero;
        }

        public override void ProcessAbility() {
            base.ProcessAbility();

            // 임시 속도 배율 타이머 체크
            if (_multiplierEndTime > 0f && Time.time >= _multiplierEndTime) {
                _speedMultiplier = 1f;
                _multiplierEndTime = -1f;
            }
            
            if (!Context.IsMovementAllowed) {
                StopMovement();
                return;
            }

            CalculateInterpolatedInput();
            FinalMovementInput = _lerpedInput;

            var characterMoveSpeed = _settings.baseMovementSpeed;
            var finalMovementVector = FinalMovementInput * (characterMoveSpeed * _speedMultiplier);

            Context.Move(finalMovementVector);
        }

        public override void UpdateAnimator() {
            // ========== Animator Parameter Architecture ==========
            // [Continuous Values] - Ability에서 매 프레임 업데이트
            // - MoveSpeed: 이동 속도 (연속 값)
            //
            // [State Flags] - FSM Action에서 상태 전환 시 설정
            // - IsMoving: FSM Enter_Move/Exit_Move Action에서 제어
            //   (Ability에서는 설정하지 않음)
            // =====================================================

            var moveSpeed = _lerpedInput.magnitude * _speedMultiplier;
            Context.PlayAnimation(YisoCharacterAnimationState.MoveSpeed, moveSpeed);
        }

        #region Public API

        public void ApplyTemporarySpeedMultiplier(float multiplier, float duration) {
            _speedMultiplier = multiplier;
            _multiplierEndTime = Time.time + duration;
        }

        #endregion

        #region Private Logic

        private void CalculateInterpolatedInput() {
            if (_currentInput.sqrMagnitude > _settings.idleThreshold * _settings.idleThreshold) {
                // 방향 전환 감지: 현재 입력과 이전 입력의 내적이 음수면 반대 방향
                var directionChanged = false;
                if (_lerpedInput.sqrMagnitude > 0.01f) {
                    var dot = Vector2.Dot(_currentInput.normalized, _lerpedInput.normalized);
                    if (dot < 0f) { // 반대 방향 (180도 이상)
                        directionChanged = true;
                    }
                }

                // 방향이 크게 바뀌면 가속도 리셋 (즉시 방향 전환)
                if (directionChanged) {
                    _currentAcceleration = 0f;
                    _lerpedInput = Vector2.zero;
                }

                _currentAcceleration = Mathf.Lerp(_currentAcceleration, 1f, _settings.acceleration * Time.deltaTime);
                _lerpedInput = _settings.useAnalogInput
                    ? Vector2.ClampMagnitude(_currentInput, _currentAcceleration)
                    : Vector2.ClampMagnitude(_currentInput.normalized, _currentAcceleration);
            }
            else {
                _currentAcceleration = Mathf.Lerp(_currentAcceleration, 0f, _settings.deceleration * Time.deltaTime);
                _lerpedInput = _lerpedInput.normalized * _currentAcceleration;
            }
        }

        #endregion
        
        /// <summary>
        /// 운동량만 멈추는 함수 (버프는 유지)
        /// </summary>
        private void StopMovement() {
            _currentAcceleration = 0f;
            _lerpedInput = Vector2.zero;
            Context.Move(Vector2.zero);
        }

        /// <summary>
        /// 버프 (이속 증가)까지 리셋
        /// </summary>
        public override void ResetAbility() {
            base.ResetAbility();
    
            StopMovement();
            
            _currentInput = Vector2.zero;
            _speedMultiplier = 1f;
            _multiplierEndTime = -1f;
        }

        public override void OnDeath() {
            base.OnDeath();

            // 사망 시 이동 중지 및 속도 배율 리셋
            ResetAbility();
        }

        public override void OnRevive() {
            base.OnRevive();

            // 부활 시 모든 상태 초기화
            ResetAbility();
        }
    }
}