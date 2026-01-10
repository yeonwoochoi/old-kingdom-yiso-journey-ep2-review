using Gameplay.Character.Abilities.Definitions;
using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using Gameplay.Character.Types;
using Gameplay.Character.Weapon;
using UnityEngine;
using Utils;

namespace Gameplay.Character.Abilities {
    /// <summary>
    /// 캐릭터의 방향(Orientation)을 결정하는 유일한 책임자입니다.
    /// 이동, 조준 등 여러 입력 소스를 종합하여 최종 방향 벡터를 결정하고 외부에 제공합니다.
    /// </summary>
    public class YisoOrientationAbility: YisoCharacterAbilityBase {
        private readonly YisoOrientationAbilitySO _settings;

        private YisoMovementAbility _movementAbility;
        private YisoCharacterWeaponModule _weaponModule;
        private YisoWeaponAim _weaponAim;

        /// <summary>
        /// 방향 전환 잠금 플래그.
        /// true일 경우 입력이 들어와도 방향이 변경되지 않습니다 (공격 중 등).
        /// </summary>
        private bool _isOrientationLocked = false;
        
        // [핵심] 부모(Base)의 로직을 무시하고, 사망 상태가 아니라면 항상 실행되도록 변경
        public override bool IsAbilityEnabled {
            get {
                // 1. 캐릭터가 죽었으면 방향 전환도 멈춰야 함 (선택 사항)
                if (Context.IsDead()) return false; 
            
                // 2. State의 CanCastAbility 여부와 상관없이 무조건 실행 (Always Run)
                return true; 
            }
        }
        
        #region Public Properties (외부 제공 정보)
        
        /// <summary>
        /// 캐릭터가 현재 바라보고 있는 방향 상태입니다.
        /// </summary>
        public FacingDirections CurrentFacingDirection { get; private set; }
        public Vector2 CurrentFacingDirectionVector { get; private set; }
        
        /// <summary>
        /// 마지막으로 유효했던 정규화된 방향 벡터입니다. (애니메이터 전달용)
        /// </summary>
        public Vector2 LastDirectionVector { get; private set; }
        
        #endregion
        
        public YisoOrientationAbility(YisoOrientationAbilitySO settings) {
            _settings = settings;
        }

        public override void Initialize(IYisoCharacterContext context) {
            base.Initialize(context);
            ForceFace(_settings.initialFacingDirection);
            YisoLogger.Log($"OrientationAbility 초기화: 초기 방향={_settings.initialFacingDirection}");
        }

        public override void LateInitialize() {
            base.LateInitialize();
            var abilityModule = Context.GetModule<YisoCharacterAbilityModule>();
            if (abilityModule != null) {
                _movementAbility = abilityModule.GetAbility<YisoMovementAbility>();
            }

            if (_movementAbility == null) {
                YisoLogger.LogWarning("YisoMovementAbility를 찾을 수 없습니다. 이동 기반 방향 전환이 작동하지 않습니다.");
            }

            _weaponModule = Context.GetModule<YisoCharacterWeaponModule>();
            if (_weaponModule != null) {
                _weaponAim = _weaponModule.CurrentWeapon?.WeaponAim;
            }

            if (_weaponAim == null) {
                YisoLogger.LogWarning("WeaponAim을 찾을 수 없습니다. 무기 조준 기반 방향 전환이 작동하지 않습니다.");
            } else {
                YisoLogger.Log("WeaponAim 연결 완료");
            }
        }
        
        
        public override void ProcessAbility() {
            // 1. 현재 상황에 따라 '바라볼 방향'의 소스를 결정합니다. (우선순위 로직)
            var sourceDirection = DetermineSourceDirection();
            
            // 2. 결정된 소스 방향을 바탕으로 실제 방향 상태를 업데이트합니다.
            UpdateFacingState(sourceDirection);
        }

        public override void UpdateAnimator() {
            // ========== Animator Parameter Architecture ==========
            // [Continuous Values] - Ability에서 매 프레임 업데이트
            // - Horizontal: 캐릭터가 바라보는 방향 X축 (-1 ~ 1)
            // - Vertical: 캐릭터가 바라보는 방향 Y축 (-1 ~ 1)
            // =====================================================

            // 계산된 최종 방향 벡터를 애니메이터에 전달합니다.
            Context.PlayAnimation(YisoCharacterAnimationState.Horizontal, LastDirectionVector.x);
            Context.PlayAnimation(YisoCharacterAnimationState.Vertical, LastDirectionVector.y);
        }

        private Vector2 DetermineSourceDirection() {
            // "공격" 시 aim 기반 방향 결정 (1순위)
            if (_weaponAim != null) {
                var aimDirection = _weaponAim.CurrentAim;
                if (aimDirection.sqrMagnitude > _settings.aimThreshold * _settings.aimThreshold) {
                    return aimDirection;
                }
            }

            // "움직일" 시 movement 기반 방향 결정 (2순위)
            if (_movementAbility != null) {
                var moveVector = _movementAbility.FinalMovementInput;
                if (moveVector.sqrMagnitude > _settings.movementThreshold * _settings.movementThreshold) {
                    return moveVector;
                }
            }

            return LastDirectionVector;
        }

        private void UpdateFacingState(Vector2 direction) {
            if (direction.sqrMagnitude < 0.01f) return;

            // 방향 전환이 잠겨있으면 업데이트하지 않음
            if (_isOrientationLocked) return;

            LastDirectionVector = direction.normalized;
            CurrentFacingDirectionVector = LastDirectionVector;
            
            if (Mathf.Abs(LastDirectionVector.x) > Mathf.Abs(LastDirectionVector.y)) {
                CurrentFacingDirection = LastDirectionVector.x > 0 ? FacingDirections.Right : FacingDirections.Left;
            }
            else {
                CurrentFacingDirection = LastDirectionVector.y > 0 ? FacingDirections.Up : FacingDirections.Down;
            }
        }
        
        #region Public API

        /// <summary>
        /// 캐릭터의 방향을 외부에서 강제로 설정합니다 (예: 컷신, 상호작용).
        /// </summary>
        public void ForceFace(FacingDirections direction) {
            CurrentFacingDirection = direction;
            LastDirectionVector = direction.ToVector2();
            CurrentFacingDirectionVector = LastDirectionVector;
        }

        /// <summary>
        /// 방향 전환을 잠급니다 (예: 공격 중).
        /// 잠금 상태에서는 입력이 들어와도 방향이 변경되지 않습니다.
        /// </summary>
        public void LockOrientation() {
            _isOrientationLocked = true;
        }

        /// <summary>
        /// 방향 전환 잠금을 해제합니다.
        /// </summary>
        public void UnlockOrientation() {
            _isOrientationLocked = false;
        }

        /// <summary>
        /// 현재 방향 전환이 잠겨있는지 여부를 반환합니다.
        /// </summary>
        public bool IsOrientationLocked() {
            return _isOrientationLocked;
        }

        #endregion

        public override void OnDeath() {
            base.OnDeath();

            // 사망 시 방향 잠금 해제 (다른 능력이 잠궈놨을 수 있음)
            UnlockOrientation();

            // 무기 방향 잠금도 해제 (안전장치)
            if (_weaponAim != null) {
                _weaponAim.UnlockAim();
            }
        }

        public override void OnRevive() {
            base.OnRevive();

            // 부활 시 방향 초기화
            // 1. Orientation 잠금 해제
            UnlockOrientation();

            // 2. 무기 방향 잠금도 해제
            if (_weaponAim != null) {
                _weaponAim.UnlockAim();
            }

            // 3. 초기 방향으로 복구
            ForceFace(_settings.initialFacingDirection);
        }
    }
}