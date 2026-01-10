using Core.Behaviour;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils;

namespace Gameplay.Core {
    interface IPhysicsControllable {
        void Impact(Vector2 direction, float force);
        void SetCollisionsEnabled(bool state);
        void SetMovementEnabled(bool state);
        void SetMovement(Vector2 movement);
    }
    
    [RequireComponent(typeof(Rigidbody2D))]
    public class TopDownController: RunIBehaviour, IPhysicsControllable {
        [ReadOnly] public Vector2 currentMovement; // 외부(Input)에서 전달된 목표 이동 벡터
        
        private Rigidbody2D _rigidbody2D;
        private Collider2D[] _collider2Ds;

        private bool _freeMovement = true; // 외부에서 조작 가능한 상태인지 여부
        private bool _allowImpact = true; // 외부에서 받은 물리 충격 적용 가능한 상태인지 여부
        private Vector2 _impactForce; // 외부 힘에 의해 받는 힘
        private Vector2 _surfaceForce; // 마찰력에 의해 받는 힘
        private float _friction = 0f; // 현재 표면의 마찰력

        public bool FreeMovement {
            get => _freeMovement;
            private set {
                _rigidbody2D.linearVelocity = Vector2.zero;
                _impactForce = Vector2.zero;
                _surfaceForce = Vector2.zero;
                _freeMovement = value;
            }
        }

        public bool AllowImpact {
            get => _allowImpact;
            private set {
                _rigidbody2D.linearVelocity = Vector2.zero;
                _impactForce = Vector2.zero;
                _surfaceForce = Vector2.zero;
                _allowImpact = value;
            }
        }

        private const float ImpactThreshold  = 0.2f;
        private const float ImpactFalloffRate  = 5f; // Impact 감쇠 비율 (클수록 빨리 감쇠)
        
        protected override void Awake() {
            base.Awake();

            _rigidbody2D = GetComponent<Rigidbody2D>();
            _collider2Ds = GetComponents<Collider2D>();
        }

        public override void OnFixedUpdate() {
            base.OnFixedUpdate();

            if (AllowImpact) {
                ApplyImpact();
            }

            // 넉백이 충분히 크면(Threshold 이상) 움직이지 못하게끔. (넉백이 상쇄되지 않게)
            // 미미한 넉백(Threshold 이하)은 무시하고 이동 허용
            if (!FreeMovement || _impactForce.magnitude > ImpactThreshold) return;

            if (_friction > 1f) {
                currentMovement /= _friction;
            }
            if (_friction is > 0f and < 1f) {
                currentMovement = Vector2.Lerp(currentMovement, Vector2.zero, 1 - _friction);
            }

            var finalMovement = (currentMovement + _surfaceForce) * Time.fixedDeltaTime;
            // YisoLogger.Log($"물리 이동: currentMovement={currentMovement}, surfaceForce={_surfaceForce}, finalMovement={finalMovement}");
            MovePosition(_rigidbody2D.position + finalMovement);
        }

        public void SetCollisionsEnabled(bool state) {
            foreach (var collider2D in _collider2Ds) {
                collider2D.enabled = state;
            }
        }

        public void SetMovementEnabled(bool state) {
            FreeMovement = state;
        }

        /// <summary>
        /// 특정 벡터로 이동 (선 입력)
        /// 기본 이동용
        /// </summary>
        /// <param name="movement"></param>
        public void SetMovement(Vector2 movement) {
            currentMovement = movement;
        }
        
        /// <summary>
        /// 특정 위치로 이동할때 쓰는 함수 (실제 이동)
        /// 내부 상태와 독립적으로 적용됨
        /// 스킬 대쉬, 순간 이동 등
        /// </summary>
        /// <param name="targetPosition"></param>
        public void MovePosition(Vector2 targetPosition) {
            if (_rigidbody2D.bodyType != RigidbodyType2D.Static) {
                _rigidbody2D.MovePosition(targetPosition);
            }
        }

        /// <summary>
        /// 마찰 적용
        /// </summary>
        public void SetFriction(float friction, Vector2 surfaceForce) {
            _friction = friction;
            _surfaceForce = surfaceForce;
        }

        /// <summary>
        /// 외부에서 받은 힘 처리 (선 입력)
        /// 누적되는 구조 (매 프레임마다 감쇠됨)
        /// 넉백 등
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="force"></param>
        public void Impact(Vector2 direction, float force) {
            if (!AllowImpact) return;
            _impactForce += direction.normalized * force;
        }

        /// <summary>
        /// Impact함수로 입력받은 impact를 rigidbody에 직접 적용 (후 처리)
        /// </summary>
        private void ApplyImpact() {
            if (_impactForce.magnitude > ImpactThreshold) {
                _rigidbody2D.AddForce(_impactForce);
            }
            _impactForce = Vector2.Lerp(_impactForce, Vector2.zero, Time.fixedDeltaTime * ImpactFalloffRate);
        }
    }
}