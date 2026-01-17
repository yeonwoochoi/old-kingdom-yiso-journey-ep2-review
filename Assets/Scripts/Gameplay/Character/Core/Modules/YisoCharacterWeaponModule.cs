using System;
using Gameplay.Character.Data;
using Gameplay.Character.Weapon;
using UnityEngine;
using Utils;

namespace Gameplay.Character.Core.Modules {
    /// <summary>
    /// 캐릭터의 무기 생성, 교체, 파괴를 담당하는 모듈.
    /// </summary>
    public sealed class YisoCharacterWeaponModule : YisoCharacterModuleBase {
        private readonly Settings _settings;

        /// <summary>
        /// 현재 장착된 무기 인스턴스
        /// </summary>
        public YisoWeaponInstance CurrentWeapon { get; private set; }

        /// <summary>
        /// 무기가 부착될 Transform (캐릭터의 손 등)
        /// </summary>
        private Transform _weaponAttachPoint;

        public YisoCharacterWeaponModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
        }

        public override void Initialize() {
            base.Initialize();

            // 무기 부착 지점 설정
            if (_settings.weaponAttachPoint != null) {
                _weaponAttachPoint = _settings.weaponAttachPoint;
            }
            else {
                // 설정되지 않은 경우 캐릭터의 Model을 부착 지점으로 사용
                _weaponAttachPoint = Context.Transform;
                YisoLogger.LogWarning($"[YisoCharacterWeaponModule] WeaponAttachPoint가 설정되지 않아 캐릭터의 Transform을 사용합니다.");
            }
        }

        public override void LateInitialize() {
            base.LateInitialize();

            // 초기 무기 장착

            YisoLogger.Log($"[WeaponModule] LateInitialize 실행됨: {Context.GameObject.name}", Context.GameObject);

            if (_settings.initialWeapon != null)
            {
                EquipWeapon(_settings.initialWeapon);
            }
            else
            {
                YisoLogger.LogWarning($"[WeaponModule] 초기 무기가 설정되지 않았습니다: {Context.GameObject.name}", Context.GameObject);
            }
        }

        // OnUpdate에서의 Bottom-up 동기화 제거
        // → Top-down Push 방식으로 변경: AnimationModule.Set* 호출 시 자동으로 외부 Animator에 전파됨
        public override void OnDestroy() {
            base.OnDestroy();
            // 모듈 파괴 시 현재 무기도 파괴
            if (CurrentWeapon != null) {
                CurrentWeapon.Destroy();
                CurrentWeapon = null;
            }
        }

        #region Public API

        /// <summary>
        /// 새로운 무기를 장착합니다.
        /// 기존 무기가 있으면 파괴하고 새 무기를 생성합니다.
        /// </summary>
        /// <param name="weaponData">장착할 무기 데이터</param>
        public void EquipWeapon(YisoWeaponDataSO weaponData) {
            if (weaponData == null) {
                YisoLogger.LogWarning("[YisoCharacterWeaponModule] EquipWeapon: weaponData가 null입니다.");
                return;
            }

            // 기존 무기 제거
            UnequipWeapon();

            // 새 무기 생성
            CurrentWeapon = new YisoWeaponInstance(
                Context,
                weaponData,
                _weaponAttachPoint,
                Context.GameObject
            );

            CurrentWeapon.Activate();

            // 무기 Animator를 AnimationModule에 등록 (Top-down Push 방식)
            if (CurrentWeapon.WeaponAnimator != null) {
                var animationModule = Context.GetModule<YisoCharacterAnimationModule>();
                animationModule?.RegisterExternalAnimator(CurrentWeapon.WeaponAnimator);
            }

            YisoLogger.Log($"[YisoCharacterWeaponModule] 무기 '{weaponData.weaponName}' 장착 완료.");
        }

        /// <summary>
        /// 현재 무기를 해제합니다.
        /// </summary>
        public void UnequipWeapon() {
            if (CurrentWeapon != null) {
                // 무기 Animator 등록 해제
                if (CurrentWeapon.WeaponAnimator != null) {
                    var animationModule = Context.GetModule<YisoCharacterAnimationModule>();
                    animationModule?.UnregisterExternalAnimator(CurrentWeapon.WeaponAnimator);
                }

                CurrentWeapon.Destroy();
                CurrentWeapon = null;
            }
        }

        /// <summary>
        /// 현재 무기의 방향을 설정합니다.
        /// </summary>
        /// <param name="direction">목표 방향</param>
        public void SetWeaponDirection(Vector2 direction) {
            CurrentWeapon?.SetAimDirection(direction);
        }

        /// <summary>
        /// 현재 무기의 DamageOnTouch를 활성화합니다.
        /// </summary>
        public void EnableWeaponDamage() {
            CurrentWeapon?.EnableDamage();
        }

        /// <summary>
        /// 현재 무기의 DamageOnTouch를 비활성화합니다.
        /// </summary>
        public void DisableWeaponDamage() {
            CurrentWeapon?.DisableDamage();
        }

        /// <summary>
        /// 현재 장착된 무기의 데이터를 반환합니다.
        /// </summary>
        public YisoWeaponDataSO GetCurrentWeaponData() {
            return CurrentWeapon?.WeaponData;
        }

        /// <summary>
        /// 무기가 장착되어 있는지 확인합니다.
        /// </summary>
        public bool HasWeapon() {
            return CurrentWeapon != null && CurrentWeapon.IsActive;
        }

        /// <summary>
        /// 현재 무기의 콤보 인덱스를 설정합니다.
        /// </summary>
        public void SetComboIndex(int comboIndex) {
            CurrentWeapon?.SetComboIndex(comboIndex);
        }

        #endregion

        [Serializable]
        public class Settings {
            [Tooltip("무기가 부착될 Transform (예: 캐릭터의 손)")]
            public Transform weaponAttachPoint;

            [Tooltip("게임 시작 시 기본으로 장착할 무기")]
            public YisoWeaponDataSO initialWeapon;
        }
    }
}
