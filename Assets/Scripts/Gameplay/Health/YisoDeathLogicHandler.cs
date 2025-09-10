using Core.Behaviour;
using Gameplay.Character.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Health {
    [AddComponentMenu("Yiso/Health/Death Logic Handler")]
    public class YisoDeathLogicHandler : RunIBehaviour {
        [Title("Object Lifecycle")] [Tooltip("사망 시 이 게임 오브젝트를 파괴할지 여부입니다.")]
        [SerializeField] private bool destroyOnDeath = true;

        [Tooltip("destroyOnDeath가 true일 때, 사망 후 몇 초 뒤에 파괴할지 결정합니다.")] [ShowIf("destroyOnDeath")]
        [SerializeField] private float delayBeforeDestruction = 3f;

        [Tooltip("부활 시, 저장된 초기 위치로 이동할지 여부")]
        [SerializeField] private bool respawnAtInitialLocation = false;

        [Tooltip("사망 시 모델을 비활성화할지 여부입니다.")]
        [SerializeField] private bool disableModelOnDeath = true;

        [Title("Rewards")] [Header("Experience Points")]
        [SerializeField] private bool grantExperienceOnDeath = false;

        private YisoEntityHealth _entityHealth;
        private IYisoCharacterContext _characterContext;
        private GameObject _model;
        private Vector3 _initialPosition;
        private bool _isQuitting = false;

        protected override void Awake() {
            base.Awake();
            _entityHealth = GetComponent<YisoEntityHealth>();
            _characterContext = GetComponent<IYisoCharacterContext>();
            _model = _characterContext != null ? _characterContext.Model : gameObject;

            if (respawnAtInitialLocation) {
                _initialPosition = transform.position;
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            if (_entityHealth == null) return;
            _entityHealth.OnDied += HandleDeath;
            _entityHealth.OnRevived += HandleRevive;
        }

        protected override void OnDisable() {
            base.OnDisable();
            if (_entityHealth == null || _isQuitting) return;
            _entityHealth.OnDied -= HandleDeath;
            _entityHealth.OnRevived -= HandleRevive;
        }

        private void HandleDeath() {
            if (grantExperienceOnDeath) GrantExperience();
            if (disableModelOnDeath && _model != null) _model.SetActive(false);
            
            if (destroyOnDeath) {
                DestroySelf();
            }
        }

        private void HandleRevive() {
            if (_model != null) _model.SetActive(true);
            if (respawnAtInitialLocation) {
                transform.position = _initialPosition;
            }
        }

        private void GrantExperience() {
            if (!grantExperienceOnDeath) return;
            // TODO: Core Service 연동
        }

        private void DestroySelf() {
            Destroy(gameObject, delayBeforeDestruction);
        }

        private void OnApplicationQuit() {
            _isQuitting = true;
        }
    }
}