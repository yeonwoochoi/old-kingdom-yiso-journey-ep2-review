using System;
using System.Collections;
using Core.Behaviour;
using Gameplay.Health;
using UnityEngine;
using Utils;

namespace Gameplay.Character.Weapon {
    /// <summary>
    /// 투사체 컴포넌트.
    /// YisoProjectileAttackAbility에서 생성되어 날아가며, 충돌 시 데미지를 처리합니다.
    /// 프리팹에 Collider2D (IsTrigger)와 함께 사용됩니다.
    /// </summary>
    [AddComponentMenu("Yiso/Weapon/Projectile")]
    [RequireComponent(typeof(Collider2D))]
    public class YisoProjectile : RunIBehaviour {
        [Header("Visual")]
        [Tooltip("투사체가 이동 방향을 바라보도록 회전할지 여부")]
        [SerializeField] private bool rotateTowardDirection = true;

        [Tooltip("투사체 이펙트의 회전 오프셋 (도)")]
        [SerializeField] private float rotationOffset = 0f;

        [Header("Collision")]
        [Tooltip("충돌 시 투사체 파괴 여부")]
        [SerializeField] private bool destroyOnHit = true;

        [Tooltip("벽/장애물 레이어 (충돌 시 파괴)")]
        [SerializeField] private LayerMask obstacleLayerMask = 0;

        [Header("Animator")]
        [Tooltip("벽에 박혔을 때 파괴 딜레이")]
        [SerializeField] private float destroyDelayOnWedged = 2f;

        // --- 런타임 데이터 (Initialize에서 설정됨) ---
        private GameObject _owner;
        private Vector2 _direction;
        private float _speed;
        private float _maxRange;
        private float _damage;
        private LayerMask _targetLayerMask;
        private Animator _animator;

        private Vector3 _startPosition;
        private Collider2D _collider;
        private bool _isInitialized = false;
        
        private const string k_IsWedged = "Wedged";
        private int _isWedgedHash;

        /// <summary>
        /// 충돌 발생 시 호출되는 이벤트 (외부에서 추가 처리 가능).
        /// </summary>
        public event Action<GameObject, Vector3> OnHit;

        protected override void Awake() {
            base.Awake();
            _collider = GetComponent<Collider2D>();

            // Trigger 모드 강제
            if (_collider != null) {
                _collider.isTrigger = true;
            }
            
            _animator = GetComponent<Animator>();
            _isWedgedHash = Animator.StringToHash(k_IsWedged);

            if (_animator != null) {
                _animator.SetBool(_isWedgedHash, false);
            }
        }

        /// <summary>
        /// 투사체를 초기화합니다. Ability에서 Spawn 후 호출해야 합니다.
        /// </summary>
        /// <param name="owner">발사한 캐릭터의 GameObject</param>
        /// <param name="direction">발사 방향 (정규화됨)</param>
        /// <param name="speed">투사체 속도</param>
        /// <param name="maxRange">최대 사거리</param>
        /// <param name="damage">데미지</param>
        /// <param name="targetLayerMask">타겟 레이어</param>
        public void Initialize(GameObject owner, Vector2 direction, float speed, float maxRange, float damage, LayerMask targetLayerMask) {
            _owner = owner;
            _direction = direction.normalized;
            _speed = speed;
            _maxRange = maxRange;
            _damage = damage;
            _targetLayerMask = targetLayerMask;
            _startPosition = transform.position;
            _isInitialized = true;

            // 방향으로 회전
            if (rotateTowardDirection) {
                float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
            }
        }

        public override void OnUpdate() {
            base.OnUpdate();

            if (!_isInitialized) return;

            // 이동
            transform.position += (Vector3)(_direction * _speed * Time.deltaTime);

            // 최대 사거리 체크
            float distanceTraveled = Vector3.Distance(_startPosition, transform.position);
            if (distanceTraveled >= _maxRange) {
                DestroyProjectile();
            }
        }

        private void OnTriggerEnter2D(Collider2D other) {
            if (!_isInitialized) return;

            // 자기 자신 및 자신의 자식은 무시
            if (_owner != null && IsChildOf(other.gameObject, _owner)) {
                return;
            }

            // 장애물 충돌 체크
            if (((1 << other.gameObject.layer) & obstacleLayerMask) != 0) {
                DestroyProjectile(destroyDelayOnWedged);
                return;
            }

            // 타겟 레이어 체크
            if (((1 << other.gameObject.layer) & _targetLayerMask) == 0) {
                return;
            }

            // Health 컴포넌트 확인
            var health = other.GetComponent<YisoEntityHealth>();
            if (health == null || health.IsDead) {
                return;
            }

            // 히트 포인트 계산
            var hitPoint = other.ClosestPoint(transform.position);

            // 데미지 적용
            ApplyDamage(other.gameObject, health, hitPoint);

            // 이벤트 발생
            OnHit?.Invoke(other.gameObject, hitPoint);

            // 충돌 시 파괴
            if (destroyOnHit) {
                DestroyProjectile(destroyDelayOnWedged);
            }
        }

        /// <summary>
        /// 대상에게 데미지를 적용합니다.
        /// </summary>
        private void ApplyDamage(GameObject target, YisoEntityHealth health, Vector3 hitPoint) {
            var damageInfo = new DamageInfo {
                FinalDamage = _damage,
                Attacker = _owner,
                DamageDirection = _direction,
                HitPoint = hitPoint,
                KnockbackForce = 0f, // TODO: 필요 시 설정에서 받아오기
                IsCritical = false // TODO: 필요 시 크리티컬 계산
            };

            health.TakeDamage(damageInfo);
        }

        /// <summary>
        /// target이 owner의 자식인지 확인 (자해 방지).
        /// </summary>
        private bool IsChildOf(GameObject target, GameObject owner) {
            if (target == owner) return true;

            var current = target.transform;
            while (current != null) {
                if (current.gameObject == owner) {
                    return true;
                }
                current = current.parent;
            }

            return false;
        }
        
        /// <summary>
        /// 투사체를 파괴합니다.
        /// </summary>
        private void DestroyProjectile(float delay = 0f) {
            if (delay > 0f) {
                StartCoroutine(DestroyProjectileCo(delay));
                return;
            }
            // TODO: 오브젝트 풀링 사용 시 Pool.Return(this) 호출
            Destroy(gameObject);
        }

        private IEnumerator DestroyProjectileCo(float delay) {
            _animator?.SetBool(_isWedgedHash, true);
            yield return new WaitForSeconds(delay);
            // TODO: 오브젝트 풀링 사용 시 Pool.Return(this) 호출
            Destroy(gameObject);
        }


#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            // 최대 사거리 시각화
            Gizmos.color = Color.yellow;
            if (_isInitialized) {
                Gizmos.DrawWireSphere(_startPosition, _maxRange);
            }
        }
#endif
    }
}
