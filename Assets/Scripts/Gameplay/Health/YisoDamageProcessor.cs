using Core.Behaviour;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Health {
    /// <summary>
    /// Core Service에서 계산된 DamageInfo를 받아, 이 개체의 현재 상태(무적 여부 등)에 따라
    /// 데미지를 최종적으로 가공하거나 거부하는 '관문' 역할을 한다.
    /// </summary>
    [AddComponentMenu("Yiso/Health/Damage Processor")]
    public class YisoDamageProcessor : RunIBehaviour {
        [Title("Invulnerability Settings")] [Tooltip("체크 시 이 개체는 영구적으로 모든 피해를 받지 않습니다.")] [SerializeField]
        private bool isPermanentlyInvulnerable = false;

        [Tooltip("피격 후 일시적으로 무적이 되는 시간입니다. 0으로 설정 시 이 기능을 사용하지 않습니다.")] [SerializeField]
        private float invincibilityDurationOnHit = 0.3f;

        private YisoEntityHealth _entityHealth;
        private float _lastHitTimestamp = -1f;

        public bool IsInvulnerable =>
            Time.time - _lastHitTimestamp < invincibilityDurationOnHit || isPermanentlyInvulnerable;

        protected override void Awake() {
            base.Awake();
            _entityHealth = GetComponentInParent<YisoEntityHealth>();
        }

        public float FinalizeDamage(DamageInfo damageInfo) {
            if (!CanTakeDamage()) {
                return 0f;
            }

            if (invincibilityDurationOnHit > 0f) {
                _lastHitTimestamp = Time.time;
            }

            return damageInfo.FinalDamage;
        }

        public bool CanTakeDamage() {
            if (_entityHealth == null || _entityHealth.IsDead) return false;
            if (IsInvulnerable) return false;
            return true;
        }
    }
}