using System.Collections;
using Gameplay.Character.Abilities.Definitions;
using Gameplay.Character.Core.Modules;
using UnityEngine;

namespace Gameplay.Character.Abilities {
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
    }
}