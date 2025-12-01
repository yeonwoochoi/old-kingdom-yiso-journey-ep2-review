using Gameplay.Character.Abilities.Definitions;
using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using Gameplay.Character.StateMachine;
using Gameplay.Character.Types;
using Gameplay.Character.Weapon;
using UnityEngine;

namespace Gameplay.Character.Abilities {
    /// <summary>
    /// 캐릭터의 방향(Orientation)을 결정하는 유일한 책임자입니다.
    /// 이동, 조준 등 여러 입력 소스를 종합하여 최종 방향 벡터를 결정하고 외부에 제공합니다.
    /// </summary>
    public class YisoOrientationAbility: YisoCharacterAbilityBase {
        private readonly YisoOrientationAbilitySO _settings;
        
        private YisoMovementAbility _movementAbility;
        private YisoCharacterStateModule _stateModule;
        private YisoWeaponAim _weaponAim;
        // private YisoBaseAim _weaponAim; // TODO (Weapon Aim) 구현 후 참조하기
        
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
        }
        
        public override void LateInitialize() {
            base.LateInitialize();
            var abilityModule = Context.GetModule<YisoCharacterAbilityModule>();
            if (abilityModule != null) {
                _movementAbility = abilityModule.GetAbility<YisoMovementAbility>();
                // _weaponAim = Blah Blah; TODO: 이 부분도 Weapon Aim 가져오기
            }
            
            _stateModule = Context.GetModule<YisoCharacterStateModule>();
            
            var weaponModule = Context.GetModule<YisoCharacterWeaponModule>();
            if (weaponModule != null) {
                _weaponAim = weaponModule.CurrentWeapon?.WeaponAim;
            }
        }
        
        
        public override void ProcessAbility() {
            // 1. 현재 상황에 따라 '바라볼 방향'의 소스를 결정합니다. (우선순위 로직)
            var sourceDirection = DetermineSourceDirection();

            // 2. 결정된 소스 방향을 바탕으로 실제 방향 상태를 업데이트합니다.
            UpdateFacingState(sourceDirection);
        }

        public override void UpdateAnimator() {
            // 계산된 최종 방향 벡터를 애니메이터에 전달합니다.
            Context.PlayAnimation(YisoCharacterAnimationState.Horizontal, LastDirectionVector.x);
            Context.PlayAnimation(YisoCharacterAnimationState.Vertical, LastDirectionVector.y);
        }

        private Vector2 DetermineSourceDirection() {
            var currentState = _stateModule?.CurrentState;
            if (currentState == null) return LastDirectionVector;

            // "공격" 시 aim 기반 방향 결정 (1순위)
            if (currentState.Role == YisoStateRole.Attack || currentState.Role == YisoStateRole.SkillAttack) {
                if (_weaponAim != null) {
                    var aimDirection = _weaponAim.CurrentAim;
                    if (aimDirection.sqrMagnitude > _settings.aimThreshold * _settings.aimThreshold) {
                        return aimDirection;
                    }
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

            LastDirectionVector = direction.normalized;
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
        }
        
        #endregion
    }
}