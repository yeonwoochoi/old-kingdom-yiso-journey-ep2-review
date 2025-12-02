using UnityEngine;

namespace Utils {
    /// <summary>
    /// Unity Editor Gizmo 그리기 유틸리티 클래스.
    /// 디버깅 및 시각화를 위한 범용 메서드 제공.
    /// </summary>
    public static class YisoDebugUtils {
        #region Collider Gizmo Drawing

        /// <summary>
        /// Collider2D 타입에 따라 자동으로 적절한 Gizmo를 그립니다.
        /// - BoxCollider2D: 박스
        /// - CircleCollider2D: 원
        /// - CapsuleCollider2D: 캡슐
        /// - PolygonCollider2D: 폴리곤 (Arc Collider 등)
        /// </summary>
        /// <param name="collider">그릴 Collider2D</param>
        /// <param name="fillColor">채우기 색상 (알파 0이면 채우기 없음)</param>
        /// <param name="wireColor">테두리 색상</param>
        public static void DrawGizmoCollider2D(Collider2D collider, Color fillColor, Color wireColor) {
            if (collider == null) return;

            // Collider의 로컬 변환 행렬 적용
            var previousMatrix = Gizmos.matrix;
            Gizmos.matrix = collider.transform.localToWorldMatrix;

            // 타입에 따라 분기
            if (collider is BoxCollider2D boxCollider) {
                DrawGizmoBox(boxCollider.offset, boxCollider.size, fillColor, wireColor);
            }
            else if (collider is CircleCollider2D circleCollider) {
                DrawGizmoCircle(circleCollider.offset, circleCollider.radius, fillColor, wireColor);
            }
            else if (collider is CapsuleCollider2D capsuleCollider) {
                DrawGizmoCapsule(capsuleCollider.offset, capsuleCollider.size, fillColor, wireColor);
            }
            else if (collider is PolygonCollider2D polygonCollider) {
                DrawGizmoPolygon(polygonCollider, fillColor, wireColor);
            }

            // 행렬 복원
            Gizmos.matrix = previousMatrix;
        }

        #endregion

        #region Shape Drawing Helpers

        /// <summary>
        /// 박스 Gizmo를 그립니다 (채우기 + 테두리).
        /// </summary>
        private static void DrawGizmoBox(Vector3 center, Vector3 size, Color fillColor, Color wireColor) {
            // 채우기 (알파가 0보다 크면)
            if (fillColor.a > 0f) {
                Gizmos.color = fillColor;
                Gizmos.DrawCube(center, size);
            }

            // 테두리
            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(center, size);
        }

        /// <summary>
        /// 원형 Gizmo를 그립니다 (채우기 + 테두리).
        /// </summary>
        private static void DrawGizmoCircle(Vector3 center, float radius, Color fillColor, Color wireColor) {
            // 채우기 (알파가 0보다 크면)
            if (fillColor.a > 0f) {
                Gizmos.color = fillColor;
                Gizmos.DrawSphere(center, radius);
            }

            // 테두리 (와이어프레임 원)
            Gizmos.color = wireColor;
            DrawWireCircle(center, radius);
        }

        /// <summary>
        /// 캡슐 Gizmo를 그립니다 (박스로 근사).
        /// </summary>
        private static void DrawGizmoCapsule(Vector3 center, Vector2 size, Color fillColor, Color wireColor) {
            // 캡슐은 박스로 근사하여 그리기 (정확한 캡슐 그리기는 복잡하므로)
            DrawGizmoBox(center, size, fillColor, wireColor);
        }

        /// <summary>
        /// 폴리곤 Gizmo를 그립니다 (Arc Collider 등).
        /// PolygonCollider2D의 모든 패스를 순회하며 그립니다.
        /// </summary>
        private static void DrawGizmoPolygon(PolygonCollider2D polygonCollider, Color fillColor, Color wireColor) {
            // PolygonCollider2D는 여러 개의 path를 가질 수 있음
            for (int pathIndex = 0; pathIndex < polygonCollider.pathCount; pathIndex++) {
                var points = polygonCollider.GetPath(pathIndex);
                if (points.Length < 2) continue;

                // 테두리 그리기
                Gizmos.color = wireColor;
                for (int i = 0; i < points.Length; i++) {
                    var start = points[i];
                    var end = points[(i + 1) % points.Length]; // 마지막 점은 첫 점과 연결
                    Gizmos.DrawLine(start, end);
                }

                // 채우기 (간단한 삼각형 팬으로 근사)
                if (fillColor.a > 0f && points.Length >= 3) {
                    Gizmos.color = fillColor;
                    DrawPolygonFill(points);
                }
            }
        }

        /// <summary>
        /// 폴리곤 내부를 채웁니다 (Triangle Fan 방식으로 근사).
        /// </summary>
        private static void DrawPolygonFill(Vector2[] points) {
            if (points.Length < 3) return;

            // 중심점 계산
            var center = Vector2.zero;
            foreach (var point in points) {
                center += point;
            }

            center /= points.Length;

            // Triangle Fan으로 삼각형 그리기
            for (int i = 0; i < points.Length; i++) {
                var p1 = center;
                var p2 = points[i];
                var p3 = points[(i + 1) % points.Length];

                // Gizmos.DrawMesh가 없으므로 선으로 근사 (알파가 있으면 선을 여러 번 그려서 채워진 느낌)
                DrawTriangleFill(p1, p2, p3);
            }
        }

        /// <summary>
        /// 삼각형을 채웁니다 (선으로 근사).
        /// </summary>
        private static void DrawTriangleFill(Vector2 p1, Vector2 p2, Vector2 p3) {
            // 삼각형의 각 변을 그려서 채워진 느낌 근사
            // 실제로는 Gizmos.DrawMesh를 사용해야 하지만 2D에서는 제한적
            // 대신 중심에서 각 변으로 라인을 그려 채워진 느낌을 줌
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p1);
        }

        /// <summary>
        /// 와이어프레임 원을 그립니다.
        /// </summary>
        /// <param name="center">원의 중심</param>
        /// <param name="radius">반지름</param>
        /// <param name="segments">세그먼트 수 (기본값: 32)</param>
        public static void DrawWireCircle(Vector3 center, float radius, int segments = 32) {
            var angleStep = 360f / segments;
            var prevPoint = center + new Vector3(radius, 0, 0);

            for (var i = 1; i <= segments; i++) {
                var angle = i * angleStep * Mathf.Deg2Rad;
                var nextPoint = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0
                );
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }

        #endregion

        #region Arrow Drawing

        /// <summary>
        /// 방향 화살표를 그립니다.
        /// </summary>
        /// <param name="origin">화살표 시작점</param>
        /// <param name="direction">화살표 방향 (정규화 권장)</param>
        /// <param name="length">화살표 길이</param>
        /// <param name="arrowHeadSize">화살표 머리 크기</param>
        /// <param name="color">화살표 색상</param>
        public static void DrawGizmoArrow(Vector3 origin, Vector2 direction, float length, float arrowHeadSize,
            Color color) {
            Gizmos.color = color;

            // 화살표 본체
            var endPoint = origin + (Vector3) (direction.normalized * length);
            Gizmos.DrawLine(origin, endPoint);

            // 화살표 머리
            DrawArrowHead(endPoint, direction, arrowHeadSize);
        }

        /// <summary>
        /// 화살표 머리를 그립니다.
        /// </summary>
        private static void DrawArrowHead(Vector3 tip, Vector2 direction, float size) {
            // 화살표 각도 (150도)
            var headAngle = 150f;
            var headDirection1 = Quaternion.Euler(0, 0, headAngle) * (-direction);
            var headDirection2 = Quaternion.Euler(0, 0, -headAngle) * (-direction);

            var headPoint1 = tip + (Vector3) (headDirection1.normalized * size);
            var headPoint2 = tip + (Vector3) (headDirection2.normalized * size);

            Gizmos.DrawLine(tip, headPoint1);
            Gizmos.DrawLine(tip, headPoint2);
        }

        #endregion
    }
}