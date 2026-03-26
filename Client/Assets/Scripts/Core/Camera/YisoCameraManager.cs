using Core.Event;
using Core.Scene;
using Core.Singleton;
using Unity.Cinemachine;
using UnityEngine;

namespace Core {
    /// <summary>
    /// [역할] 씬 타입에 따른 카메라 동작 관리
    /// [책임]
    ///   - 플레이어 Transform 추적 (데드존, 스무딩)
    ///   - SceneTransitionEvent 수신 → 씬 타입에 맞게 동작 전환
    ///   - 컷씬 플로우에 따른 카메라 이동
    ///   - 카메라 흔들림 (Camera Shake)
    ///   - Area Trigger 기반 Boundary 제한
    ///   - Orthographic Size 조절 (Zoom In/Out)
    /// [타입] MonoSingleton (Camera.main 관리, 코루틴 필요)
    /// [API] SetTarget / MoveToPosition / ReleaseControl / Shake / ZoomTo / SetBoundary / ClearBoundary
    /// </summary>
    public class YisoCameraManager : YisoMonoSingleton<YisoCameraManager>, IYisoEventListener<SceneTransitionCompleteEvent> {
        private Camera _mainCamera;
        private CinemachineCamera _virtualCamera;
        private CinemachineBasicMultiChannelPerlin _perlin;
        private CinemachineConfiner2D _confiner;

        private bool _isShaking = false;
        private float _lastStartShakeTime;
        private float _shakeDuration;

        private bool _isZooming = false;
        private float _targetZoomSize = 0f;
        private float _zoomSpeed = 0f;

        public Transform Target { get; private set; }

        protected override void Awake() {
            base.Awake();
            _mainCamera = Camera.main;
            _virtualCamera = FindFirstObjectByType<CinemachineCamera>();
            if (_virtualCamera != null) {
                _perlin = _virtualCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
                _confiner = _virtualCamera.GetComponent<CinemachineConfiner2D>();
            }
            ResetData();
        }

        private void Update() {
            if (_perlin != null && _isShaking && UnityEngine.Time.time - _lastStartShakeTime >= _shakeDuration) {
                _isShaking = false;
                StopShake();
            }

            if (_isZooming && _virtualCamera != null) {
                _virtualCamera.Lens.OrthographicSize = Mathf.Lerp(_virtualCamera.Lens.OrthographicSize, _targetZoomSize, UnityEngine.Time.deltaTime * _zoomSpeed);

                if (Mathf.Abs(_targetZoomSize - _virtualCamera.Lens.OrthographicSize) <= 0.01f) {
                    _virtualCamera.Lens.OrthographicSize = _targetZoomSize;
                    _isZooming = false;
                    _confiner.InvalidateLensCache(); // 내부 캐시 삭제
                }
            }
        }

        public void SetTarget(Transform target) {
            if (_virtualCamera == null)
                return;

            Target = target;
            _virtualCamera.Follow = Target;
        }

        public void MoveToPosition(Vector3 position) {
            if (_virtualCamera == null)
                return;

            _virtualCamera.Follow = null;
            _virtualCamera.ForceCameraPosition(position, _virtualCamera.transform.rotation);
        }

        public void ReleaseControl() {
            if (_virtualCamera == null) return;
            _virtualCamera.Follow = Target;
        }

        public void Shake(float amplitude, float frequency, float duration) {
            if (_perlin == null)
                return;

            _perlin.AmplitudeGain = amplitude;
            _perlin.FrequencyGain = frequency;

            _shakeDuration = duration;
            _lastStartShakeTime = UnityEngine.Time.time;
            _isShaking = true;
        }

        private void StopShake() {
            if (_perlin == null)
                return;

            _perlin.AmplitudeGain = 0f;
            _perlin.FrequencyGain = 0f;
        }

        public void ZoomTo(float orthographicSize, float zoomSpeed) {
            if (_virtualCamera == null)
                return;
            _targetZoomSize = orthographicSize;
            _isZooming = true;
            _zoomSpeed = zoomSpeed;
        }

        public void SetBoundary(Collider2D boundary) {
            if (_confiner == null)
                return;

            _confiner.BoundingShape2D = boundary;
            _confiner.InvalidateBoundingShapeCache();
        }

        public void ClearBoundary() {
            if (_confiner == null)
                return;

            _confiner.BoundingShape2D = null;
            _confiner.InvalidateBoundingShapeCache();
        }

        private void ResetData() {
            _isShaking = false;
            _lastStartShakeTime = 0f;
            _shakeDuration = 0f;

            _isZooming = false;
            _targetZoomSize = 0f;
        }

        public void OnEvent(SceneTransitionCompleteEvent args) {
            _virtualCamera = FindFirstObjectByType<CinemachineCamera>();
            if (_virtualCamera != null) {
                _perlin = _virtualCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
                _confiner = _virtualCamera.GetComponent<CinemachineConfiner2D>();
                if (Target != null) _virtualCamera.Follow = Target;
            }
            ResetData();
        }

        private void OnEnable() {
            this.StartListening();
        }

        private void OnDisable() {
            this.StopListening();
        }
    }
}
