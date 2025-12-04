using Gameplay.Character.Core;
using Gameplay.Character.Data;
using Gameplay.Character.Types;
using Gameplay.Health;
using UnityEngine;

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
        public YisoDamageOnTouch DamageOnTouch { get; private set; }

        public bool IsActive => WeaponObject != null && WeaponObject.activeSelf;
        public int CurrentComboIndex { get; private set; } = 0;

        private GameObject _owner;
        private IYisoCharacterContext _context;

        public YisoWeaponInstance(IYisoCharacterContext context, YisoWeaponDataSO weaponData, Transform parent, GameObject owner) {
            WeaponData = weaponData;
            _owner = owner;
            _context = context;

            // 무기 프리팹 인스턴스화
            if (weaponData.weaponPrefab != null) {
                WeaponObject = Object.Instantiate(weaponData.weaponPrefab, parent);
                WeaponObject.transform.localPosition = Vector3.zero;
                WeaponObject.transform.localRotation = Quaternion.identity;

                // 컴포넌트 캐싱
                WeaponAnimator = WeaponObject.GetComponentInChildren<Animator>();
                WeaponAim = WeaponObject.GetComponentInChildren<YisoWeaponAim>();
                DamageOnTouch = WeaponObject.GetComponentInChildren<YisoDamageOnTouch>();

                // 4. 검증 및 설정 (Enemy는 Weapon에 Animator 없음)
                if (_context.Type == CharacterType.Player && WeaponAnimator == null) Debug.LogWarning($"[YisoWeaponInstance] No Animator on '{weaponData.weaponName}'");
                if (WeaponAim == null) Debug.LogWarning($"[YisoWeaponInstance] No WeaponAim on '{weaponData.weaponName}'");

                if (DamageOnTouch != null) {
                    DamageOnTouch.SetOwner(owner);
                    DamageOnTouch.OnHit += HandleHit;
                } else {
                    Debug.LogWarning($"[YisoWeaponInstance] No DamageOnTouch on '{weaponData.weaponName}'");
                }
            }
            else {
                Debug.LogError($"[YisoWeaponInstance] WeaponDataSO '{weaponData.name}' has no prefab!");
            }
        }

        public void Activate() => WeaponObject?.SetActive(true);
        public void Deactivate() => WeaponObject?.SetActive(false);

        public void Destroy() {
            if (DamageOnTouch != null) DamageOnTouch.OnHit -= HandleHit;
            
            if (WeaponObject != null) {
                Object.Destroy(WeaponObject);
                WeaponObject = null;
            }
            
            WeaponAnimator = null;
            WeaponAim = null;
            DamageOnTouch = null;
            WeaponData = null;
        }

        // --- Proxy Methods ---
        public void EnableDamage() => DamageOnTouch?.EnableDamage();
        public void DisableDamage() => DamageOnTouch?.DisableDamage();
        public void SetAimDirection(Vector2 direction) => WeaponAim?.SetAimDirection(direction);
        public void SetComboIndex(int comboIndex) => CurrentComboIndex = comboIndex;

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