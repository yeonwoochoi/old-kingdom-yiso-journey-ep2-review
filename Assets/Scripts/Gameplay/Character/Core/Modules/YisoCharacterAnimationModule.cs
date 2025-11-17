using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gameplay.Character.Core.Modules {
    /// <summary>
    /// 캐릭터 애니메이터의 모든 파라미터 이름을 정의하는 Enum.
    /// 이 Enum의 멤버 이름은 Animator Controller의 파라미터 이름과 정확히 일치해야 한다.
    /// </summary>
    public enum YisoCharacterAnimationState {
        None, // 기본값 또는 유효하지 않은 상태를 나타내기 위한 값
        MoveSpeed,
        AttackSpeed,
        Horizontal,
        Vertical,
        IsIdle,
        IsMoving,
        IsAttacking,
        IsMoveAttacking,
        IsDead,
        IsSpellCasting,
        IsSkillCasting,
        IsSpawning, // Trigger 타입
        SkillNumber,
        Combo,
        DeathType
    }

    public static class YisoAnimatorHashManager {
        private static readonly Dictionary<YisoCharacterAnimationState, int> Hashes = new();

        private static readonly Dictionary<YisoCharacterAnimationState, string> NameMap = new() {
            {YisoCharacterAnimationState.MoveSpeed, "MoveSpeed"},
            {YisoCharacterAnimationState.AttackSpeed, "AttackSpeed"},
            {YisoCharacterAnimationState.Horizontal, "X"},
            {YisoCharacterAnimationState.Vertical, "Y"},
            {YisoCharacterAnimationState.IsIdle, "IsIdle"},
            {YisoCharacterAnimationState.IsMoving, "IsMoving"},
            {YisoCharacterAnimationState.IsAttacking, "IsAttacking"},
            {YisoCharacterAnimationState.IsMoveAttacking, "IsMoveAttack"},
            {YisoCharacterAnimationState.IsDead, "IsDeath"},
            {YisoCharacterAnimationState.IsSpellCasting, "IsSpellCasting"},
            {YisoCharacterAnimationState.IsSkillCasting, "IsSkillCasting"},
            {YisoCharacterAnimationState.IsSpawning, "IsSpawn"},
            {YisoCharacterAnimationState.SkillNumber, "SkillNumber"},
            {YisoCharacterAnimationState.Combo, "Combo"},
            {YisoCharacterAnimationState.DeathType, "DeathType"},
        };

        static YisoAnimatorHashManager() {
            foreach (YisoCharacterAnimationState state in Enum.GetValues(typeof(YisoCharacterAnimationState))) {
                if (state == YisoCharacterAnimationState.None) continue;
                if (NameMap.TryGetValue(state, out var name)) {
                    Hashes[state] = Animator.StringToHash(name);
                }
            }
        }
        
        public static int GetHash(YisoCharacterAnimationState state) {
            return Hashes.GetValueOrDefault(state, 0);
        }
        
        public static string GetName(YisoCharacterAnimationState state) {
            return NameMap.GetValueOrDefault(state);
        }
    }
    
    public sealed class YisoCharacterAnimationModule : YisoCharacterModuleBase {
        private Settings _settings;
        
        private Animator _animator;
        private readonly HashSet<int> _verifiedHashes = new();

        public YisoCharacterAnimationModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
            _animator = context.Animator;
        }

        public override void Initialize() {
            base.Initialize();
            VerifyAnimatorParameters();
        }

        private void VerifyAnimatorParameters() {
            if (_animator == null) {
                Debug.LogWarning($"[YisoCharacterAnimationModule] Animator is null for {Context.GameObject.name}. Cannot initialize parameters.");
                return;
            }
            
            _verifiedHashes.Clear();
            
            foreach (YisoCharacterAnimationState state in Enum.GetValues(typeof(YisoCharacterAnimationState))) {
                if (state == YisoCharacterAnimationState.None) continue; // None은 실제 파라미터가 아니므로 건너뛴다.

                var paramName = YisoAnimatorHashManager.GetName(state);
                var paramType = state.GetParameterType();

                // Animator Controller에 해당 이름과 타입의 파라미터가 실제로 존재하는지 확인
                if (!string.IsNullOrEmpty(paramName) && _animator.HasParameterOfType(paramName, paramType)) {
                    _verifiedHashes.Add(YisoAnimatorHashManager.GetHash(state));
                } else {
                    Debug.LogWarning($"[YisoCharacterAnimationModule] Animator parameter '{paramName}' (Type: {paramType}) not found in Animator Controller for {Context.GameObject.name}.");
                }
            }
            
            Debug.Log($"[YisoCharacterAnimationModule] Initialized {Context.GameObject.name}'s Animator parameters. Found {_verifiedHashes.Count} parameters.");
        }
        
        private bool IsVerified(int hash) {
            return hash != 0 && _verifiedHashes.Contains(hash);
        }
        
        public void SetBool(YisoCharacterAnimationState state, bool value) {
            var hash = YisoAnimatorHashManager.GetHash(state);
            if (IsVerified(hash)) {
                _animator.SetBool(hash, value);
            }
        }

        public void SetFloat(YisoCharacterAnimationState state, float value) {
            var hash = YisoAnimatorHashManager.GetHash(state);
            if (IsVerified(hash)) {
                _animator.SetFloat(hash, value);
            }
        }

        public void SetInteger(YisoCharacterAnimationState state, int value) {
            var hash = YisoAnimatorHashManager.GetHash(state);
            if (IsVerified(hash)) {
                _animator.SetInteger(hash, value);
            }
        }

        public void SetTrigger(YisoCharacterAnimationState state) {
            var hash = YisoAnimatorHashManager.GetHash(state);
            if (IsVerified(hash)) {
                _animator.SetTrigger(hash);
            }
        }

        /// <summary>
        /// 애니메이션 상태 머신의 '흐름'을 즉시 건너뛰고 특정 애니메이션을 '강제로 재생'하는 데 사용됨.
        /// 즉, Animator Controller에 정의된 트랜지션(Transition) 규칙을 '개무시'하고 바로 해당 상태(State)로 넘어가는 역할을 함. (사용 주의!)
        /// CrossFade() 재생 후 동작: 다시 트랜지션 규칙을 따른다. (그 state에서 시작)
        /// </summary>
        /// <param name="state"></param>
        /// <param name="crossFadeDuration"></param>
        public void PlayAnimationForce(YisoCharacterAnimationState state, float crossFadeDuration = 0.2f) {
            if (_animator != null) {
                _animator.CrossFade(state.ToString(), crossFadeDuration);
            } else {
                Debug.LogWarning($"[YisoCharacterAnimationModule] Animator is null for {Context.GameObject.name}. Cannot play animation '{state}'.");
            }
        }
        
        [Serializable] 
        public class Settings {}
    }

    public static class CharacterAnimationStateHelper {
        public static AnimatorControllerParameterType GetParameterType(this YisoCharacterAnimationState state) {
            switch (state) {
                case YisoCharacterAnimationState.MoveSpeed: return AnimatorControllerParameterType.Float;
                case YisoCharacterAnimationState.AttackSpeed: return AnimatorControllerParameterType.Float;
                case YisoCharacterAnimationState.Horizontal: return AnimatorControllerParameterType.Float;
                case YisoCharacterAnimationState.Vertical: return AnimatorControllerParameterType.Float;
                case YisoCharacterAnimationState.IsIdle: return AnimatorControllerParameterType.Bool;
                case YisoCharacterAnimationState.IsMoving: return AnimatorControllerParameterType.Bool;
                case YisoCharacterAnimationState.IsAttacking: return AnimatorControllerParameterType.Bool;
                case YisoCharacterAnimationState.IsMoveAttacking: return AnimatorControllerParameterType.Bool;
                case YisoCharacterAnimationState.IsDead: return AnimatorControllerParameterType.Bool;
                case YisoCharacterAnimationState.IsSpellCasting: return AnimatorControllerParameterType.Bool;
                case YisoCharacterAnimationState.IsSkillCasting: return AnimatorControllerParameterType.Bool;
                case YisoCharacterAnimationState.IsSpawning: return AnimatorControllerParameterType.Trigger;
                case YisoCharacterAnimationState.SkillNumber: return AnimatorControllerParameterType.Int;
                case YisoCharacterAnimationState.Combo: return AnimatorControllerParameterType.Int;
                case YisoCharacterAnimationState.DeathType: return AnimatorControllerParameterType.Int;
                default: return AnimatorControllerParameterType.Int; // 기본값 (실제로는 사용되지 않아야 함)
            }
        }
        
        public static bool HasParameterOfType(this Animator self, string name, AnimatorControllerParameterType type) {
            if (string.IsNullOrEmpty(name)) return false;
            var parameters = self.parameters;
            return parameters.Any(param => param.type == type && param.name == name);
        }
    }
}