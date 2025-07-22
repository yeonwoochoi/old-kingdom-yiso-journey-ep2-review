using System.Collections.Generic;
using UnityEngine;
using Utilities.Extensions;

namespace Gameplay.Character.Core.Modules {
    public sealed class YisoCharacterAnimationModule : YisoCharacterModuleBase {
        private Animator _animator;
        private HashSet<int> _animatorParameters;

        #region Animator Parameters

        // Animator Parameter Names
        private const string k_MoveSpeedParam = "MoveSpeed";
        private const string k_AtkSpeedParam = "AttackSpeed";
        private const string k_HorizontalParam = "X";
        private const string k_VerticalParam = "Y";
        private const string k_IsIdleParam = "IsIdle";
        private const string k_IsMovingParam = "IsMoving";
        private const string k_IsAttackingParam = "IsAttacking";
        private const string k_IsMoveAttackingParam = "IsMoveAttack";
        private const string k_IsDeadParam = "IsDeath";
        private const string k_IsSpellCastingParam = "IsSpellCasting";
        private const string k_IsSkillCastingParam = "IsSkillCasting";
        private const string k_IsSpawningParam = "IsSpawn";
        private const string k_SkillNumberParam = "SkillNumber";
        private const string k_ComboParam = "Combo";
        private const string k_DeathTypeParam = "DeathType";

        // Hashed Parameter IDs
        private int _horizontalHash;
        private int _verticalHash;
        private int _isIdleHash;
        private int _isMovingHash;
        private int _isAttackingHash;
        private int _isMoveAttackingHash;
        private int _isDeadHash;
        private int _isSpellCastingHash;
        private int _isSkillCastingHash;
        private int _isSpawningHash;
        private int _skillNumberHash;
        private int _comboHash;
        private int _deathTypeHash;
        private int _moveSpeedHash;
        private int _attackSpeedHash;

        #endregion

        public override void Initialize(YisoCharacter character) {
            base.Initialize(character);
            _animator = character.CharacterAnimator;
            _animatorParameters = new HashSet<int>();
            InitializeParameters();
        }

        private void InitializeParameters() {
            if (_animator == null) return;

            AnimatorExtensions.AddAnimatorParameterIfExists(_animator, k_HorizontalParam, out _horizontalHash,
                AnimatorControllerParameterType.Float, _animatorParameters);
            AnimatorExtensions.AddAnimatorParameterIfExists(_animator, k_VerticalParam, out _verticalHash,
                AnimatorControllerParameterType.Float, _animatorParameters);
            AnimatorExtensions.AddAnimatorParameterIfExists(_animator, k_IsIdleParam, out _isIdleHash,
                AnimatorControllerParameterType.Bool, _animatorParameters);
            AnimatorExtensions.AddAnimatorParameterIfExists(_animator, k_IsMovingParam, out _isMovingHash,
                AnimatorControllerParameterType.Bool, _animatorParameters);
            AnimatorExtensions.AddAnimatorParameterIfExists(_animator, k_IsAttackingParam, out _isAttackingHash,
                AnimatorControllerParameterType.Bool, _animatorParameters);
            AnimatorExtensions.AddAnimatorParameterIfExists(_animator, k_IsMoveAttackingParam, out _isMoveAttackingHash,
                AnimatorControllerParameterType.Bool, _animatorParameters);
            AnimatorExtensions.AddAnimatorParameterIfExists(_animator, k_IsDeadParam, out _isDeadHash,
                AnimatorControllerParameterType.Bool, _animatorParameters);
            AnimatorExtensions.AddAnimatorParameterIfExists(_animator, k_IsSpellCastingParam, out _isSpellCastingHash,
                AnimatorControllerParameterType.Bool, _animatorParameters);
            AnimatorExtensions.AddAnimatorParameterIfExists(_animator, k_IsSkillCastingParam, out _isSkillCastingHash,
                AnimatorControllerParameterType.Bool, _animatorParameters);
            AnimatorExtensions.AddAnimatorParameterIfExists(_animator, k_IsSpawningParam, out _isSpawningHash,
                AnimatorControllerParameterType.Trigger, _animatorParameters);
            AnimatorExtensions.AddAnimatorParameterIfExists(_animator, k_SkillNumberParam, out _skillNumberHash,
                AnimatorControllerParameterType.Int, _animatorParameters);
            AnimatorExtensions.AddAnimatorParameterIfExists(_animator, k_ComboParam, out _comboHash,
                AnimatorControllerParameterType.Int, _animatorParameters);
            AnimatorExtensions.AddAnimatorParameterIfExists(_animator, k_DeathTypeParam, out _deathTypeHash,
                AnimatorControllerParameterType.Int, _animatorParameters);
            AnimatorExtensions.AddAnimatorParameterIfExists(_animator, k_MoveSpeedParam, out _moveSpeedHash,
                AnimatorControllerParameterType.Float, _animatorParameters);
            AnimatorExtensions.AddAnimatorParameterIfExists(_animator, k_AtkSpeedParam, out _attackSpeedHash,
                AnimatorControllerParameterType.Float, _animatorParameters);
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