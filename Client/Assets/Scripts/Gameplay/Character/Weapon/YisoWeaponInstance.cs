using Gameplay.Character.Core;
using Gameplay.Character.Data;
using Gameplay.Character.Types;
using Gameplay.Health;
using UnityEngine;
using Utils;

namespace Gameplay.Character.Weapon {
    /// <summary>
    /// 인스턴스화된 무기를 래핑하는 클래스.
    /// WeaponDataSO와 실제 GameObject를 연결하여 관리하고,
    /// YisoCharacterAnimationModule을 통해 캐릭터 Animator와 동기화하며, 콤보 기반 데미지 계산을 담당.
    /// </summary>
    public class YisoWeaponInstance {
        public YisoWeaponDataSO WeaponData { get; private set; }
        public GameObject WeaponObject { get; private set; }
        public Animator WeaponAnimator { get; private set; }
        public YisoWeaponAim WeaponAim { get; private set; }

        public bool IsActive => WeaponObject != null && WeaponObject.activeSelf;
        public int CurrentComboIndex { get; private set; } = 0;

        private GameObject _owner;
        private IYisoCharacterContext _context;

        public YisoWeaponInstance(IYisoCharacterContext context, YisoWeaponDataSO weaponData, Transform parent, GameObject owner) {
            WeaponData = weaponData;
            _owner = owner;
            _context = context;

            // 무기 프리팹 인스턴스화
            if (weaponData.weaponPrefab != null)
            {
                WeaponObject = Object.Instantiate(weaponData.weaponPrefab, parent);
                WeaponObject.transform.localPosition = Vector3.zero;
                WeaponObject.transform.localRotation = Quaternion.identity;

                WeaponAnimator = WeaponObject.GetComponentInChildren<Animator>();
                WeaponAim = WeaponObject.GetComponentInChildren<YisoWeaponAim>();

                if (WeaponAim != null)
                {
                    // [변경] WeaponAim에 등록된 모든 히트박스에 이벤트 구독
                    if (WeaponAim.ComboSettings != null)
                    {
                        foreach (var setting in WeaponAim.ComboSettings)
                        {
                            if (setting.hitbox != null)
                            {
                                setting.hitbox.SetOwner(owner);
                                setting.hitbox.OnHit += HandleHit;
                            }
                        }
                    }

                    // 초기 상태 설정 (0번 콤보)
                    SetComboIndex(0);
                }
                else
                {
                    YisoLogger.LogWarning($"[YisoWeaponInstance] No WeaponAim on '{weaponData.weaponName}'");
                }

                if (_context.Type == CharacterType.Player && WeaponAnimator == null)
                    YisoLogger.LogWarning($"[YisoWeaponInstance] No Animator on '{weaponData.weaponName}'");
            }
            else
            {
                YisoLogger.LogError($"[YisoWeaponInstance] WeaponDataSO '{weaponData.name}' has no prefab!");
            }
        }

        public void Activate() => WeaponObject?.SetActive(true);
        public void Deactivate() => WeaponObject?.SetActive(false);

        public void Destroy() {
            if (WeaponAim != null && WeaponAim.ComboSettings != null)
            {
                foreach (var setting in WeaponAim.ComboSettings)
                {
                    if (setting.hitbox != null) setting.hitbox.OnHit -= HandleHit;
                }
            }

            if (WeaponObject != null) {
                WeaponObject.SafeDestroy();
                WeaponObject = null;
            }
            
            WeaponAnimator = null;
            WeaponAim = null;
            WeaponData = null;
        }

        // --- Proxy Methods ---
        public void EnableDamage() => WeaponAim?.CurrentHitbox?.EnableDamage();
        public void DisableDamage() => WeaponAim?.CurrentHitbox?.DisableDamage();
        public void SetAimDirection(Vector2 direction) => WeaponAim?.SetAimDirection(direction);
        public void SetComboIndex(int comboIndex)
        {
            CurrentComboIndex = comboIndex;
            WeaponAim?.SetComboIndex(comboIndex);
        }

        private void HandleHit(GameObject target, Vector3 hitPoint) {
            if (WeaponData == null || _owner == null) return;

            var finalDamage = WeaponData.GetComboDamage(CurrentComboIndex);

            var damageInfo = new DamageInfo {
                FinalDamage = finalDamage,
                Attacker = _owner,
                DamageDirection = (target.transform.position - WeaponObject.transform.position).normalized,
                HitPoint = hitPoint,
                KnockbackForce = 5f, 
                IsCritical = false 
            };

            var health = target.GetComponent<YisoEntityHealth>();
            if (health != null && !health.IsDead) {
                health.TakeDamage(damageInfo);
            }
        }
    }
}