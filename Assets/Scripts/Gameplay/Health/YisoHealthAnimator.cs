using System;
using System.Collections.Generic;
using Core.Behaviour;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Health {
    [Serializable]
    public struct AnimatorParameterAction {
        public AnimatorControllerParameterType parameterType;
        public string parameterName;

        [ShowIf("parameterType", AnimatorControllerParameterType.Float)]
        public float floatValue;

        [ShowIf("parameterType", AnimatorControllerParameterType.Bool)]
        public bool boolValue;

        [ShowIf("parameterType", AnimatorControllerParameterType.Int)]
        public int intValue;

        [HideInInspector] public int parameterHash;
    }

    [AddComponentMenu("Yiso/Health/Health Animator")]
    public class YisoHealthAnimator : RunIBehaviour {
        [Title("Animation Settings")] [Tooltip("체크 해제 시 이 컴포넌트의 모든 애니메이션 업데이트 기능이 비활성화됩니다.")] [SerializeField]
        private bool updateAnimatorParameters = true;

        [Tooltip("데미지를 받았을 때 실행할 애니메이션 파라미터 목록입니다.")] [SerializeField]
        private List<AnimatorParameterAction> onDamageActions;

        [Tooltip("사망했을 때 실행할 애니메이션 파라미터 목록입니다.")] [SerializeField]
        private List<AnimatorParameterAction> onDeathActions;

        [Tooltip("부활했을 때 실행할 애니메이션 파라미터 목록입니다.")] [SerializeField]
        private List<AnimatorParameterAction> onReviveActions;

        private YisoEntityHealth _entityHealth;
        private Animator _animator;

        protected override void Awake() {
            base.Awake();
            _animator = GetComponentInChildren<Animator>();
            _entityHealth = GetComponent<YisoEntityHealth>();

            if (_animator == null) {
                Debug.LogError($"[{gameObject.name}] YisoHealthAnimator가 제어할 Animator를 찾을 수 없습니다!", this);
                updateAnimatorParameters = false; // 기능 비활성화
            }

            if (_entityHealth == null) {
                Debug.LogError($"[{gameObject.name}] YisoHealthAnimator가 구독할 YisoEntityHealth를 찾을 수 없습니다!", this);
            }

            InitializeParameterHashes();
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

        private void HandleDamage(DamageInfo damageInfo) => ApplyActions(onDamageActions);
        private void HandleDeath() => ApplyActions(onDeathActions);
        private void HandleRevive() => ApplyActions(onReviveActions);

        private void ApplyActions(List<AnimatorParameterAction> actions) {
            if (!updateAnimatorParameters || _animator == null || actions == null) {
                return;
            }

            foreach (var action in actions) {
                switch (action.parameterType) {
                    case AnimatorControllerParameterType.Trigger:
                        _animator.SetTrigger(action.parameterHash);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        _animator.SetBool(action.parameterHash, action.boolValue);
                        break;
                    case AnimatorControllerParameterType.Int:
                        _animator.SetInteger(action.parameterHash, action.intValue);
                        break;
                    case AnimatorControllerParameterType.Float:
                        _animator.SetFloat(action.parameterHash, action.floatValue);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void InitializeParameterHashes() {
            ConvertListToHashes(onDamageActions);
            ConvertListToHashes(onDeathActions);
            ConvertListToHashes(onReviveActions);
        }

        private static void ConvertListToHashes(IList<AnimatorParameterAction> actions) {
            if (actions == null) return;
            for (var i = 0; i < actions.Count; i++) {
                var action = actions[i];
                if (string.IsNullOrEmpty(action.parameterName)) continue;
                action.parameterHash = Animator.StringToHash(action.parameterName);
                actions[i] = action;
            }
        }
    }
}