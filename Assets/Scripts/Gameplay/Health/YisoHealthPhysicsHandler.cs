using Core.Behaviour;
using Gameplay.Character.Core;
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
        [Tooltip("넉백을 적용할 Rigidbody2D")] [SerializeField]
        private Rigidbody2D targetRigidbody;

        [Tooltip("활성화/비활성화할 주 Collider2D")] [SerializeField]
        private Collider2D mainCollider;

        [Tooltip("활성화/비활성화할 추가 컴포넌트")] [SerializeField]
        private MonoBehaviour[] additionalComponents; // 특정 컨트롤러 타입 대신 MonoBehaviour로 받아 유연성 확보

        [Tooltip("정렬 레이어를 변경할 Renderer")] [SerializeField]
        private Renderer mainRenderer;

        [Title("Knockback Settings")] [Tooltip("이 개체가 넉백을 받을 수 있는지 여부")] [SerializeField]
        private bool canBeKnockedBack = true;

        [Tooltip("받는 넉백의 강도를 조절하는 배율")] [SerializeField, ShowIf("canBeKnockedBack")]
        private float knockbackMultiplier = 1f;

        [Title("Death Physics Settings")] [Tooltip("사망 시 주 콜라이더를 비활성화할지 여부")] [SerializeField]
        private bool disableColliderOnDeath = true;

        [Tooltip("사망 시 캐릭터 컨트롤러를 비활성화할지 여부")] [SerializeField]
        private bool disableCharacterControllerOnDeath = true;

        [Tooltip("사망 시 정렬 레이어를 변경할지 여부")] [SerializeField]
        private bool changeLayerOnDeath = true;

        [SerializeField, ShowIf("changeLayerOnDeath")]
        private string layerOnDeath = GameLayers.BackgroundSortingName;

        private YisoEntityHealth _entityHealth;

        private string _layerOnAlive; // 살아있을 때 원래 레이어 저장
        private TopDownController _topDownController; // top down controller가 있는 경우 우선 적용됨.
        private YisoCharacter _character; // character main hub

        protected override void Awake() {
            base.Awake();
            _entityHealth = GetComponent<YisoEntityHealth>();
            _topDownController = GetComponent<TopDownController>();

            if (targetRigidbody == null) targetRigidbody = GetComponent<Rigidbody2D>();
            if (mainCollider == null) mainCollider = GetComponent<Collider2D>();
            if (mainRenderer == null) mainRenderer = GetComponentInChildren<Renderer>();

            if (changeLayerOnDeath && mainRenderer != null) {
                _layerOnAlive = mainRenderer.sortingLayerName;
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
            if (!canBeKnockedBack || damageInfo.KnockbackForce <= 0) {
                return;
            }

            // 넉백 힘 계산 및 적용
            Vector2 knockbackDirection = damageInfo.DamageDirection.normalized;
            var totalKnockbackForce = damageInfo.KnockbackForce * knockbackMultiplier;

            if (_topDownController != null) {
                _topDownController.Impact(knockbackDirection, totalKnockbackForce);
            }
            else if (targetRigidbody != null) {
                targetRigidbody.velocity = Vector2.zero; // 기존 속도를 잠시 0으로 만들어 넉백이 더 잘 느껴지게 할 수 있음.
                targetRigidbody.AddForce(knockbackDirection * totalKnockbackForce, ForceMode2D.Impulse);
            }
        }

        private void HandleDeath() {
            if (disableColliderOnDeath) {
                if (_topDownController != null) {
                    _topDownController.SetCollisions(false);
                }
                else if (mainCollider != null) {
                    mainCollider.enabled = false;
                }
            }

            if (disableCharacterControllerOnDeath && _character != null) {
                _character.enabled = false;
            }

            if (changeLayerOnDeath && mainRenderer != null) {
                mainRenderer.sortingLayerName = layerOnDeath;
            }

            SetAdditionalComponents(false);
        }

        private void HandleRevive() {
            if (disableColliderOnDeath) {
                if (_topDownController != null) {
                    _topDownController.SetCollisions(true);
                }
                else if (mainCollider != null) {
                    mainCollider.enabled = true;
                }
            }

            if (disableCharacterControllerOnDeath && _character != null) {
                _character.enabled = true;
            }

            if (changeLayerOnDeath && mainRenderer != null) {
                mainRenderer.sortingLayerName = _layerOnAlive;
            }

            SetAdditionalComponents(true);
        }


        private void SetAdditionalComponents(bool isEnabled) {
            if (additionalComponents == null) return;
            foreach (var component in additionalComponents) {
                if (component != null) component.enabled = isEnabled;
            }
        }
    }
}