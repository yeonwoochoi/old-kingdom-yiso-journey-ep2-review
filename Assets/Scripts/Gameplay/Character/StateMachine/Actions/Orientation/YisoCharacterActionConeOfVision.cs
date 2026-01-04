using Gameplay.Tools.Visual;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Actions.Orientation {
    public class YisoCharacterActionConeOfVision: YisoCharacterAction {
        [Title("Visual Settings")]
        [SerializeField] private float viewRadius = 5f;
        [SerializeField, Range(0, 360)] private float viewAngle = 90f;
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private Color viewColor = new Color(1, 0, 0, 0.3f);
        
        [Title("Offset Settings")]
        [SerializeField] private Vector3 offset = Vector3.zero;
        
        [Title("Performance")]
        [SerializeField, Range(1, 10)] private int resolution = 10;
        
        private YisoFieldOfViewRenderer _fovRenderer;

        protected override void Awake() {
            base.Awake();
            // 렌더러 찾기 시도 (같은 오브젝트 혹은 자식/부모)
            if (StateMachine != null)
            {
                _fovRenderer = StateMachine.GetComponentInChildren<YisoFieldOfViewRenderer>();
                
                // 없으면 런타임에 추가 (선택사항, 에러 로그가 나을 수도 있음)
                if (_fovRenderer == null)
                {
                    Debug.Log($"[ConeOfVision] Renderer not found on {StateMachine.name}. Creating new one.");
                    var go = new GameObject("FOV_Renderer");
                    go.transform.SetParent(StateMachine.transform);
                    go.transform.localPosition = Vector3.zero;
                    _fovRenderer = go.AddComponent<YisoFieldOfViewRenderer>();
                }
            }
        }

        public override void OnEnterState() {
            if (_fovRenderer == null) return;

            // 렌더러 활성화 및 설정 주입
            _fovRenderer.enabled = true;
            _fovRenderer.SetSettings(viewRadius, viewAngle, obstacleMask, viewColor, offset, resolution);
            
            // MeshRenderer 컴포넌트도 켜줌
            var meshRenderer = _fovRenderer.GetComponent<MeshRenderer>();
            if (meshRenderer != null) meshRenderer.enabled = true;
            
            // 진입 시 방향 동기화 (깜빡임 방지)
            UpdateRendererDirection();

        }

        public override void PerformAction() {
            UpdateRendererDirection();
        }

        public override void OnExitState() {
            if (_fovRenderer == null) return;

            // 상태를 나가면 시야 끄기
            _fovRenderer.enabled = false;
            
            var meshRenderer = _fovRenderer.GetComponent<MeshRenderer>();
            if (meshRenderer != null) meshRenderer.enabled = false;
        }

        private void UpdateRendererDirection() {
            if (_fovRenderer == null || StateMachine.Owner == null) return;
            var currentDir = StateMachine.Owner.FacingDirectionVector;
            _fovRenderer.SetAimDirection(currentDir);
        }
    }
}