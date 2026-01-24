using System.Collections.Generic;
using Core.Behaviour;
using UnityEngine;

namespace Gameplay.Tools.Visual {
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class YisoFieldOfViewRenderer : RunIBehaviour {
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _viewMesh;

        // Action에서 주입받을 설정값
        private float _viewRadius;
        private float _viewAngle;
        private LayerMask _obstacleMask;
        private Vector3 _offset; // 눈의 위치 보정

        private int _meshResolution = 2; // 1도당 레이캐스트 횟수
        private bool _isDirty = false; // 설정 변경 플래그
        private Vector2 _aimDirection = Vector2.down;

        protected override void Awake() {
            base.Awake();
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            _viewMesh = new Mesh();
            _viewMesh.name = "View Mesh";
            _meshFilter.mesh = _viewMesh;
        }

        public void SetAimDirection(Vector2 direction) {
            if (direction != Vector2.zero) {
                _aimDirection = direction.normalized;
            }
        }

        public override void OnLateUpdate() {
            base.OnLateUpdate();

            // 활성화 상태에서만 그리기
            if (enabled) DrawFieldOfView();
        }

        public void SetSettings(float radius, float angle, LayerMask obstacleMask, Color color, Vector3 offset,
            int resolution) {
            _viewRadius = radius;
            _viewAngle = angle;
            _obstacleMask = obstacleMask;
            _offset = offset;
            _meshResolution = resolution;

            if (_meshRenderer.material == null) {
                // 기본 재질이 없으면 생성 (스프라이트 쉐이더 등 사용 권장)
                _meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }

            // Material 복제 방지를 위해 material 대신 sharedMaterial 사용 고려, 
            // 여기서는 색상 변경을 위해 인스턴스 사용
            _meshRenderer.material.color = color;

            _isDirty = true;
        }

        private void DrawFieldOfView() {
            // 1. 단계 수 계산
            var stepCount = Mathf.RoundToInt(_viewAngle * _meshResolution);
            var stepAngleSize = _viewAngle / stepCount;
            
            // 2. 바라보는 각도 계산 (Vector -> Degree)
            // Atan2는 (y, x) 순서이며 라디안을 반환하므로 Deg2Deg 곱하기
            var currentAimAngle = Mathf.Atan2(_aimDirection.y, _aimDirection.x) * Mathf.Rad2Deg;
            
            // 3. 시작 각도 설정 (부채꼴의 가장 오른쪽/아래쪽 끝)
            var currentAngle = currentAimAngle - _viewAngle / 2;

            // 4. 원점(눈 위치) 계산
            // 캐릭터의 Transform 회전 대신, 현재 바라보는 각도(currentAimAngle)를 기준으로 오프셋 회전
            var rotatedOffset = Quaternion.Euler(0, 0, currentAimAngle) * _offset;
            var origin = transform.position + rotatedOffset;

            // 5. 점 데이터 수집 (GC 최소화를 위해 List 대신 배열 사용 권장하지만, 가독성을 위해 일단 리스트 유지)
            // 성능 민감 시 멤버 변수로 List<Vector3> _viewPoints 선언 후 Clear() 해서 재사용 권장
            var viewPoints = new List<Vector3>(); 

            for (var i = 0; i <= stepCount; i++) {
                var dir = DirFromAngle(currentAngle); 

                // Raycast 수행
                var hit = Physics2D.Raycast(origin, dir, _viewRadius, _obstacleMask);

                if (hit.collider != null) {
                    viewPoints.Add(hit.point);
                }
                else {
                    viewPoints.Add(origin + dir * _viewRadius);
                }

                currentAngle += stepAngleSize;
            }
            
            // 6. Mesh 데이터 생성
            var vertexCount = viewPoints.Count + 1;
            var vertices = new Vector3[vertexCount];
            var triangles = new int[(vertexCount - 2) * 3];

            // 7. 좌표 변환 (World -> Local)
            // MeshFilter는 로컬 좌표계를 쓰므로 InverseTransformPoint 필수
            vertices[0] = transform.InverseTransformPoint(origin);

            for (var i = 0; i < vertexCount - 1; i++) {
                vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

                if (i < vertexCount - 2) {
                    triangles[i * 3] = 0;
                    triangles[i * 3 + 1] = i + 1;
                    triangles[i * 3 + 2] = i + 2;
                }
            }

            // 8. Mesh 적용
            _viewMesh.Clear();
            _viewMesh.vertices = vertices;
            _viewMesh.triangles = triangles;
            _viewMesh.RecalculateBounds();
        }

        private Vector3 DirFromAngle(float angleInDegrees) {
            return new Vector3(Mathf.Cos(angleInDegrees * Mathf.Deg2Rad), Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0);
        }
    }
}