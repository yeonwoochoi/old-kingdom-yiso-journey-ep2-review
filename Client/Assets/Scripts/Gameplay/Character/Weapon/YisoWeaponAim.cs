using Core.Behaviour;
using Gameplay.Character.Types;
using System;
using UnityEngine;
using Utils;

namespace Gameplay.Character.Weapon {
    [AddComponentMenu("Yiso/Weapon/Weapon Aim")]
    public class YisoWeaponAim : RunIBehaviour
    {
        /// <summary>
        /// 조준 방향 잠금 플래그.
        /// true일 경우 SetAimDirection 호출 시 방향이 변경되지 않습니다 (공격 중 등).
        /// </summary>
        private bool _isAimLocked = false;

        /// <summary>
        /// 현재 무기가 조준하는 정확한 방향 벡터 (입력된 원본 방향)
        /// </summary>
        public Vector2 CurrentAim { get; private set; } = Vector2.down;

        /// <summary>
        /// 현재 무기가 바라보는 4방향 Enum (참조용)
        /// </summary>
        public FacingDirections CurrentDirection { get; private set; } = FacingDirections.Down;

        /// <summary>
        /// Aim 방향이 갱신될 때 발행되는 이벤트.
        /// HitboxController 등 외부 컴포넌트가 구독하여 회전/오프셋을 갱신합니다.
        /// </summary>
        public event Action<Vector2, FacingDirections> OnAimUpdated;

        #region Public API

        public void SetAimDirection(Vector2 direction) {
            if (direction.sqrMagnitude < 0.01f) return;
            if (_isAimLocked) return;

            ApplyAim(direction, lockAfter: false);
        }

        /// <summary>
        /// 조준 방향을 강제로 설정하고 잠급니다 (공격 시작 시 사용).
        /// </summary>
        /// <param name="direction">고정할 방향</param>
        public void LockAimToDirection(Vector2 direction) {
            if (direction.sqrMagnitude < 0.01f) return;

            ApplyAim(direction, lockAfter: true);
        }

        public void LockAim() => _isAimLocked = true;
        public void UnlockAim() => _isAimLocked = false;
        public bool IsAimLocked() => _isAimLocked;

        #endregion

        #region Private Helper

        private void ApplyAim(Vector2 direction, bool lockAfter)
        {
            CurrentAim = direction.normalized;
            CurrentDirection = ConvertToFacingDirection(CurrentAim);

            // 이벤트 발행 — 구독자(HitboxController 등)가 회전/오프셋 갱신
            OnAimUpdated?.Invoke(CurrentAim, CurrentDirection);

            if (lockAfter) _isAimLocked = true;
        }

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

            // Aim 방향 화살표 그리기
            YisoDebugUtils.DrawGizmoArrow(
                transform.position,
                CurrentAim,
                gizmoArrowLength,
                gizmoArrowHeadSize,
                gizmoArrowColor
            );
        }
#endif
    }
}
