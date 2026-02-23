using System;
using System.Collections.Generic;
using System.Linq;
using Core.Behaviour;
using Gameplay.Health;
using UnityEngine;
using Utils;

namespace Gameplay.Character.Weapon {
    /// <summary>
    /// 충돌 감지 및 OnHit 이벤트를 발생시키는 센서 컴포넌트.
    /// 실제 데미지 계산은 YisoWeaponInstance에서 처리.
    /// </summary>
    [AddComponentMenu("Yiso/Weapon/Damage On Touch")]
    [RequireComponent(typeof(Collider2D))]
    public class YisoDamageOnTouch : RunIBehaviour {
        [Header("Target Settings")]
        [Tooltip("데미지를 입힐 대상 레이어")]
        [SerializeField] private LayerMask targetLayers = -1;

        [Tooltip("한 번 활성화 시 여러 대상을 공격할지 여부")]
        [SerializeField] private bool canHitMultipleTargets = false;

        /// <summary>
        /// 충돌 발생 시 호출되는 이벤트. (히트된 GameObject, 히트 포인트)
        /// WeaponInstance에서 구독하여 데미지 계산 및 적용.
        /// </summary>
        public event Action<GameObject, Vector3> OnHit;

        private Collider2D _collider;
        private GameObject _owner; // 무기 소유자 (공격자)
        private HashSet<GameObject> _hitTargets = new HashSet<GameObject>();

        protected override void Awake() {
            base.Awake();
            _collider = GetComponent<Collider2D>();
            // 기본적으로 비활성화 상태로 시작
            DisableDamage();
        }

        private void OnTriggerEnter2D(Collider2D other) {
            ProcessCollision(other);
        }

        private void OnTriggerStay2D(Collider2D other) {
            ProcessCollision(other);
        }

        private void ProcessCollision(Collider2D other) {
            // 레이어 체크
            if (((1 << other.gameObject.layer) & targetLayers) == 0) {
                return;
            }

            // 자기 자신 및 자기 자신의 자식은 공격하지 않음 (자해 방지)
            if (_owner != null && IsChildOf(other.gameObject, _owner)) {
                return;
            }

            // 최초로 맞은 대상만 공격
            if (!canHitMultipleTargets && _hitTargets.Count > 0)
                return;

            // Health 컴포넌트 확인 (Hurtbox 우선, 없으면 직접 찾기)
            var health = FindHealth(other);
            if (health == null || health.IsDead) {
                return;
            }
            
            // health.gameObject 기준으로 추적 -> 적에 콜라이더가 여러 개여도 한 번만 맞음
            var healthOwner = health.gameObject;
            if (_hitTargets.Contains(healthOwner)) {
                return;
            }

            // 히트 포인트 계산
            var hitPoint = other.ClosestPoint(transform.position);

            // OnHit 이벤트 발생 (WeaponInstance에서 데미지 계산)
            OnHit?.Invoke(other.gameObject, hitPoint);

            // 히트 대상 기록 (health 오너 기준)
            _hitTargets.Add(healthOwner);
        }

        /// <summary>
        /// Collider에서 YisoEntityHealth를 찾음
        /// YisoHurtbox가 있으면 우선 사용하고, 없으면 직접 GetComponent로 찾음
        /// </summary>
        private YisoEntityHealth FindHealth(Collider2D collider) {
            // 1. Hurtbox 컴포넌트 확인 (피격 판정 영역 분리된 경우)
            var hurtbox = collider.GetComponent<YisoHurtbox>();
            if (hurtbox != null) {
                return hurtbox.Health;
            }

            // 2. 직접 Health 컴포넌트 확인 (기존 방식, 하위 호환)
            return collider.GetComponent<YisoEntityHealth>();
        }
        
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

        #region Public API
        
        public void EnableDamage() {
            if (_collider != null) {
                _collider.enabled = true;
            }
        }
        
        public void DisableDamage() {
            if (_collider != null) {
                _collider.enabled = false;
            }
            // 히트 기록 초기화
            _hitTargets.Clear();
        }

        public void SetOwner(GameObject owner) {
            _owner = owner;
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            if (_collider == null) {
                _collider = GetComponent<Collider2D>();
            }

            if (_collider == null) return;

            var isActive = _collider.enabled;
            var fillColor = isActive ? new Color(1f, 0f, 0f, 0.3f) : new Color(1f, 0f, 0f, 0f);
            var wireColor = Color.red;
            
            Utils.YisoDebugUtils.DrawGizmoCollider2D(_collider, fillColor, wireColor);
        }
#endif
    }
}
