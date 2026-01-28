using Core.Behaviour;
using UnityEngine;

namespace Gameplay.Health {
    /// <summary>
    /// 피격 판정용 히트박스 컴포넌트.
    /// 캐릭터의 자식 오브젝트에 배치하여 물리 충돌 영역과 피격 판정 영역을 분리합니다.
    ///
    /// 사용 예:
    /// - Body Collider (isTrigger=false): 캐릭터 간 물리 충돌용 (발 부분)
    /// - Hurtbox Collider (isTrigger=true): 피격 판정용 (몸통 전체)
    ///
    /// 투사체나 근접 공격은 Hurtbox와 충돌하면 이 컴포넌트를 통해 YisoEntityHealth에 접근합니다.
    /// </summary>
    [AddComponentMenu("Yiso/Health/Hurtbox")]
    [RequireComponent(typeof(Collider2D))]
    public class YisoHurtbox : RunIBehaviour {
        private YisoEntityHealth _health;

        /// <summary>
        /// 이 Hurtbox와 연결된 YisoEntityHealth를 반환합니다.
        /// </summary>
        public YisoEntityHealth Health => _health;

        /// <summary>
        /// 연결된 Health가 죽었는지 여부
        /// </summary>
        public bool IsDead => _health == null || _health.IsDead;

        protected override void Awake() {
            base.Awake();
            // Health가 설정되지 않았으면 부모에서 찾기
            if (_health == null) {
                _health = GetComponentInParent<YisoEntityHealth>();
            }

            if (_health == null) {
                Debug.LogWarning($"[YisoHurtbox] {gameObject.name}: YisoEntityHealth를 찾을 수 없습니다. 부모 계층에 YisoEntityHealth가 있는지 확인하세요.");
            }

            // Collider가 Trigger 모드인지 확인
            var collider = GetComponent<Collider2D>();
            if (collider != null && !collider.isTrigger) {
                Debug.LogWarning($"[YisoHurtbox] {gameObject.name}: Collider의 IsTrigger가 false입니다. 피격 판정을 위해 true로 설정하는 것을 권장합니다.");
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            var collider = GetComponent<Collider2D>();
            if (collider == null) return;

            // Hurtbox 영역을 초록색으로 표시
            var fillColor = new Color(0f, 1f, 0f, 0.2f);
            var wireColor = Color.green;
            Utils.YisoDebugUtils.DrawGizmoCollider2D(collider, fillColor, wireColor);
        }
#endif
    }
}
