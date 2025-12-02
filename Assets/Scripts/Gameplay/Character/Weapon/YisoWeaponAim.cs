using Core.Behaviour;
using Gameplay.Character.Types;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils;

namespace Gameplay.Character.Weapon {
    /// <summary>
    /// 무기의 방향 상태를 관리하는 컴포넌트.
    /// - CurrentAim (Vector2): 정확한 입력 방향 벡터를 보유하여 히트박스 회전에 사용
    /// - CurrentDirection (FacingDirections): 4방향 Enum 상태 (필요시 참조용)
    ///
    /// 무기 비주얼(Sprite)은 Animator의 X, Y 파라미터로 4방향 애니메이션 제어되며,
    /// 실제 타격 판정(Collider/Hitbox)은 CurrentAim 방향으로 회전하여 정확한 판정을 보장합니다.
    /// </summary>
    [AddComponentMenu("Yiso/Weapon/Weapon Aim")]
    public class YisoWeaponAim : RunIBehaviour {
        [Header("Hitbox Settings")]
        [Tooltip("타격 판정(DamageOnTouch)이 붙어있는 Transform. 입력 방향에 맞춰 회전됨.\n" +
                 "할당하지 않으면 회전하지 않음 (Animator만으로 제어).")]
        [SerializeField] private Transform hitboxTransform;
        
        [Header("Aim Settings")]
        [Tooltip("무기의 기본 오프셋 (로컬 좌표)")]
        [SerializeField] private Vector3 weaponOffset = Vector3.zero;

        [Header("Directional Offsets")]
        [Tooltip("방향별 오프셋 사용 여부. 픽셀 아트에서 방향마다 무기 위치를 미세 조정할 때 사용.")]
        [SerializeField] private bool useDirectionalOffsets = false;

        [Tooltip("위쪽을 볼 때 추가 오프셋"), ShowIf("useDirectionalOffsets")]
        [SerializeField] private Vector3 offsetUp = Vector3.zero;

        [Tooltip("아래쪽을 볼 때 추가 오프셋"), ShowIf("useDirectionalOffsets")]
        [SerializeField] private Vector3 offsetDown = Vector3.zero;
        
        [Tooltip("왼쪽을 볼 때 추가 오프셋"), ShowIf("useDirectionalOffsets")]
        [SerializeField] private Vector3 offsetLeft = Vector3.zero;

        [Tooltip("오른쪽을 볼 때 추가 오프셋"), ShowIf("useDirectionalOffsets")]
        [SerializeField] private Vector3 offsetRight = Vector3.zero;

        private Transform _weaponTransform;
        private Vector3 _initialLocalScale;

        /// <summary>
        /// 현재 무기가 조준하는 정확한 방향 벡터 (입력된 원본 방향)
        /// 히트박스 회전에 사용됨.
        /// </summary>
        public Vector2 CurrentAim { get; private set; } = Vector2.down;

        /// <summary>
        /// 현재 무기가 바라보는 4방향 Enum (참조용)
        /// </summary>
        public FacingDirections CurrentDirection { get; private set; } = FacingDirections.Down;

        protected override void Awake() {
            base.Awake();
            _weaponTransform = transform;
            _initialLocalScale = _weaponTransform.localScale;
        }

        #region Public API

        /// <summary>
        /// 무기의 방향을 설정합니다.
        /// - CurrentAim: 입력받은 벡터를 그대로 저장
        /// - CurrentDirection: Vector2 → FacingDirections로 변환하여 저장
        /// - 히트박스: CurrentAim 방향으로 회전
        /// - 플립: X축 기준 좌우 반전 (선택적)
        /// </summary>
        /// <param name="direction">목표 방향 벡터</param>
        public void SetAimDirection(Vector2 direction) {
            if (direction.sqrMagnitude < 0.01f) {
                return;
            }

            // 1. Vector2 원본 저장
            CurrentAim = direction.normalized;

            // 2. FacingDirections Enum 변환
            CurrentDirection = ConvertToFacingDirection(direction);

            // 3. 히트박스 회전 (Collider가 정확한 방향을 가리키도록)
            RotateHitbox(CurrentAim);

            // 4. 방향에 따라 오프셋 조정 (선택적)
            ApplyDirectionalOffset(CurrentDirection);
        }

        /// <summary>
        /// 무기 오프셋을 설정합니다.
        /// </summary>
        public void SetWeaponOffset(Vector3 offset) {
            weaponOffset = offset;
            _weaponTransform.localPosition = offset;
        }

        #endregion

        #region Private Helper

        /// <summary>
        /// Vector2를 FacingDirections Enum으로 변환합니다.
        /// </summary>
        private FacingDirections ConvertToFacingDirection(Vector2 direction) {
            // 가장 지배적인 축을 기준으로 방향 결정
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y)) {
                return direction.x > 0 ? FacingDirections.Right : FacingDirections.Left;
            }
            else {
                return direction.y > 0 ? FacingDirections.Up : FacingDirections.Down;
            }
        }

        /// <summary>
        /// 히트박스를 입력 방향으로 회전시킵니다.
        /// 무기 비주얼(Sprite)은 Animator가 제어하므로 회전하지 않고,
        /// 타격 판정(Collider)만 회전하여 정확한 방향 판정을 보장합니다.
        /// </summary>
        /// <param name="direction">목표 방향 벡터</param>
        private void RotateHitbox(Vector2 direction) {
            if (hitboxTransform == null) return;

            // Vector2를 각도로 변환 (2D 평면에서 Z축 회전)
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            hitboxTransform.rotation = Quaternion.Euler(0, 0, angle);
        }

        /// <summary>
        /// 방향에 따라 무기 오프셋을 조정합니다.
        /// useDirectionalOffsets가 true일 경우, 기본 오프셋에 방향별 오프셋을 더합니다.
        /// </summary>
        private void ApplyDirectionalOffset(FacingDirections direction) {
            // 기본 오프셋으로 시작
            var finalOffset = weaponOffset;

            // 방향별 오프셋이 활성화된 경우
            if (useDirectionalOffsets) {
                switch (direction) {
                    case FacingDirections.Up:
                        finalOffset += offsetUp;
                        break;

                    case FacingDirections.Down:
                        finalOffset += offsetDown;
                        break;

                    case FacingDirections.Left:
                        finalOffset += offsetLeft;
                        break;

                    case FacingDirections.Right:
                        finalOffset += offsetRight;
                        break;
                }
            }

            // 최종 위치 적용
            _weaponTransform.localPosition = finalOffset;
        }

        #endregion

#if UNITY_EDITOR
        [Header("Gizmo Settings")]
        [Tooltip("Gizmo 화살표 길이")]
        [SerializeField] private float gizmoArrowLength = 1.5f;

        [Tooltip("Gizmo 화살표 머리 크기")]
        [SerializeField] private float gizmoArrowHeadSize = 0.3f;

        [Tooltip("Gizmo 화살표 색상")]
        [SerializeField] private Color gizmoArrowColor = Color.yellow;

        private void OnDrawGizmos() {
            if (CurrentAim.sqrMagnitude < 0.01f) return;

            // 주 화살표 그리기 (무기 위치에서)
            YisoDebugUtils.DrawGizmoArrow(
                transform.position,
                CurrentAim,
                gizmoArrowLength,
                gizmoArrowHeadSize,
                gizmoArrowColor
            );

            // 히트박스가 있으면 히트박스 중심에서도 화살표 그리기 (반투명)
            if (hitboxTransform != null) {
                var hitboxColor = new Color(gizmoArrowColor.r, gizmoArrowColor.g, gizmoArrowColor.b, 0.5f);
                YisoDebugUtils.DrawGizmoArrow(
                    hitboxTransform.position,
                    CurrentAim,
                    gizmoArrowLength * 0.7f,
                    gizmoArrowHeadSize * 0.7f,
                    hitboxColor
                );
            }
        }
#endif
    }
}
