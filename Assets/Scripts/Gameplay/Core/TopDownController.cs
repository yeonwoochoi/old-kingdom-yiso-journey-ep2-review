using Core.Behaviour;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Core {
    [RequireComponent(typeof(Rigidbody2D))]
    public class TopDownController: RunIBehaviour {
        [ReadOnly] public Vector3 speed; // 한 Frame당 얼마나 이동했는지
        [ReadOnly] public Vector3 currentMovement; // 외부(Input)에서 전달된 목표 이동 벡터
        [ReadOnly] public Vector3 currentDirection; // currentMovement의 방향 벡터 (Normalized)
        [ReadOnly] public float friction; // 현재 표면의 마찰력
        [ReadOnly] public Vector3 addedForce; // 표면 등에 의한 추가적인 힘 (Dash)
        [ReadOnly] public bool freeMovement = true; // 외부에서 조작 가능한 상태인지 여부
        
        private Rigidbody2D _rigidbody2D;
        private Collider2D _collider2D;
        protected BoxCollider2D _boxCollider2D;
        protected CapsuleCollider2D _capsuleCollider2D;
        protected CircleCollider2D _circleCollider2D;
        
        protected override void Awake() {
            base.Awake();
            
        }
    }
}