using Core.Behaviour;
using Gameplay.Character.Types;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Character.Weapon {
    /// <summary>
    /// 근접 무기의 히트박스/콤보 관리 전담 컴포넌트.
    /// YisoWeaponAim에서 분리되어, Aim 변경 시 이벤트를 통해 히트박스 회전/오프셋을 갱신합니다.
    /// Bow 등 투사체 무기에는 이 컴포넌트를 붙이지 않으면 됩니다.
    /// </summary>
    [AddComponentMenu("Yiso/Weapon/Melee Hitbox Controller")]
    public class YisoMeleeHitboxController : RunIBehaviour
    {
        [Serializable]
        public class HitboxComboSetting
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
        [SerializeField] private List<HitboxComboSetting> comboSettings;

        // 현재 적용 중인 설정 (캐싱용)
        private HitboxComboSetting _currentSetting;
        private Transform _hitboxTransform;
        private YisoWeaponAim _weaponAim;

        public IReadOnlyList<HitboxComboSetting> ComboSettings => comboSettings;
        public YisoDamageOnTouch CurrentHitbox => _currentSetting?.hitbox;

        protected override void Awake() {
            base.Awake();
            _hitboxTransform = transform;
            _weaponAim = GetComponent<YisoWeaponAim>();

            // 초기화 시 모든 히트박스 끄기
            if (comboSettings != null)
            {
                foreach (var setting in comboSettings)
                {
                    if (setting.hitbox != null) setting.hitbox.gameObject.SetActive(false);
                }
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            if (_weaponAim != null) _weaponAim.OnAimUpdated += HandleAimUpdated;
        }

        protected override void OnDisable() {
            if (_weaponAim != null) _weaponAim.OnAimUpdated -= HandleAimUpdated;
            base.OnDisable();
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
                _currentSetting.hitbox.DisableDamage();
                _currentSetting.hitbox.gameObject.SetActive(false);
            }

            // 2. 새 설정 적용
            _currentSetting = comboSettings[index];

            // 3. 새 히트박스 켜기
            if (_currentSetting.hitbox != null)
            {
                _currentSetting.hitbox.gameObject.SetActive(true);
            }

            // 4. 변경된 오프셋 및 회전 즉시 갱신 (현재 Aim 방향 기준)
            if (_weaponAim != null)
            {
                RotateHitbox(_weaponAim.CurrentAim);
                ApplyOffset(_weaponAim.CurrentDirection);
            }
        }

        /// <summary>
        /// 현재 히트박스의 데미지를 활성화합니다.
        /// </summary>
        public void EnableDamage() => _currentSetting?.hitbox?.EnableDamage();

        /// <summary>
        /// 현재 히트박스의 데미지를 비활성화합니다.
        /// </summary>
        public void DisableDamage() => _currentSetting?.hitbox?.DisableDamage();

        #endregion

        #region Private Helper

        /// <summary>
        /// WeaponAim의 OnAimUpdated 이벤트 핸들러.
        /// Aim 방향이 변경될 때 히트박스 회전/오프셋을 갱신합니다.
        /// </summary>
        private void HandleAimUpdated(Vector2 aim, FacingDirections direction)
        {
            if (_currentSetting == null) return;

            RotateHitbox(aim);
            ApplyOffset(direction);
        }

        private void RotateHitbox(Vector2 direction)
        {
            if (_currentSetting?.hitbox == null) return;

            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _currentSetting.hitbox.transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void ApplyOffset(FacingDirections direction)
        {
            if (_currentSetting == null) return;

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
            _hitboxTransform.localPosition = finalOffset;
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            // 히트박스가 있으면 히트박스 중심에서 화살표 그리기
            if (_currentSetting?.hitbox != null && _weaponAim != null) {
                var aim = _weaponAim.CurrentAim;
                if (aim.sqrMagnitude < 0.01f) return;

                var hitboxColor = new Color(1f, 0f, 0f, 0.5f);
                Utils.YisoDebugUtils.DrawGizmoArrow(
                    _currentSetting.hitbox.transform.position,
                    aim,
                    1.0f,
                    0.2f,
                    hitboxColor
                );
            }
        }
#endif
    }
}
