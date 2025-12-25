using System.Collections;
using Gameplay.Character.Abilities.Definitions;
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

            if (!Context.IsMovementAllowed) {
                Context.Move(Vector2.zero);
                return; 
            }

            var characterMoveSpeed = _settings.baseMovementSpeed;
            var finalMovementVector = FinalMovementInput * (characterMoveSpeed * _speedMultiplier);

            // 디버그: AI의 경우 이동 벡터 확인
            if (!Context.IsPlayer) {
                Debug.Log($"[MovementAbility] _currentInput: {_currentInput}, _lerpedInput: {_lerpedInput}, finalMovementVector: {finalMovementVector}");
            }

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
            if (_temporaryMultiplierCoroutine != null) {
                Context.StopCoroutine(_temporaryMultiplierCoroutine);
            }
            _temporaryMultiplierCoroutine = Context.StartCoroutine(ApplyTemporarySpeedMultiplierCoroutine(multiplier, duration));
        }

        #endregion

        #region Private Logic

        private void CalculateInterpolatedInput(bool forceDecelerate = false) {
            if (_currentInput.sqrMagnitude > _settings.idleThreshold * _settings.idleThreshold && !forceDecelerate) {
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