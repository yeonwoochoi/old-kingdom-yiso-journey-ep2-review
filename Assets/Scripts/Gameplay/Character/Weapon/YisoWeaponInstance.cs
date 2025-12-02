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
        public int CurrentComboIndex { get; private set; } = 0;

        private GameObject _owner;

        // --- Performance Optimization: Cached Hash IDs ---
        // 매 프레임 Dictionary 조회를 피하기 위해 해시값을 미리 저장합니다.
        // Float Parameters
        private readonly int _hashMoveSpeed;
        private readonly int _hashAttackSpeed;
        private readonly int _hashHorizontal;
        private readonly int _hashVertical;

        // Bool Parameters
        private readonly int _hashIsIdle;
        private readonly int _hashIsMoving;
        private readonly int _hashIsAttacking;
        private readonly int _hashIsMoveAttacking;
        private readonly int _hashIsDead;
        private readonly int _hashIsSpellCasting;
        private readonly int _hashIsSkillCasting;

        // Int Parameters
        private readonly int _hashSkillNumber;
        private readonly int _hashCombo;
        private readonly int _hashDeathType;

        public YisoWeaponInstance(YisoWeaponDataSO weaponData, Transform parent, GameObject owner) {
            WeaponData = weaponData;
            _owner = owner;

            // 1. 해시 캐싱 (생성 시 1회만 조회) - Full Synchronization
            // Float Parameters
            _hashMoveSpeed = YisoAnimatorHashManager.GetHash(YisoCharacterAnimationState.MoveSpeed);
            _hashAttackSpeed = YisoAnimatorHashManager.GetHash(YisoCharacterAnimationState.AttackSpeed);
            _hashHorizontal = YisoAnimatorHashManager.GetHash(YisoCharacterAnimationState.Horizontal);
            _hashVertical = YisoAnimatorHashManager.GetHash(YisoCharacterAnimationState.Vertical);

            // Bool Parameters
            _hashIsIdle = YisoAnimatorHashManager.GetHash(YisoCharacterAnimationState.IsIdle);
            _hashIsMoving = YisoAnimatorHashManager.GetHash(YisoCharacterAnimationState.IsMoving);
            _hashIsAttacking = YisoAnimatorHashManager.GetHash(YisoCharacterAnimationState.IsAttacking);
            _hashIsMoveAttacking = YisoAnimatorHashManager.GetHash(YisoCharacterAnimationState.IsMoveAttacking);
            _hashIsDead = YisoAnimatorHashManager.GetHash(YisoCharacterAnimationState.IsDead);
            _hashIsSpellCasting = YisoAnimatorHashManager.GetHash(YisoCharacterAnimationState.IsSpellCasting);
            _hashIsSkillCasting = YisoAnimatorHashManager.GetHash(YisoCharacterAnimationState.IsSkillCasting);

            // Int Parameters
            _hashSkillNumber = YisoAnimatorHashManager.GetHash(YisoCharacterAnimationState.SkillNumber);
            _hashCombo = YisoAnimatorHashManager.GetHash(YisoCharacterAnimationState.Combo);
            _hashDeathType = YisoAnimatorHashManager.GetHash(YisoCharacterAnimationState.DeathType);

            // 2. 무기 프리팹 인스턴스화
            if (weaponData.weaponPrefab != null) {
                WeaponObject = Object.Instantiate(weaponData.weaponPrefab, parent);
                WeaponObject.transform.localPosition = Vector3.zero;
                WeaponObject.transform.localRotation = Quaternion.identity;

                // 3. 컴포넌트 캐싱
                WeaponAnimator = WeaponObject.GetComponentInChildren<Animator>();
                WeaponAim = WeaponObject.GetComponentInChildren<YisoWeaponAim>();
                DamageOnTouch = WeaponObject.GetComponentInChildren<YisoDamageOnTouch>();

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
        public void SetComboIndex(int comboIndex) => CurrentComboIndex = comboIndex;
        
        /// <summary>
        /// 캐릭터 Animator의 모든 파라미터를 무기 Animator에 동기화합니다.
        /// 캐싱된 Hash ID를 사용하여 성능을 최적화했습니다. (Zero Allocation, No Dictionary Lookup)
        /// </summary>
        public void SyncAnimator(Animator characterAnimator) {
            if (WeaponAnimator == null || characterAnimator == null) return;

            // --- Float Parameters (직접 대입, 비교 비용이 더 큼) ---
            WeaponAnimator.SetFloat(_hashMoveSpeed, characterAnimator.GetFloat(_hashMoveSpeed));
            WeaponAnimator.SetFloat(_hashAttackSpeed, characterAnimator.GetFloat(_hashAttackSpeed));
            WeaponAnimator.SetFloat(_hashHorizontal, characterAnimator.GetFloat(_hashHorizontal));
            WeaponAnimator.SetFloat(_hashVertical, characterAnimator.GetFloat(_hashVertical));

            // --- Bool Parameters (값 변경 체크 후 대입, Trigger 오동작 방지) ---
            SyncBool(characterAnimator, _hashIsIdle);
            SyncBool(characterAnimator, _hashIsMoving);
            SyncBool(characterAnimator, _hashIsAttacking);
            SyncBool(characterAnimator, _hashIsMoveAttacking);
            SyncBool(characterAnimator, _hashIsDead);
            SyncBool(characterAnimator, _hashIsSpellCasting);
            SyncBool(characterAnimator, _hashIsSkillCasting);

            // --- Int Parameters (값 변경 체크 후 대입) ---
            SyncInt(characterAnimator, _hashSkillNumber);
            SyncInt(characterAnimator, _hashCombo);
            SyncInt(characterAnimator, _hashDeathType);
        }

        /// <summary>
        /// Bool 파라미터 동기화 헬퍼 메서드.
        /// 값이 변경된 경우에만 SetBool 호출하여 불필요한 State Machine 트리거 방지.
        /// </summary>
        private void SyncBool(Animator source, int hash) {
            var value = source.GetBool(hash);
            if (WeaponAnimator.GetBool(hash) != value) {
                WeaponAnimator.SetBool(hash, value);
            }
        }

        /// <summary>
        /// Int 파라미터 동기화 헬퍼 메서드.
        /// 값이 변경된 경우에만 SetInteger 호출하여 최적화.
        /// </summary>
        private void SyncInt(Animator source, int hash) {
            var value = source.GetInteger(hash);
            if (WeaponAnimator.GetInteger(hash) != value) {
                WeaponAnimator.SetInteger(hash, value);
            }
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