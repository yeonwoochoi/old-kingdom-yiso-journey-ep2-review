#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Gameplay.Tools.Collider.Editor {
    [CustomEditor(typeof(ArcCollider2D))]
    public class ArcCollider2DEditor : UnityEditor.Editor {
        private ArcCollider2D arc;
        private PolygonCollider2D poly;
        private Vector2 off;

        private void OnEnable() {
            arc = (ArcCollider2D) target;
            poly = arc.GetComponent<PolygonCollider2D>();

            if (poly == null) {
                poly = arc.gameObject.AddComponent<PolygonCollider2D>();
            }

            poly.points = arc.GetPoints();
        }

        public override void OnInspectorGUI() {
            GUI.changed = false;
            DrawDefaultInspector();

            arc.advanced = EditorGUILayout.Toggle("Advance", arc.advanced);
            if (arc.advanced) {
                arc.Radius = EditorGUILayout.FloatField("Radius", arc.Radius);
            }
            else {
                arc.Radius = EditorGUILayout.Slider("Radius", arc.Radius, 1, 25);
            }

            if (arc.PizzaSlice) {
                if (arc.advanced) {
                    arc.Thickness = EditorGUILayout.FloatField("Thickness", arc.Thickness);
                }
                else {
                    arc.Thickness = EditorGUILayout.Slider("Thickness", arc.Thickness, 1, 25);
                }
            }

            if (GUI.changed || !off.Equals(poly.offset)) {
                poly.points = arc.GetPoints();
            }

            off = poly.offset;
        }
    }
}
#endif