using Gameplay.Character.Abilities.Definitions;
using Gameplay.Character.Core.Modules;
using Gameplay.Character.Data;
using Gameplay.Character.StateMachine;
using Gameplay.Character.Types;
using UnityEngine;

namespace Gameplay.Character.Abilities {
    /// <summary>
    /// 근접 무기를 사용한 공격 Ability.
    /// WeaponModule의 DamageOnTouch를 On/Off하여 공격을 제어하고, 콤보 시스템을 관리.
    /// AnimationModule을 사용하여 Enum 기반 애니메이션 제어를 수행.
    /// 완전한 애니메이션 이벤트 기반으로 동작 (코루틴 미사용).
    /// Safety Net 타이머를 통해 애니메이션 이벤트가 씹혀도 강제 종료됩니다.
    /// </summary>
    public class YisoMeleeAttackAbility : YisoCharacterAbilityBase {
        #region Animation Event Constants (Magic String 방지)

        private const string EVENT_ENABLE_DAMAGE = "EnableDamage";
        private const string EVENT_DISABLE_DAMAGE = "DisableDamage";
        private const string EVENT_ATTACK_END = "AttackEnd";

        #endregion

        private readonly YisoMeleeAttackAbilitySO _settings;

        private YisoCharacterWeaponModule _weaponModule;
        private YisoCharacterInputModule _inputModule; // Player 전용

        private bool _isAttacking = false;
        private float _lastAttackTime = -999f;

        // Input Edge Detection (단발 입력 감지용)
        private bool _wasAttackPressedLastFrame = false;

        // Combo System
        private int _currentCombo = 0; // 현재 콤보 (0부터 시작)
        private float _comboResetTimer = 0f; // 콤보 리셋 타이머

        // Safety Net: 애니메이션 이벤트가 씹혀도 강제 종료하는 안전장치
        private float _safetyTimer = 0f;

        public YisoMeleeAttackAbility(YisoMeleeAttackAbilitySO settings) {
            _settings = settings;
        }

        public override void Initialize(Core.IYisoCharacterContext context) {
            base.Initialize(context);
            _weaponModule = Context.GetModule<YisoCharacterWeaponModule>();
            _inputModule = Context.GetModule<YisoCharacterInputModule>();

            if (_weaponModule == null) {
                Debug.LogWarning("[YisoMeleeAttackAbility] YisoCharacterWeaponModule을 찾을 수 없습니다. 이 Ability는 작동하지 않습니다.");
            }

            if (_animationModule == null) {
                Debug.LogWarning("[YisoMeleeAttackAbility] YisoCharacterAnimationModule을 찾을 수 없습니다. 애니메이션이 작동하지 않습니다.");
            }
        }

        public override void PreProcessAbility() {
            base.PreProcessAbility();
            
            // 공격 중이면 입력 무시
            if (_isAttacking) return;

            // 무기가 없으면 공격 불가
            if (_weaponModule == null || !_weaponModule.HasWeapon()) return;

            // 공격 입력 확인 (Player만)
            if (_inputModule != null && Context.Type == CharacterType.Player) {
                // Pull 방식: InputModule에서 공격 버튼 상태를 가져옴
                var isPressed = _inputModule.AttackInput;

                // 단발/연속 입력 모드 분기
                var shouldAttack = false;

                if (_settings.continuousPressAttack) {
                    // 연속 입력 모드: 버튼을 누르고 있는 동안 계속 공격
                    shouldAttack = isPressed;
                }
                else {
                    // 단발 입력 모드: Edge Detection (버튼을 누르는 순간만 공격)
                    // 이전 프레임에 false였고, 현재 프레임에 true이면 공격
                    shouldAttack = !_wasAttackPressedLastFrame && isPressed;
                }

                // Edge Detection 상태 업데이트
                _wasAttackPressedLastFrame = isPressed;

                // 공격 실행
                if (shouldAttack) {
                    TryAttack();
                }
            }
            // AI의 경우 외부에서 TriggerAttack() 호출
        }

        public override void ProcessAbility() {
            base.ProcessAbility();

            // Safety Net: 애니메이션 이벤트가 호출되지 않는 경우 강제 종료
            if (_isAttacking && _safetyTimer > 0f) {
                _safetyTimer -= Time.deltaTime;

                if (_safetyTimer <= 0f) {
                    Debug.LogWarning($"[YisoMeleeAttackAbility] Safety Net 타이머 만료! " +
                                     $"애니메이션 이벤트 '{EVENT_ATTACK_END}'가 호출되지 않아 강제 종료합니다. " +
                                     $"애니메이션 클립에 이벤트가 제대로 설정되었는지 확인하세요.");

                    // 강제 정리
                    HandleDisableDamage(); // 혹시 모를 DamageOnTouch 비활성화
                    HandleAttackEnd();
                }
            }

            // 콤보 리셋 타이머 업데이트 (콤보 사용 시에만)
            if (_settings.useComboAttacks && !_isAttacking && _comboResetTimer > 0f) {
                _comboResetTimer -= Time.deltaTime;

                // 타이머 만료 시 콤보 리셋
                if (_comboResetTimer <= 0f) {
                    ResetCombo();
                }
            }
        }
        
        public override void UpdateAnimator() {
            base.UpdateAnimator();

            if (_animationModule != null) {
                // IsAttacking 파라미터
                _animationModule.SetBool(YisoCharacterAnimationState.IsAttacking, _isAttacking);

                // Combo 파라미터
                // useComboAttacks가 false면: 0으로 고정 (기본 공격 애니메이션)
                // useComboAttacks가 true면: _currentCombo + 1 (1, 2, 3, ... 콤보 애니메이션)
                var comboValue = _settings.useComboAttacks ? _currentCombo + 1 : 0;
                _animationModule.SetInteger(YisoCharacterAnimationState.Combo, comboValue);

                // AttackSpeed 파라미터
                _animationModule.SetFloat(YisoCharacterAnimationState.AttackSpeed, 1f);
            }
        }

        /// <summary>
        /// 애니메이션 이벤트를 처리합니다.
        /// Animator의 Animation Event에서 호출됩니다.
        /// </summary>
        /// <param name="eventName">이벤트 이름 (const string 상수와 비교)</param>
        public override void OnAnimationEvent(string eventName) {
            base.OnAnimationEvent(eventName);

            // 공격 중이 아니면 이벤트 무시 (다른 어빌리티의 이벤트일 수 있음)
            if (!_isAttacking) return;

            switch (eventName) {
                case EVENT_ENABLE_DAMAGE:
                    HandleEnableDamage();
                    break;

                case EVENT_DISABLE_DAMAGE:
                    HandleDisableDamage();
                    break;

                case EVENT_ATTACK_END:
                    HandleAttackEnd();
                    break;

                default:
                    // 알 수 없는 이벤트 무시 (다른 어빌리티용일 수 있음)
                    break;
            }
        }

        #region Public API

        /// <summary>
        /// 외부에서 공격을 트리거합니다. (AI 등에서 사용)
        /// </summary>
        public void TriggerAttack() {
            TryAttack();
        }

        /// <summary>
        /// 현재 공격 중인지 여부를 반환합니다.
        /// </summary>
        public bool IsAttacking() {
            return _isAttacking;
        }

        #endregion

        #region Private Logic

        /// <summary>
        /// 공격을 시도합니다.
        /// </summary>
        private void TryAttack() {
            // 무기가 없으면 공격 불가
            if (_weaponModule == null || !_weaponModule.HasWeapon()) {
                return;
            }

            var weaponData = _weaponModule.GetCurrentWeaponData();
            if (weaponData == null) return;

            // 쿨타임 체크
            var cooldown = weaponData.GetAttackCooldown();
            if (Time.time - _lastAttackTime < cooldown) {
                return;
            }

            // [핵심] FSM에 Attack 상태 전이 요청
            // StateModule이 Transition 검증 후 승인/거부 결정
            Context.RequestStateChangeByRole(YisoStateRole.Attack);

            // 콤보 업데이트 (콤보 사용 시에만)
            if (_settings.useComboAttacks) {
                UpdateCombo(weaponData);
            }
            else {
                // 콤보를 사용하지 않으면 항상 0으로 유지
                _currentCombo = 0;
                _weaponModule.SetComboIndex(0);
            }

            // 공격 시작
            AttackStart(weaponData);
        }

        /// <summary>
        /// 콤보를 업데이트합니다.
        /// </summary>
        private void UpdateCombo(YisoWeaponDataSO weaponData) {
            // 콤보 리셋 타임 체크
            if (_comboResetTimer <= 0f) {
                // 타이머 만료 -> 콤보 리셋
                _currentCombo = 0;
            }
            else {
                // 콤보 증가
                _currentCombo++;

                // 최대 콤보 초과 시 리셋
                if (_currentCombo >= weaponData.maxComboCount) {
                    _currentCombo = 0;
                }
            }

            // 콤보 리셋 타이머 초기화
            _comboResetTimer = weaponData.comboResetTime;

            // WeaponModule에 콤보 인덱스 설정
            _weaponModule.SetComboIndex(_currentCombo);
        }

        /// <summary>
        /// 콤보를 리셋합니다.
        /// </summary>
        private void ResetCombo() {
            _currentCombo = 0;
            _comboResetTimer = 0f;
            _weaponModule?.SetComboIndex(_currentCombo);
        }

        /// <summary>
        /// 공격을 시작합니다.
        /// Safety Net 타이머를 설정하여 애니메이션 이벤트가 씹혀도 강제 종료되도록 합니다.
        /// </summary>
        private void AttackStart(YisoWeaponDataSO weaponData) {
            _isAttacking = true;
            _lastAttackTime = Time.time;

            // Safety Net: 애니메이션 duration + 여유 시간(0.1초)
            // 이 시간이 지나면 AttackEnd 이벤트가 없어도 강제 종료
            var duration = weaponData.attackDuration;
            _safetyTimer = duration + 0.1f;

            // 애니메이션 이벤트가 DamageOnTouch를 제어하므로 여기서는 상태만 설정
            // EnableDamage, DisableDamage, AttackEnd는 애니메이션 이벤트에서 호출됨
        }

        #endregion

        #region Animation Event Handlers

        /// <summary>
        /// 애니메이션 이벤트: 데미지 활성화
        /// </summary>
        private void HandleEnableDamage() {
            if (_weaponModule == null) {
                Debug.LogWarning("[YisoMeleeAttackAbility] WeaponModule이 null입니다. DamageOnTouch를 활성화할 수 없습니다.");
                return;
            }

            _weaponModule.EnableWeaponDamage();
        }

        /// <summary>
        /// 애니메이션 이벤트: 데미지 비활성화
        /// </summary>
        private void HandleDisableDamage() {
            _weaponModule?.DisableWeaponDamage();
        }

        /// <summary>
        /// 애니메이션 이벤트: 공격 종료
        /// </summary>
        private void HandleAttackEnd() {
            _isAttacking = false;
            _safetyTimer = 0f; // Safety Net 타이머 리셋
            
            Context.RequestStateChangeByRole(YisoStateRole.Idle);
        }

        #endregion
    }
}
