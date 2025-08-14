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
        private Settings _settings;
        
        private Animator _animator;
        private HashSet<int> _animatorParameters;
        private Dictionary<YisoCharacterAnimationState, int> _animationStateToHash;

        public YisoCharacterAnimationModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
            _animator = context.Animator;
            _animatorParameters = new HashSet<int>();
            _animationStateToHash = new Dictionary<YisoCharacterAnimationState, int>();
        }

        public override void Initialize() {
            base.Initialize();
            InitializeParameters();
        }

        private void InitializeParameters() {
            if (_animator == null) {
                Debug.LogWarning($"[YisoCharacterAnimationModule] Animator is null for {Context.GameObject.name}. Cannot initialize parameters.");
                return;
            }
            
            _animatorParameters.Clear();
            _animationStateToHash.Clear();
            
            foreach (YisoCharacterAnimationState state in Enum.GetValues(typeof(YisoCharacterAnimationState))) {
                if (state == YisoCharacterAnimationState.None) continue; // None은 실제 파라미터가 아니므로 건너뛴다.

                var paramName = state.ToString(); // Enum 이름을 문자열 파라미터 이름으로 사용
                var paramType = state.GetParameterType();

                // Animator Controller에 해당 이름과 타입의 파라미터가 실제로 존재하는지 확인
                if (_animator.HasParameterOfType(paramName, paramType)) {
                    var hash = Animator.StringToHash(paramName);
                    _animationStateToHash[state] = hash; // Enum과 해시 매핑
                    _animatorParameters.Add(hash); // 유효한 파라미터 해시만 HashSet에 추가
                } else {
                    Debug.LogWarning($"[YisoCharacterAnimationModule] Animator parameter '{paramName}' (Type: {paramType}) not found in Animator Controller for {Context.GameObject.name}.");
                }
            }
            
            Debug.Log($"[YisoCharacterAnimationModule] Initialized {Context.GameObject.name}'s Animator parameters. Found {_animatorParameters.Count} parameters.");
        }
        
        private bool TryGetHash(YisoCharacterAnimationState state, out int hash) {
            if (_animationStateToHash.TryGetValue(state, out hash)) {
                return true;
            }
            Debug.LogWarning($"[YisoCharacterAnimationModule] Animator parameter '{state.ToString()}' not registered/found for {Context.GameObject.name}.");
            return false;
        }

        public void SetBool(YisoCharacterAnimationState state, bool value) {
            if (TryGetHash(state, out var hash)) {
                _animator.SetBool(hash, value);
            }
        }

        public void SetFloat(YisoCharacterAnimationState state, float value) {
            if (TryGetHash(state, out var hash)) {
                _animator.SetFloat(hash, value);
            }
        }

        public void SetInteger(YisoCharacterAnimationState state, int value) {
            if (TryGetHash(state, out var hash)) {
                _animator.SetInteger(hash, value);
            }
        }

        public void SetTrigger(YisoCharacterAnimationState state) {
            if (TryGetHash(state, out var hash)) {
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