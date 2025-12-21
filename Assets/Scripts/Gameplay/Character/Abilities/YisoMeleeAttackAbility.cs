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
        private YisoOrientationAbility _orientationAbility; // 방향 제어

        private bool _isAttacking = false;
        private float _lastAttackTime = -999f;

        // Input Edge Detection (단발 입력 감지용)
        private bool _wasAttackPressedLastFrame = false;

        // Attack Queue (선입력 버퍼)
        private bool _nextAttackQueued = false;

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

        public override void LateInitialize() {
            base.LateInitialize();

            // OrientationAbility 참조 가져오기
            var abilityModule = Context.GetModule<YisoCharacterAbilityModule>();
            if (abilityModule != null) {
                _orientationAbility = abilityModule.GetAbility<YisoOrientationAbility>();
            }

            if (_orientationAbility == null) {
                Debug.LogWarning("[YisoMeleeAttackAbility] YisoOrientationAbility를 찾을 수 없습니다. 공격 중 방향 잠금이 작동하지 않습니다.");
            }
        }

        public override void PreProcessAbility() {
            base.PreProcessAbility();

            // 공격 중이면 선입력 버퍼만 처리하고 리턴
            if (_isAttacking) {
                // 공격 중 추가 입력 감지 (선입력 버퍼)
                if (_inputModule != null && Context.Type == CharacterType.Player) {
                    var isPressed = _inputModule.AttackInput;

                    // 단발 모드일 때만 선입력 저장 (연속 모드는 HandleAttackEnd에서 자동 판단)
                    if (!_settings.continuousPressAttack) {
                        // Edge Detection: 버튼을 누르는 순간만 큐에 저장
                        if (!_wasAttackPressedLastFrame && isPressed) {
                            _nextAttackQueued = true;
                        }
                    }

                    // Edge Detection 상태 업데이트
                    _wasAttackPressedLastFrame = isPressed;
                }
                return;
            }

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
                // ========== Animator Parameter Architecture ==========
                // [Continuous Values] - Ability에서 매 프레임 업데이트
                // - Combo: Enemy도 사용하는 공통 로직
                // - AttackSpeed: 무기별 공격 속도 (연속 값)
                //
                // [State Flags] - FSM Action에서 상태 전환 시 설정
                // - IsAttacking: FSM Enter_Attack/Exit_Attack Action에서 제어
                //   (Ability에서는 설정하지 않음)
                // =====================================================

                // Combo 파라미터 (Player/Enemy 공통)
                // useComboAttacks가 false면: 0으로 고정 (기본 공격 애니메이션)
                // useComboAttacks가 true면: _currentCombo + 1 (1, 2, 3, ... 콤보 애니메이션)
                var comboValue = _settings.useComboAttacks ? _currentCombo + 1 : 0;
                _animationModule.SetInteger(YisoCharacterAnimationState.Combo, comboValue);

                // AttackSpeed 파라미터 (Continuous value)
                // Note: WeaponDataSO의 attackRate는 내부 시스템용 값 (x2 배수)
                // Animator AttackSpeed는 1.0 = 정상 속도이므로, attackRate를 0.5배하여 설정
                // 예: attackRate = 2.0 → AttackSpeed = 1.0 (정상 속도)
                var attackSpeed = _weaponModule.GetCurrentWeaponData().attackRate * 0.5f;
                _animationModule.SetFloat(YisoCharacterAnimationState.AttackSpeed, attackSpeed);
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
        /// <returns>공격이 성공적으로 시작되었으면 true, 실패하면 false</returns>
        private bool TryAttack() {
            // 무기가 없으면 공격 불가
            if (_weaponModule == null || !_weaponModule.HasWeapon()) {
                return false;
            }

            var weaponData = _weaponModule.GetCurrentWeaponData();
            if (weaponData == null) return false;

            // 쿨타임 체크
            var cooldown = weaponData.GetAttackCooldown();
            if (Time.time - _lastAttackTime < cooldown) {
                return false;
            }

            // [핵심] FSM에 Attack 상태 전이 요청
            // Player만 State 전환 요청 (AI는 FSM이 이미 전환함)
            // 또한 이미 Attack 상태라면 재전환 방지 (콤보 연계 시)
            var currentState = Context.GetCurrentState();
            if (currentState?.Role != YisoStateRole.Attack) {
                Context.RequestStateChangeByRole(YisoStateRole.Attack);
            }

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
            return true;
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

            // 선입력 큐 초기화 (새로운 공격이 시작되었으므로 이전 큐는 무효화)
            _nextAttackQueued = false;

            // Safety Net: 애니메이션 duration + 여유 시간(0.1초)
            // 이 시간이 지나면 AttackEnd 이벤트가 없어도 강제 종료
            var duration = weaponData.attackDuration;
            _safetyTimer = duration + 0.1f;

            // ========== 공격 방향 고정 ==========
            // 1. Orientation 잠금: 공격 중에는 캐릭터 방향이 변경되지 않도록
            if (_orientationAbility != null) {
                _orientationAbility.LockOrientation();
            }

            // 2. WeaponAim 방향 고정: 공격 시작 시점의 방향으로 고정
            var currentWeapon = _weaponModule?.CurrentWeapon;
            if (currentWeapon != null && currentWeapon.WeaponAim != null) {
                // 공격 시작 시점의 방향 (현재 Orientation의 LastDirectionVector 사용)
                var attackDirection = _orientationAbility?.LastDirectionVector ?? Vector2.down;
                currentWeapon.WeaponAim.LockAimToDirection(attackDirection);
            }

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
            _safetyTimer = 0f; // Safety Net 타이머 리셋

            // 1. Orientation 잠금 해제 (콤보 계속 시 다음 공격에서 다시 잠금)
            _orientationAbility?.UnlockOrientation();

            // 2. WeaponAim 잠금 해제
            var currentWeapon = _weaponModule?.CurrentWeapon;
            if (currentWeapon != null && currentWeapon.WeaponAim != null) {
                currentWeapon.WeaponAim.UnlockAim();
            }

            // 3. 콤보 연계 판단: 연속 입력 또는 선입력 큐 확인
            var shouldContinueCombo = false;

            if (_inputModule != null && Context.Type == CharacterType.Player) {
                if (_settings.continuousPressAttack) {
                    // 연속 입력 모드: 버튼이 계속 눌려있으면 콤보 계속
                    shouldContinueCombo = _inputModule.AttackInput;
                }
                else {
                    // 단발 입력 모드: 선입력 큐가 있으면 콤보 계속
                    shouldContinueCombo = _nextAttackQueued;
                }
            }

            // 4. 콤보 계속 또는 종료
            _isAttacking = false;

            if (shouldContinueCombo) {
                // 콤보 계속: 다음 공격 시도
                var attackStarted = TryAttack();

                // 공격이 실패하면 (쿨타임, 무기 없음 등) Idle로 전환
                // Player만 State 전환 요청 (AI는 FSM Transition이 처리)
                if (!attackStarted && Context.IsPlayer) {
                    Context.RequestStateChangeByRole(YisoStateRole.Idle);
                }
                // 공격이 성공하면 Attack 상태 유지 (TryAttack에서 이미 RequestStateChange 호출)
            }
            else {
                // 콤보 종료: Idle로 전환
                // Player만 State 전환 요청 (AI는 FSM Transition이 처리)
                if (Context.IsPlayer) {
                    Context.RequestStateChangeByRole(YisoStateRole.Idle);
                }
            }
        }

        #endregion

        public override void ResetAbility() {
            base.ResetAbility();

            // 1. 공격 상태 강제 중단
            _isAttacking = false;
            _safetyTimer = 0f;

            // 2. 입력 상태 리셋
            _wasAttackPressedLastFrame = false;
            _nextAttackQueued = false;

            // 3. 콤보 리셋
            ResetCombo();

            // 4. 무기 데미지 비활성화
            _weaponModule?.DisableWeaponDamage();

            // 5. Orientation & WeaponAim 잠금 해제
            _orientationAbility?.UnlockOrientation();

            var currentWeapon = _weaponModule?.CurrentWeapon;
            if (currentWeapon != null && currentWeapon.WeaponAim != null) {
                currentWeapon.WeaponAim.UnlockAim();
            }
        }

        public override void OnDeath() {
            base.OnDeath();

            // 공격 중이었다면 강제 중단
            if (_isAttacking) {
                // 데미지 비활성화
                HandleDisableDamage();

                // 공격 상태 정리 (FSM 전환 없이)
                _isAttacking = false;
                _safetyTimer = 0f;

                // Orientation & WeaponAim 잠금 해제
                _orientationAbility?.UnlockOrientation();

                var currentWeapon = _weaponModule?.CurrentWeapon;
                if (currentWeapon != null && currentWeapon.WeaponAim != null) {
                    currentWeapon.WeaponAim.UnlockAim();
                }
            }
        }

        public override void OnRevive() {
            base.OnRevive();

            // ResetAbility와 동일하지만, 부활 시 특별히 처리할 사항이 있다면 여기에 추가
            // 현재는 ResetAbility를 호출하여 모든 상태 초기화
            ResetAbility();
        }
    }
}
