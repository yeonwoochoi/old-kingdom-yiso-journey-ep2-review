using Gameplay.Character.Core.Modules;
using Gameplay.Character.Data;
using Gameplay.Character.Types;
using Gameplay.Health;
using UnityEngine;

namespace Gameplay.Character.Weapon {
    /// <summary>
    /// 인스턴스화된 무기를 래핑하는 클래스.
    /// WeaponDataSO와 실제 GameObject를 연결하여 관리하고,
    /// 캐릭터 Animator와 동기화(Hash Caching 최적화 적용), 콤보 기반 데미지 계산을 담당.
    /// </summary>
    public class YisoWeaponInstance {
        public YisoWeaponDataSO WeaponData { get; private set; }
        public GameObject WeaponObject { get; private set; }
        public Animator WeaponAnimator { get; private set; }
        public YisoWeaponAim WeaponAim { get; private set; }
        public YisoDamageOnTouch DamageOnTouch { get; private set; }
        
        public bool IsActive => WeaponObject != null && WeaponObject.activeSelf;

        private int _currentComboIndex = 0;
        private GameObject _owner;

        // --- Performance Optimization: Cached Hash IDs ---
        // 매 프레임 Dictionary 조회를 피하기 위해 해시값을 미리 저장합니다.
        private readonly int _hashHorizontal;
        private readonly int _hashVertical;
        private readonly int _hashCombo;
        private readonly int _hashIsAttacking;
        private readonly int _hashAttackSpeed;

        public YisoWeaponInstance(YisoWeaponDataSO weaponData, Transform parent, GameObject owner) {
            WeaponData = weaponData;
            _owner = owner;

            // 1. 해시 캐싱 (생성 시 1회만 조회)
            _hashHorizontal = YisoAnimatorHashManager.GetHash(YisoCharacterAnimationState.Horizontal);
            _hashVertical = YisoAnimatorHashManager.GetHash(YisoCharacterAnimationState.Vertical);
            _hashCombo = YisoAnimatorHashManager.GetHash(YisoCharacterAnimationState.Combo);
            _hashIsAttacking = YisoAnimatorHashManager.GetHash(YisoCharacterAnimationState.IsAttacking);
            _hashAttackSpeed = YisoAnimatorHashManager.GetHash(YisoCharacterAnimationState.AttackSpeed);

            // 2. 무기 프리팹 인스턴스화
            if (weaponData.weaponPrefab != null) {
                WeaponObject = Object.Instantiate(weaponData.weaponPrefab, parent);
                WeaponObject.transform.localPosition = Vector3.zero;
                WeaponObject.transform.localRotation = Quaternion.identity;

                // 3. 컴포넌트 캐싱
                WeaponAnimator = WeaponObject.GetComponent<Animator>();
                WeaponAim = WeaponObject.GetComponent<YisoWeaponAim>();
                DamageOnTouch = WeaponObject.GetComponent<YisoDamageOnTouch>();

                // 4. 검증 및 설정
                if (WeaponAnimator == null) Debug.LogWarning($"[YisoWeaponInstance] No Animator on '{weaponData.weaponName}'");
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
        public void SetComboIndex(int comboIndex) => _currentComboIndex = comboIndex;
        
        /// <summary>
        /// 캐릭터 Animator의 파라미터를 무기 Animator에 동기화합니다.
        /// 캐싱된 Hash ID를 사용하여 성능을 최적화했습니다. (Zero Allocation, No Dictionary Lookup)
        /// </summary>
        public void SyncAnimator(Animator characterAnimator) {
            if (WeaponAnimator == null || characterAnimator == null) return;

            // 1. Read (Native Call)
            var x = characterAnimator.GetFloat(_hashHorizontal);
            var y = characterAnimator.GetFloat(_hashVertical);
            var combo = characterAnimator.GetInteger(_hashCombo);
            var isAttacking = characterAnimator.GetBool(_hashIsAttacking);
            var attackSpeed = characterAnimator.GetFloat(_hashAttackSpeed);

            // 2. Write (Native Call)
            // Float은 비교 비용 문제로 그냥 대입
            WeaponAnimator.SetFloat(_hashHorizontal, x);
            WeaponAnimator.SetFloat(_hashVertical, y);
            WeaponAnimator.SetFloat(_hashAttackSpeed, attackSpeed);

            // Int, Bool은 값 변경 체크 후 대입 (Trigger 오동작 방지 및 최적화)
            if (WeaponAnimator.GetInteger(_hashCombo) != combo) 
                WeaponAnimator.SetInteger(_hashCombo, combo);

            if (WeaponAnimator.GetBool(_hashIsAttacking) != isAttacking) 
                WeaponAnimator.SetBool(_hashIsAttacking, isAttacking);
        }

        private void HandleHit(GameObject target, Vector3 hitPoint) {
            if (WeaponData == null || _owner == null) return;

            var finalDamage = WeaponData.GetComboDamage(_currentComboIndex);

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