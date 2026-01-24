using Core.Behaviour;
using Gameplay.Character.Types;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Gameplay.Character.Weapon {
    [AddComponentMenu("Yiso/Weapon/Weapon Aim")]
    public class YisoWeaponAim : RunIBehaviour
    {
        [Serializable]
        public class WeaponComboSetting
        {
            [Title("Hitbox")]
            [Required]
            public YisoDamageOnTouch hitbox; // 해당 콤보에 쓸 히트박스

            [Title("Offsets")]
            [Tooltip("이 콤보에서의 기본 위치 오프셋")]
            public Vector3 baseOffset = Vector3.zero;

            public bool useDirectionalOffsets = false;

            [ShowIf("useDirectionalOffsets")][Indent] public Vector3 offsetUp = Vector3.zero;
            [ShowIf("useDirectionalOffsets")][Indent] public Vector3 offsetDown = Vector3.zero;
            [ShowIf("useDirectionalOffsets")][Indent] public Vector3 offsetLeft = Vector3.zero;
            [ShowIf("useDirectionalOffsets")][Indent] public Vector3 offsetRight = Vector3.zero;
        }

        [Header("Combo Settings")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<WeaponComboSetting> comboSettings;

        // 현재 적용 중인 설정 (캐싱용)
        private WeaponComboSetting _currentSetting; // 현재 히트박스
        private Transform _weaponTransform;

        /// <summary>
        /// 조준 방향 잠금 플래그.
        /// true일 경우 SetAimDirection 호출 시 방향이 변경되지 않습니다 (공격 중 등).
        /// </summary>
        private bool _isAimLocked = false;

        /// <summary>
        /// 현재 무기가 조준하는 정확한 방향 벡터 (입력된 원본 방향)
        /// 히트박스 회전에 사용됨.
        /// </summary>
        public Vector2 CurrentAim { get; private set; } = Vector2.down;

        /// <summary>
        /// 현재 무기가 바라보는 4방향 Enum (참조용)
        /// </summary>
        public FacingDirections CurrentDirection { get; private set; } = FacingDirections.Down;
        public IReadOnlyList<WeaponComboSetting> ComboSettings => comboSettings;
        public YisoDamageOnTouch CurrentHitbox => _currentSetting?.hitbox;

        protected override void Awake() {
            base.Awake();
            _weaponTransform = transform;

            // 초기화 시 모든 히트박스 끄기
            if (comboSettings != null)
            {
                foreach (var setting in comboSettings)
                {
                    if (setting.hitbox != null) setting.hitbox.gameObject.SetActive(false);
                }
            }
        }

        #region Public API

        /// <summary>
        /// 콤보 인덱스를 변경하고, 해당 설정(히트박스, 오프셋)을 적용합니다.
        /// </summary>
        public void SetComboIndex(int index)
        {
            if (comboSettings == null || comboSettings.Count == 0) return;

            // 인덱스 안전 처리
            if (index < 0 || index >= comboSettings.Count) index = 0;

            // 1. 이전 히트박스 끄기
            if (_currentSetting?.hitbox != null)
            {
                _currentSetting.hitbox.DisableDamage(); // Collider 끄기
                _currentSetting.hitbox.gameObject.SetActive(false);
            }

            // 2. 새 설정 적용
            _currentSetting = comboSettings[index];

            // 3. 새 히트박스 켜기
            if (_currentSetting.hitbox != null)
            {
                _currentSetting.hitbox.gameObject.SetActive(true);
            }

            // 4. 변경된 오프셋 및 회전 즉시 갱신 (현재 바라보는 방향 기준)
            ApplyAim(CurrentAim, lockAfter: _isAimLocked);
        }

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

            // 현재 설정이 없으면 아무것도 못함
            if (_currentSetting == null) return;

            RotateHitbox(CurrentAim);
            ApplyOffset(CurrentDirection);

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

        private void RotateHitbox(Vector2 direction)
        {
            if (_currentSetting.hitbox == null) return;

            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _currentSetting.hitbox.transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void ApplyOffset(FacingDirections direction)
        {
            // 현재 콤보 설정의 Base Offset 사용
            var finalOffset = _currentSetting.baseOffset;

            if (_currentSetting.useDirectionalOffsets)
            {
                switch (direction)
                {
                    case FacingDirections.Up: finalOffset += _currentSetting.offsetUp; break;
                    case FacingDirections.Down: finalOffset += _currentSetting.offsetDown; break;
                    case FacingDirections.Left: finalOffset += _currentSetting.offsetLeft; break;
                    case FacingDirections.Right: finalOffset += _currentSetting.offsetRight; break;
                }
            }
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
            if (_currentSetting?.hitbox != null) {
                var hitboxColor = new Color(gizmoArrowColor.r, gizmoArrowColor.g, gizmoArrowColor.b, 0.5f);
                YisoDebugUtils.DrawGizmoArrow(
                    _currentSetting.hitbox.transform.position,
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
