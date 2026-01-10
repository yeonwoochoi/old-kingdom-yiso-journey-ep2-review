using Gameplay.Character.Abilities.Definitions;
using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using UnityEngine;
using Utils;

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
        public bool IsMoving => FinalMovementInput.magnitude > MovementThreshold;

        private const float MovementThreshold = 0.01f;

        public YisoMovementAbility(YisoMovementAbilitySO settings) {
            _settings = settings;
        }

        public override void Initialize(IYisoCharacterContext context) {
            base.Initialize(context);

            // Player일 경우에만 InputModule 캐싱 (AI는 null이어도 무방)
            if (Context.IsPlayer) {
                _inputModule = context.GetModule<YisoCharacterInputModule>();
                YisoLogger.Log($"MovementAbility 초기화: Player 모드, InputModule={(_inputModule != null ? "찾음" : "없음")}");
            } else {
                YisoLogger.Log("MovementAbility 초기화: AI 모드");
            }
        }

        /// <summary>
        /// [AI 전용] FSM Action에서 이동하고자 하는 방향을 주입할 때 사용
        /// </summary>
        public void SetMovementInput(Vector2 direction, bool force = false)
        {
            if (Context.IsPlayer && !force)
            {
                YisoLogger.LogWarning("Player의 경우 InputSystem을 통해서 Movement Input을 넣어야 합니다.");
                return;
            }
            _currentInput = direction;
        }

        public override void PreProcessAbility() {
            base.PreProcessAbility();
            if (Context.IsPlayer)
                _currentInput = _inputModule?.MoveInput ?? Vector2.zero;
            // AI라면 FSM Action이 SetMovementInput을 호출했을 것이므로 _currentInput에 값이 이미 있음.
        }

        public override void ProcessAbility() {
            base.ProcessAbility();
            // 임시 속도 배율 타이머 체크
            if (_multiplierEndTime > 0f && Time.time >= _multiplierEndTime)
            {
                _speedMultiplier = 1f;
                _multiplierEndTime = -1f;
            }

            if (!Context.IsMovementAllowed(this))
            {
                StopMovement();
                return;
            }

            CalculateInterpolatedInput();

            // TODO: 추후 StatModule에서 BaseMoveSpeed를 가져오도록 변경 가능
            var characterBaseSpeed = _settings.baseMovementSpeed;

            // 최종 벡터 = (가속된 방향) * (기본 속도) * (배율)
            FinalMovementInput = _lerpedInput * (characterBaseSpeed * _speedMultiplier);

            Context.Move(FinalMovementInput);
        }

        public override void PostProcessAbility()
        {
            base.PostProcessAbility();

            // AI의 경우, FSM이 매 프레임 입력을 넣지 않을 수도 있으므로 프레임 끝날 때 입력을 초기화해야 "미끄러짐" 방지 가능.
            // (Player는 매 프레임 InputModule을 읽으므로 상관없음)
            if (!Context.IsPlayer)
            {
                _currentInput = Vector2.zero;
            }
        }

        public override void UpdateAnimator() {
            var moveSpeed = _lerpedInput.magnitude * _speedMultiplier;
            Context.PlayAnimation(YisoCharacterAnimationState.MoveSpeed, moveSpeed);
            Context.PlayAnimation(YisoCharacterAnimationState.IsMoving, IsMoving);
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
            YisoLogger.Log("이동 정지: IsMovementAllowed=false");
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