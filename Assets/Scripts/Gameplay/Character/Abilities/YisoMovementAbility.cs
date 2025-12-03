using System.Collections;
using Gameplay.Character.Abilities.Definitions;
using Gameplay.Character.Core.Modules;
using Gameplay.Character.StateMachine;
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
        private Coroutine _temporaryMultiplierCoroutine;

        public Vector2 FinalMovementInput { get; private set; }

        public YisoMovementAbility(YisoMovementAbilitySO settings) {
            _settings = settings;
        }

        public override void PreProcessAbility() {
            base.PreProcessAbility();
            // Pull 방식: Ability가 능동적으로 입력 데이터를 조회
            // Context.MovementVector는 Player일 경우 InputModule.MoveInput,
            // AI일 경우 AIModule.PathDirection을 반환
            _currentInput = Context.MovementVector;
        }

        public override void ProcessAbility() {
            base.ProcessAbility();
            
            CalculateInterpolatedInput();
            
            FinalMovementInput = _lerpedInput;

            var movementIsPermitted = Context.GetCurrentState()?.CanMove ?? false;
            if (!movementIsPermitted) {
                Context.Move(Vector2.zero);
                return;
            }

            var characterMoveSpeed = _settings.baseMovementSpeed;
            var finalMovementVector = FinalMovementInput * (characterMoveSpeed * _speedMultiplier);

            Context.Move(finalMovementVector);

            // 플레이어만 입력 기반으로 상태를 자동 전환합니다.
            // AI는 FSM의 Transition 규칙에 따라 스스로 상태를 바꿉니다.
            if (Context.IsPlayer) {
                // [핵심] FSM 상태 동기화 요청
                var currentState = Context.GetCurrentState();
                if (currentState != null) {
                    var isMoving = FinalMovementInput.sqrMagnitude > 0.01f;

                    // Idle -> Move: 이동 입력이 있고 현재 Idle 상태일 때
                    if (isMoving && currentState.Role == YisoStateRole.Idle) {
                        Context.RequestStateChangeByRole(YisoStateRole.Move);
                    }
                    // Move -> Idle: 이동 입력이 없고 현재 Move 상태일 때
                    else if (!isMoving && currentState.Role == YisoStateRole.Move) {
                        Context.RequestStateChangeByRole(YisoStateRole.Idle);
                    }
                }
            }
        }

        public override void UpdateAnimator() {
            var moveSpeed = _lerpedInput.magnitude * _speedMultiplier;
            Context.PlayAnimation(YisoCharacterAnimationState.MoveSpeed, moveSpeed);
            
            // IsMoving과 IsIdle 파라미터는 StateModule이나 StateSO의 Action에서 처리하는 것이 더 적합할 것 같음.
        }

        #region Public API

        public void ApplyTemporarySpeedMultiplier(float multiplier, float duration) {
            if (_temporaryMultiplierCoroutine != null) {
                Context.StopCoroutine(_temporaryMultiplierCoroutine);
            }
            _temporaryMultiplierCoroutine = Context.StartCoroutine(ApplyTemporarySpeedMultiplierCoroutine(multiplier, duration));
        }

        #endregion

        #region Private Logic

        private void CalculateInterpolatedInput(bool forceDecelerate = false) {
            if (_currentInput.sqrMagnitude > _settings.idleThreshold * _settings.idleThreshold && !forceDecelerate) {
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

        private IEnumerator ApplyTemporarySpeedMultiplierCoroutine(float multiplier, float duration) {
            _speedMultiplier = multiplier;
            yield return new WaitForSeconds(duration);
            _speedMultiplier = 1f;
            _temporaryMultiplierCoroutine = null;
        }

        #endregion

        public override void OnDeath() {
            base.OnDeath();
            Context.Move(Vector2.zero);
        }
    }
}