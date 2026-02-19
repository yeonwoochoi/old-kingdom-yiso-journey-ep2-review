using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

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
        IsMoving,
        IsAttacking,
        IsMoveAttacking,
        IsDead,
        IsHit, // Trigger 타입 (피격 시)
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
            {YisoCharacterAnimationState.IsMoving, "IsMoving"},
            {YisoCharacterAnimationState.IsAttacking, "IsAttack"},
            {YisoCharacterAnimationState.IsMoveAttacking, "IsMoveAttack"},
            {YisoCharacterAnimationState.IsDead, "IsDeath"},
            {YisoCharacterAnimationState.IsSpellCasting, "IsSpellCast"},
            {YisoCharacterAnimationState.IsSkillCasting, "IsSkillCast"},
            {YisoCharacterAnimationState.IsSpawning, "IsSpawn"},
            {YisoCharacterAnimationState.SkillNumber, "SkillNumber"},
            {YisoCharacterAnimationState.Combo, "Combo"},
            {YisoCharacterAnimationState.DeathType, "DeathType"},
            {YisoCharacterAnimationState.IsHit, "IsHit"},
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

        // 등록된 외부 Animator 목록 (무기, VFX 등)
        private readonly List<Animator> _externalAnimators = new();

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
                YisoLogger.LogWarning($"[YisoCharacterAnimationModule] Animator is null for {Context.GameObject.name}. Cannot initialize parameters.");
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
                    YisoLogger.LogWarning($"[YisoCharacterAnimationModule] Animator parameter '{paramName}' (Type: {paramType}) not found in Animator Controller for {Context.GameObject.name}.");
                }
            }
            
            YisoLogger.Log($"[YisoCharacterAnimationModule] Initialized {Context.GameObject.name}'s Animator parameters. Found {_verifiedHashes.Count} parameters.");
        }
        
        private bool IsVerified(int hash) {
            return hash != 0 && _verifiedHashes.Contains(hash);
        }

        #region Animator Controller Swap

        /// <summary>
        /// 캐릭터의 AnimatorController를 교체하고 모든 파라미터를 초기화한다.
        /// 무기 교체 시 AOC 전환에 사용.
        /// </summary>
        public void SetAnimatorController(RuntimeAnimatorController controller) {
            if (controller == null || _animator == null) return;

            // 1. AnimatorController 교체
            _animator.runtimeAnimatorController = controller;

            // 2. 모든 파라미터를 기본값으로 초기화
            ResetAllParameters();

            // 3. 새 Controller에 맞게 파라미터 재검증
            VerifyAnimatorParameters();
        }

        /// <summary>
        /// Animator의 모든 파라미터를 기본값으로 초기화한다.
        /// </summary>
        private void ResetAllParameters() {
            if (_animator == null) return;

            foreach (var param in _animator.parameters) {
                switch (param.type) {
                    case AnimatorControllerParameterType.Bool:
                        _animator.SetBool(param.nameHash, param.defaultBool);
                        break;
                    case AnimatorControllerParameterType.Float:
                        _animator.SetFloat(param.nameHash, param.defaultFloat);
                        break;
                    case AnimatorControllerParameterType.Int:
                        _animator.SetInteger(param.nameHash, param.defaultInt);
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        _animator.ResetTrigger(param.nameHash);
                        break;
                }
            }
        }

        #endregion

        #region External Animator Registration

        /// <summary>
        /// 외부 Animator를 등록하여 파라미터 변경 시 자동 동기화되도록 한다.
        /// (예: 무기 Animator, VFX Animator 등)
        /// </summary>
        public void RegisterExternalAnimator(Animator externalAnimator) {
            if (externalAnimator == null) {
                YisoLogger.LogWarning("[YisoCharacterAnimationModule] Attempted to register null external animator.");
                return;
            }

            if (_externalAnimators.Contains(externalAnimator)) {
                YisoLogger.LogWarning($"[YisoCharacterAnimationModule] Animator '{externalAnimator.name}' is already registered.");
                return;
            }

            _externalAnimators.Add(externalAnimator);

            // 등록 시 현재 상태를 초기 동기화 (Float, Bool, Int만)
            SyncToExternalAnimator(externalAnimator);

            YisoLogger.Log($"[YisoCharacterAnimationModule] Registered external animator '{externalAnimator.name}'. Total: {_externalAnimators.Count}");
        }

        /// <summary>
        /// 등록된 외부 Animator를 제거한다.
        /// </summary>
        public void UnregisterExternalAnimator(Animator externalAnimator) {
            if (externalAnimator == null) return;

            if (_externalAnimators.Remove(externalAnimator)) {
                YisoLogger.Log($"[YisoCharacterAnimationModule] Unregistered external animator '{externalAnimator.name}'. Remaining: {_externalAnimators.Count}");
            }
        }

        #endregion

        #region Animator Parameter Control (Top-down Push)

        public void SetBool(YisoCharacterAnimationState state, bool value) {
            var hash = YisoAnimatorHashManager.GetHash(state);
            if (!IsVerified(hash)) return;

            // 1. 캐릭터 Animator 설정
            _animator.SetBool(hash, value);

            // 2. 등록된 외부 Animator에도 Push
            PushBoolToExternalAnimators(hash, value, state);
        }

        public void SetFloat(YisoCharacterAnimationState state, float value) {
            var hash = YisoAnimatorHashManager.GetHash(state);
            if (!IsVerified(hash)) return;

            // 1. 캐릭터 Animator 설정
            _animator.SetFloat(hash, value);

            // 2. 등록된 외부 Animator에도 Push
            PushFloatToExternalAnimators(hash, value, state);
        }

        public void SetInteger(YisoCharacterAnimationState state, int value) {
            var hash = YisoAnimatorHashManager.GetHash(state);
            if (!IsVerified(hash)) return;

            // 1. 캐릭터 Animator 설정
            _animator.SetInteger(hash, value);

            // 2. 등록된 외부 Animator에도 Push
            PushIntToExternalAnimators(hash, value, state);
        }

        public void SetTrigger(YisoCharacterAnimationState state) {
            var hash = YisoAnimatorHashManager.GetHash(state);
            if (!IsVerified(hash)) return;

            // 1. 캐릭터 Animator 설정
            _animator.SetTrigger(hash);

            // 2. 등록된 외부 Animator에도 Push (Trigger는 즉시 전파 필수!)
            PushTriggerToExternalAnimators(hash, state);
        }

        #endregion

        #region Push to External Animators

        private void PushBoolToExternalAnimators(int hash, bool value, YisoCharacterAnimationState state) {
            if (_externalAnimators.Count == 0) return;

            var paramName = YisoAnimatorHashManager.GetName(state);
            var paramType = AnimatorControllerParameterType.Bool;

            foreach (var externalAnimator in _externalAnimators) {
                if (externalAnimator == null) continue;
                if (!externalAnimator.HasParameterOfType(paramName, paramType)) continue;

                // 값 변경 체크 (불필요한 State Machine Trigger 방지)
                if (externalAnimator.GetBool(hash) != value) {
                    externalAnimator.SetBool(hash, value);
                }
            }
        }

        private void PushFloatToExternalAnimators(int hash, float value, YisoCharacterAnimationState state) {
            if (_externalAnimators.Count == 0) return;

            var paramName = YisoAnimatorHashManager.GetName(state);
            var paramType = AnimatorControllerParameterType.Float;

            foreach (var externalAnimator in _externalAnimators) {
                if (externalAnimator == null) continue;
                if (!externalAnimator.HasParameterOfType(paramName, paramType)) continue;

                externalAnimator.SetFloat(hash, value);
            }
        }

        private void PushIntToExternalAnimators(int hash, int value, YisoCharacterAnimationState state) {
            if (_externalAnimators.Count == 0) return;

            var paramName = YisoAnimatorHashManager.GetName(state);
            var paramType = AnimatorControllerParameterType.Int;

            foreach (var externalAnimator in _externalAnimators) {
                if (externalAnimator == null) continue;
                if (!externalAnimator.HasParameterOfType(paramName, paramType)) continue;

                // 값 변경 체크
                if (externalAnimator.GetInteger(hash) != value) {
                    externalAnimator.SetInteger(hash, value);
                }
            }
        }

        private void PushTriggerToExternalAnimators(int hash, YisoCharacterAnimationState state) {
            if (_externalAnimators.Count == 0) return;

            var paramName = YisoAnimatorHashManager.GetName(state);
            var paramType = AnimatorControllerParameterType.Trigger;

            foreach (var externalAnimator in _externalAnimators) {
                if (externalAnimator == null) continue;
                if (!externalAnimator.HasParameterOfType(paramName, paramType)) continue;

                // Trigger는 즉시 발동 (Top-down Push 방식의 핵심!)
                externalAnimator.SetTrigger(hash);
            }
        }

        #endregion

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
                YisoLogger.LogWarning($"[YisoCharacterAnimationModule] Animator is null for {Context.GameObject.name}. Cannot play animation '{state}'.");
            }
        }

        /// <summary>
        /// 캐릭터 Animator의 검증된 파라미터를 외부 Animator(예: 무기)에 동기화한다.
        /// 검증되지 않은 파라미터는 동기화하지 않아 안전성을 보장한다.
        /// Trigger 파라미터는 일회성 이벤트이므로 동기화하지 않는다.
        /// </summary>
        /// <param name="externalAnimator">동기화할 외부 Animator (예: Weapon Animator)</param>
        public void SyncToExternalAnimator(Animator externalAnimator) {
            if (externalAnimator == null || _animator == null) return;

            // 검증된 파라미터만 동기화
            foreach (YisoCharacterAnimationState state in Enum.GetValues(typeof(YisoCharacterAnimationState))) {
                if (state == YisoCharacterAnimationState.None) continue;

                var hash = YisoAnimatorHashManager.GetHash(state);

                // 검증되지 않은 파라미터는 건너뛰기
                if (!IsVerified(hash)) continue;

                // 파라미터 타입 확인
                var paramType = state.GetParameterType();

                // 외부 Animator에도 해당 파라미터가 있는지 확인
                var paramName = YisoAnimatorHashManager.GetName(state);
                if (!externalAnimator.HasParameterOfType(paramName, paramType)) continue;

                // 파라미터 타입에 따라 동기화
                switch (paramType) {
                    case AnimatorControllerParameterType.Float:
                        var floatVal = _animator.GetFloat(hash);
                        externalAnimator.SetFloat(hash, floatVal);
                        break;

                    case AnimatorControllerParameterType.Bool:
                        // 값 변경 체크 (불필요한 State Machine Trigger 방지)
                        var boolVal = _animator.GetBool(hash);
                        if (externalAnimator.GetBool(hash) != boolVal) {
                            externalAnimator.SetBool(hash, boolVal);
                        }
                        break;

                    case AnimatorControllerParameterType.Int:
                        // 값 변경 체크
                        var intVal = _animator.GetInteger(hash);
                        if (externalAnimator.GetInteger(hash) != intVal) {
                            externalAnimator.SetInteger(hash, intVal);
                        }
                        break;

                    case AnimatorControllerParameterType.Trigger:
                        // Trigger는 동기화하지 않음 (일회성 이벤트)
                        break;
                }
            }
        }

        [Serializable]
        public class Settings {
            public RuntimeAnimatorController defaultAnimationController;
        }
    }

    public static class CharacterAnimationStateHelper {
        public static AnimatorControllerParameterType GetParameterType(this YisoCharacterAnimationState state) {
            switch (state) {
                case YisoCharacterAnimationState.MoveSpeed: return AnimatorControllerParameterType.Float;
                case YisoCharacterAnimationState.AttackSpeed: return AnimatorControllerParameterType.Float;
                case YisoCharacterAnimationState.Horizontal: return AnimatorControllerParameterType.Float;
                case YisoCharacterAnimationState.Vertical: return AnimatorControllerParameterType.Float;
                case YisoCharacterAnimationState.IsMoving: return AnimatorControllerParameterType.Bool;
                case YisoCharacterAnimationState.IsAttacking: return AnimatorControllerParameterType.Bool;
                case YisoCharacterAnimationState.IsMoveAttacking: return AnimatorControllerParameterType.Bool;
                case YisoCharacterAnimationState.IsDead: return AnimatorControllerParameterType.Bool;
                case YisoCharacterAnimationState.IsSpellCasting: return AnimatorControllerParameterType.Bool;
                case YisoCharacterAnimationState.IsSkillCasting: return AnimatorControllerParameterType.Bool;
                case YisoCharacterAnimationState.IsHit: return AnimatorControllerParameterType.Trigger;
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