using System.Collections.Generic;
using UnityEngine;
using CustomCollider2D = Gameplay.Tools.Collider.CustomCollider2D;

namespace Gameplay.Tools.Collider {
    [AddComponentMenu("Physics 2D/Arc Collider 2D")]
    [RequireComponent(typeof(PolygonCollider2D))]
    public class ArcCollider2D : CustomCollider2D {
        [Range(1, 25), HideInInspector, SerializeField]
        private float radius = 3;

        public float Radius {
            get { return radius; }
            set {
                radius = value;
                UpdateCollider();
            }
        }

        [Range(1, 25), HideInInspector, SerializeField]
        private float thickness = 0.4f;

        public float Thickness {
            get { return thickness; }
            set {
                thickness = value;
                UpdateCollider();
            }
        }

        [Range(0, 360), SerializeField] private float totalAngle = 360;

        public float TotalAngle {
            get { return totalAngle; }
            set {
                totalAngle = value;
                UpdateCollider();
            }
        }

        [Range(0, 360), SerializeField] private float offsetRotation = 0;

        public float OffsetRotation {
            get { return offsetRotation; }
            set {
                offsetRotation = value;
                UpdateCollider();
            }
        }

        [Header("Let there be Pizza"), SerializeField]
        private bool pizzaSlice;

        public bool PizzaSlice {
            get { return pizzaSlice; }
            set {
                pizzaSlice = value;
                UpdateCollider();
            }
        }

        [HideInInspector] public bool advanced = false;

        public override Vector2[] GetPoints() {
            var points = new List<Vector2>();

            float ang = offsetRotation;

            if (pizzaSlice && totalAngle % 360 != 0) {
                points.Add(Vector2.zero);
            }

            for (int i = 0; i <= smoothness; i++) {
                var x = radius * Mathf.Cos(ang * Mathf.Deg2Rad);
                var y = radius * Mathf.Sin(ang * Mathf.Deg2Rad);

                points.Add(new Vector2(x, y));
                ang += (float) totalAngle / smoothness;
            }

            if (!pizzaSlice) {
                for (var i = 0; i <= smoothness; i++) {
                    ang -= (float) totalAngle / smoothness;
                    var x = (radius - thickness) * Mathf.Cos(ang * Mathf.Deg2Rad);
                    var y = (radius - thickness) * Mathf.Sin(ang * Mathf.Deg2Rad);

                    points.Add(new Vector2(x, y));
                }
            }

            return points.ToArray();
        }
    }
}