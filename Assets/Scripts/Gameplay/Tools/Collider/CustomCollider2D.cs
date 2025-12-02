using Core.Behaviour;
using UnityEngine;

namespace Gameplay.Tools.Collider {
    [RequireComponent(typeof(PolygonCollider2D))]
    public abstract class CustomCollider2D : RunIBehaviour {
        [Range(1, 90), SerializeField] protected int smoothness = 24;

        public int Smoothness {
            get => smoothness;
            set {
                smoothness = value;
                UpdateCollider();
            }
        }

        public abstract Vector2[] GetPoints();

        protected void UpdateCollider() {
            GetComponent<PolygonCollider2D>().points = GetPoints();
        }
    }
}