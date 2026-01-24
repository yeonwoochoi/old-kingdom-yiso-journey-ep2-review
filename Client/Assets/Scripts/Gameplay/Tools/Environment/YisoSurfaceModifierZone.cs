using Core.Behaviour;
using Gameplay.Core;
using UnityEngine;

namespace Gameplay.Tools.Environment {
    [AddComponentMenu("Yiso/Environment/Surface Modifier")]
    public class YisoSurfaceModifierZone: RunIBehaviour {
        [Tooltip("0~1 사이 값: 0이면 미끄럽고, 1이면 완전 감속")]
        public float friction;
        
        [Tooltip("지형/환경에서 캐릭터에 추가로 가해지는 힘")]
        public Vector2 surfaceForce;

        private void OnTriggerStay2D(Collider2D other) {
            if (other.TryGetComponent<TopDownController>(out var topDownController)) {
                topDownController.SetFriction(friction, surfaceForce);
            }
        }
        private void OnTriggerExit2D(Collider2D other) {
            if (other.TryGetComponent<TopDownController>(out var topDownController)) {
                topDownController.SetFriction(0f, Vector2.zero);
            }
        }
    }
}