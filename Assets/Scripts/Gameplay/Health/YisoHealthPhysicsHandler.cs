using Core.Behaviour;
using Gameplay.Core;
using Settings;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Health {
    /// <summary>
    /// 이 컴포넌트는 오직 물리(Physics)와 관련된 책임만 다룬다.
    /// Rigidbody2D, Collider2D, Renderer의 정렬 레이어, 그리고 이동 컨트롤러의 활성화 여부만을 제어
    /// </summary>
    [AddComponentMenu("Yiso/Health/Health Physics Handler")]
    public class YisoHealthPhysicsHandler : RunIBehaviour {

        [Title("Knockback Settings")]
        [SerializeField] private bool canBeKnockedBack = true;
        [SerializeField, ShowIf("canBeKnockedBack")] private float knockbackMultiplier = 1f;

        [Title("Death Physics Settings")]
        [SerializeField] private bool disableCollisionsOnDeath = true;
        [SerializeField] private bool disableMovementOnDeath = true; // [이름 변경됨] 더 명확한 이름으로 변경
        [SerializeField] private bool changeLayerOnDeath = true;
        
        [Title("Additional Components Control")]
        [Tooltip("활성화/비활성화할 추가 컴포넌트")] [SerializeField]
        private MonoBehaviour[] additionalComponents; // 특정 컨트롤러 타입 대신 MonoBehaviour로 받아 유연성 확보
        
        private Renderer _mainRenderer; //정렬 레이어를 변경할 Renderer입니다. 없으면 자식에서 찾습니다.

        private string layerOnDeath = GameLayers.BackgroundSortingName;
        private YisoEntityHealth _entityHealth;
        private IPhysicsControllable _physicsController; // top down controller가 있는 경우 우선 적용됨.
        private string _layerOnAlive; // 살아있을 때 원래 레이어 저장

        protected override void Awake() {
            base.Awake();
            _entityHealth = GetComponentInParent<YisoEntityHealth>();
            _physicsController = GetComponentInParent<IPhysicsControllable>();
            _mainRenderer = _entityHealth?.GetComponentInChildren<Renderer>();
            
            if (changeLayerOnDeath && _mainRenderer != null) {
                _layerOnAlive = _mainRenderer.sortingLayerName;
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            if (_entityHealth != null) {
                _entityHealth.OnDamaged += HandleDamage;
                _entityHealth.OnDied += HandleDeath;
                _entityHealth.OnRevived += HandleRevive;
            }
        }

        protected override void OnDisable() {
            base.OnDisable();
            if (_entityHealth != null) {
                _entityHealth.OnDamaged -= HandleDamage;
                _entityHealth.OnDied -= HandleDeath;
                _entityHealth.OnRevived -= HandleRevive;
            }
        }

        private void HandleDamage(DamageInfo damageInfo) {
            // _physicsController가 없으면 넉백 로직을 실행하지 않는다.
            if (!canBeKnockedBack || _physicsController == null || damageInfo.KnockbackForce <= 0) {
                return;
            }

            var totalKnockbackForce = damageInfo.KnockbackForce * knockbackMultiplier;
            _physicsController.Impact(damageInfo.DamageDirection.normalized, totalKnockbackForce);
        }

        private void HandleDeath() {
            if (_physicsController != null) {
                if (disableCollisionsOnDeath) _physicsController.SetCollisionsEnabled(false);
                if (disableMovementOnDeath) _physicsController.SetMovementEnabled(false);
            }

            if (changeLayerOnDeath && _mainRenderer != null) {
                _mainRenderer.sortingLayerName = layerOnDeath;
            }

            SetAdditionalComponentsEnabled(false);
        }

        private void HandleRevive() {
            if (_physicsController != null) {
                if (disableCollisionsOnDeath) _physicsController.SetCollisionsEnabled(true);
                if (disableMovementOnDeath) _physicsController.SetMovementEnabled(true);
            }

            if (changeLayerOnDeath && _mainRenderer != null) _mainRenderer.sortingLayerName = _layerOnAlive;

            SetAdditionalComponentsEnabled(true);
        }

        private void SetAdditionalComponentsEnabled(bool isEnabled) {
            if (additionalComponents == null) return;
            foreach (var component in additionalComponents) {
                if (component != null) component.enabled = isEnabled;
            }
        }
    }
}