using System;
using System.Collections.Generic;
using Core.Behaviour;
using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Health {
    /// <summary>
    /// 추가 Animator Parameter 설정을 위한 구조체.
    /// YisoCharacterAnimationState Enum 기반으로 타입 안전성을 보장한다.
    /// </summary>
    [Serializable]
    public struct AnimatorParameterSetting {
        [LabelText("Parameter")]
        [Tooltip("제어할 Animator Parameter (Enum 기반)")]
        public YisoCharacterAnimationState parameter;

        [LabelText("Type")]
        [Tooltip("Parameter 타입")]
        public AnimatorControllerParameterType parameterType;

        // Trigger는 값 필요 없음
        [ShowIf("@parameterType == UnityEngine.AnimatorControllerParameterType.Bool")]
        [LabelText("Bool Value")]
        public bool boolValue;

        [ShowIf("@parameterType == UnityEngine.AnimatorControllerParameterType.Float")]
        [LabelText("Float Value")]
        public float floatValue;

        [ShowIf("@parameterType == UnityEngine.AnimatorControllerParameterType.Int")]
        [LabelText("Int Value")]
        public int intValue;
    }

    /// <summary>
    /// YisoEntityHealth 이벤트에 반응하여 Animator Parameter를 설정하는 컴포넌트.
    /// 필수 파라미터(IsDead, IsHit)는 코드에서 자동 설정하며, Inspector에서는 추가 파라미터만 설정 가능하다.
    /// 모든 Animator 제어는 YisoCharacterAnimationModule을 경유하여 일관성을 보장한다.
    /// </summary>
    [AddComponentMenu("Yiso/Health/Health Animator")]
    public class YisoHealthAnimator : RunIBehaviour {
        [Title("Animation Settings")]
        [InfoBox("필수 파라미터(IsDead, IsHit)는 자동으로 설정됩니다.\n여기서는 추가로 설정할 파라미터만 지정하세요 (예: DeathType, HitDirection 등).")]
        [Tooltip("체크 해제 시 이 컴포넌트의 모든 애니메이션 업데이트 기능이 비활성화됩니다.")]
        [SerializeField] private bool updateAnimatorParameters = true;

        [Title("Additional Parameters (Optional)")]
        [InfoBox("피격 시 IsHit Trigger는 자동 설정됩니다. 추가 파라미터가 있다면 아래에 지정하세요.")]
        [Tooltip("피격 시 추가로 설정할 파라미터 (예: HitType, ImpactDirection 등)")]
        [SerializeField] private List<AnimatorParameterSetting> onDamageAdditionalActions;

        [InfoBox("사망 시 IsDead=true는 자동 설정됩니다. 추가 파라미터가 있다면 아래에 지정하세요.")]
        [Tooltip("사망 시 추가로 설정할 파라미터 (예: DeathType, DeathDirection 등)")]
        [SerializeField] private List<AnimatorParameterSetting> onDeathAdditionalActions;

        [InfoBox("부활 시 IsDead=false는 자동 설정됩니다. 추가 파라미터가 있다면 아래에 지정하세요.")]
        [Tooltip("부활 시 추가로 설정할 파라미터")]
        [SerializeField] private List<AnimatorParameterSetting> onReviveAdditionalActions;

        private YisoEntityHealth _entityHealth;
        private IYisoCharacterContext _context;
        private YisoCharacterAnimationModule _animationModule;

        protected override void Awake() {
            base.Awake();
            _entityHealth = GetComponentInParent<YisoEntityHealth>();

            if (_entityHealth == null) {
                Debug.LogError($"[{gameObject.name}] YisoHealthAnimator: YisoEntityHealth를 찾을 수 없습니다!", this);
                updateAnimatorParameters = false;
                enabled = false;
                return;
            }
        }

        protected override void Start() {
            base.Start();

            // YisoCharacter Context 가져오기 (캐릭터가 아닌 경우 null)
            _context = GetComponentInParent<IYisoCharacterContext>();

            if (_context != null) {
                _animationModule = _context.GetModule<YisoCharacterAnimationModule>();
            }

            if (_animationModule == null) {
                Debug.LogWarning($"[{gameObject.name}] YisoHealthAnimator: YisoCharacterAnimationModule을 찾을 수 없습니다. " +
                                 "이 컴포넌트는 YisoCharacter에만 작동합니다.", this);
                updateAnimatorParameters = false;
                enabled = false;
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            if (_entityHealth == null) return;
            _entityHealth.OnDamaged += HandleDamage;
            _entityHealth.OnDied += HandleDeath;
            _entityHealth.OnRevived += HandleRevive;
        }

        protected override void OnDisable() {
            base.OnDisable();
            if (_entityHealth == null) return;
            _entityHealth.OnDamaged -= HandleDamage;
            _entityHealth.OnDied -= HandleDeath;
            _entityHealth.OnRevived -= HandleRevive;
        }

        /// <summary>
        /// 피격 시 호출.
        /// 필수 파라미터: IsHit Trigger 자동 설정
        /// 추가 파라미터: Inspector 설정에 따라 적용
        /// </summary>
        private void HandleDamage(DamageInfo damageInfo) {
            if (!updateAnimatorParameters || _animationModule == null) return;

            // 1. 필수 파라미터 자동 설정: IsHit Trigger
            _animationModule.SetTrigger(YisoCharacterAnimationState.IsHit);

            // 2. 추가 파라미터 적용 (선택 사항)
            ApplyAdditionalActions(onDamageAdditionalActions);
        }

        /// <summary>
        /// 사망 시 호출.
        /// 필수 파라미터: IsDead = true 자동 설정
        /// 추가 파라미터: Inspector 설정에 따라 적용 (예: DeathType)
        /// </summary>
        private void HandleDeath() {
            if (!updateAnimatorParameters || _animationModule == null) return;

            // 1. 필수 파라미터 자동 설정: IsDead = true
            _animationModule.SetBool(YisoCharacterAnimationState.IsDead, true);

            // 2. 추가 파라미터 적용 (선택 사항)
            ApplyAdditionalActions(onDeathAdditionalActions);
        }

        /// <summary>
        /// 부활 시 호출.
        /// 필수 파라미터: IsDead = false 자동 설정
        /// 추가 파라미터: Inspector 설정에 따라 적용
        /// </summary>
        private void HandleRevive() {
            if (!updateAnimatorParameters || _animationModule == null) return;

            // 1. 필수 파라미터 자동 설정: IsDead = false
            _animationModule.SetBool(YisoCharacterAnimationState.IsDead, false);
            _animationModule.SetTrigger(YisoCharacterAnimationState.IsSpawning);

            // 2. 추가 파라미터 적용 (선택 사항)
            ApplyAdditionalActions(onReviveAdditionalActions);
        }

        /// <summary>
        /// 추가 파라미터 리스트를 AnimationModule을 통해 적용한다.
        /// 모든 Animator 제어는 AnimationModule을 경유하여 검증 및 타입 안전성을 보장한다.
        /// </summary>
        private void ApplyAdditionalActions(List<AnimatorParameterSetting> actions) {
            if (actions == null || actions.Count == 0) return;

            foreach (var action in actions) {
                if (action.parameter == YisoCharacterAnimationState.None) {
                    Debug.LogWarning($"[{gameObject.name}] YisoHealthAnimator: AnimatorParameterSetting에 None이 설정되어 있습니다. 무시합니다.");
                    continue;
                }

                // AnimationModule을 통해 파라미터 설정 (검증 포함)
                switch (action.parameterType) {
                    case AnimatorControllerParameterType.Trigger:
                        _animationModule.SetTrigger(action.parameter);
                        break;

                    case AnimatorControllerParameterType.Bool:
                        _animationModule.SetBool(action.parameter, action.boolValue);
                        break;

                    case AnimatorControllerParameterType.Int:
                        _animationModule.SetInteger(action.parameter, action.intValue);
                        break;

                    case AnimatorControllerParameterType.Float:
                        _animationModule.SetFloat(action.parameter, action.floatValue);
                        break;

                    default:
                        Debug.LogWarning($"[{gameObject.name}] YisoHealthAnimator: 지원하지 않는 파라미터 타입 {action.parameterType}");
                        break;
                }
            }
        }
    }
}
