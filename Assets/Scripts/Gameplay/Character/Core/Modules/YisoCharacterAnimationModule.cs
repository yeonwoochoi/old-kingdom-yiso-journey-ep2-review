using System.Collections.Generic;
using UnityEngine;
using Utilities.Extensions;

namespace Gameplay.Character.Core.Modules {
    public static class YisoAnimatorParams {
        public const string MoveSpeed = "MoveSpeed";
        public const string AttackSpeed = "AttackSpeed";
        public const string Horizontal = "X";
        public const string Vertical = "Y";
        public const string IsIdle = "IsIdle";
        public const string IsMoving = "IsMoving";
        public const string IsAttacking = "IsAttacking";
        public const string IsMoveAttacking = "IsMoveAttack";
        public const string IsDead = "IsDeath";
        public const string IsSpellCasting = "IsSpellCasting";
        public const string IsSkillCasting = "IsSkillCasting";
        public const string IsSpawning = "IsSpawn";
        public const string SkillNumber = "SkillNumber";
        public const string Combo = "Combo";
        public const string DeathType = "DeathType";
    }
    
    public static class YisoAnimatorHashes {
        public static readonly int MoveSpeed = Animator.StringToHash(YisoAnimatorParams.MoveSpeed);
        public static readonly int AttackSpeed = Animator.StringToHash(YisoAnimatorParams.AttackSpeed);
        public static readonly int Horizontal = Animator.StringToHash(YisoAnimatorParams.Horizontal);
        public static readonly int Vertical = Animator.StringToHash(YisoAnimatorParams.Vertical);
        public static readonly int IsIdle = Animator.StringToHash(YisoAnimatorParams.IsIdle);
        public static readonly int IsMoving = Animator.StringToHash(YisoAnimatorParams.IsMoving);
        public static readonly int IsAttacking = Animator.StringToHash(YisoAnimatorParams.IsAttacking);
        public static readonly int IsMoveAttacking = Animator.StringToHash(YisoAnimatorParams.IsMoveAttacking);
        public static readonly int IsDead = Animator.StringToHash(YisoAnimatorParams.IsDead);
        public static readonly int IsSpellCasting = Animator.StringToHash(YisoAnimatorParams.IsSpellCasting);
        public static readonly int IsSkillCasting = Animator.StringToHash(YisoAnimatorParams.IsSkillCasting);
        public static readonly int IsSpawning = Animator.StringToHash(YisoAnimatorParams.IsSpawning);
        public static readonly int SkillNumber = Animator.StringToHash(YisoAnimatorParams.SkillNumber);
        public static readonly int Combo = Animator.StringToHash(YisoAnimatorParams.Combo);
        public static readonly int DeathType = Animator.StringToHash(YisoAnimatorParams.DeathType);
    }
    
    public sealed class YisoCharacterAnimationModule : YisoCharacterModuleBase {
        private Animator _animator;
        private HashSet<int> _animatorParameters;

        public override void Initialize(YisoCharacter character) {
            base.Initialize(character);
            _animator = character.Animator;
            _animatorParameters = new HashSet<int>();
            InitializeParameters();
        }

        private void InitializeParameters() {
            if (_animator == null) return;

            // 모든 파라미터 해시를 등록 (Animator에 실제로 있는 것만)
            _animator.TryAddAnimatorParameter(YisoAnimatorParams.Horizontal, AnimatorControllerParameterType.Float, _animatorParameters);
            _animator.TryAddAnimatorParameter(YisoAnimatorParams.Vertical, AnimatorControllerParameterType.Float, _animatorParameters);
            _animator.TryAddAnimatorParameter(YisoAnimatorParams.MoveSpeed, AnimatorControllerParameterType.Float, _animatorParameters);
            _animator.TryAddAnimatorParameter(YisoAnimatorParams.AttackSpeed, AnimatorControllerParameterType.Float, _animatorParameters);
            _animator.TryAddAnimatorParameter(YisoAnimatorParams.IsIdle, AnimatorControllerParameterType.Bool, _animatorParameters);
            _animator.TryAddAnimatorParameter(YisoAnimatorParams.IsMoving, AnimatorControllerParameterType.Bool, _animatorParameters);
            _animator.TryAddAnimatorParameter(YisoAnimatorParams.IsAttacking, AnimatorControllerParameterType.Bool, _animatorParameters);
            _animator.TryAddAnimatorParameter(YisoAnimatorParams.IsMoveAttacking, AnimatorControllerParameterType.Bool, _animatorParameters);
            _animator.TryAddAnimatorParameter(YisoAnimatorParams.IsDead, AnimatorControllerParameterType.Bool, _animatorParameters);
            _animator.TryAddAnimatorParameter(YisoAnimatorParams.IsSpellCasting, AnimatorControllerParameterType.Bool, _animatorParameters);
            _animator.TryAddAnimatorParameter(YisoAnimatorParams.IsSkillCasting, AnimatorControllerParameterType.Bool, _animatorParameters);
            _animator.TryAddAnimatorParameter(YisoAnimatorParams.IsSpawning, AnimatorControllerParameterType.Trigger, _animatorParameters);
            _animator.TryAddAnimatorParameter(YisoAnimatorParams.SkillNumber, AnimatorControllerParameterType.Int, _animatorParameters);
            _animator.TryAddAnimatorParameter(YisoAnimatorParams.Combo, AnimatorControllerParameterType.Int, _animatorParameters);
            _animator.TryAddAnimatorParameter(YisoAnimatorParams.DeathType, AnimatorControllerParameterType.Int, _animatorParameters);
        }

        public override void OnUpdate() {
            base.OnUpdate();
            UpdateAnimators();
        }

        private void UpdateAnimators() {
            if (_animator == null) return;
            foreach (var ability in Character.GetModule<YisoCharacterAbilityModule>().Abilities) {
                if (ability.enabled && ability.AbilityInitialized) {
                    ability.UpdateAnimator();
                }
            }
        }
    }
}