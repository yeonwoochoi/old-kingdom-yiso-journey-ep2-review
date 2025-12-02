using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Character.Data {
    /// <summary>
    /// 무기의 데이터를 정의하는 ScriptableObject.
    /// 무기의 프리팹, 데미지, 공격 속도, 콤보 설정 등의 정보를 포함.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Weapon_", menuName = "Yiso/Weapon/Weapon Data")]
    public class YisoWeaponDataSO : ScriptableObject {
        [Header("Basic Info")]
        [Tooltip("무기의 이름")]
        public string weaponName = "New Weapon";

        [Tooltip("무기 프리팹 (Animator, YisoWeaponAim, YisoDamageOnTouch 컴포넌트 포함)")]
        public GameObject weaponPrefab;

        [Header("Damage Settings")]
        [Tooltip("기본 데미지")]
        public float baseDamage = 10f;

        [Tooltip("최소 데미지")]
        public float minDamage = 10f;

        [Tooltip("최대 데미지")]
        public float maxDamage = 15f;

        [Header("Attack Settings")]
        [Tooltip("공격 속도 (초당 공격 횟수)")]
        public float attackRate = 1f;

        [Tooltip("공격 지속 시간 (초) - DamageOnTouch 활성화 시간")]
        public float attackDuration = 0.3f;

        [Header("Combo Settings")]
        [Tooltip("최대 콤보 수 (예: 4 = 4단 콤보)")]
        public int maxComboCount = 4;

        [Tooltip("콤보 리셋 시간 (초) - 마지막 공격 후 이 시간이 지나면 콤보 리셋")]
        public float comboResetTime = 1.0f;

        [Tooltip("콤보 단계별 데미지 배율 (인덱스 0 = 1타, 1 = 2타, ...)")]
        public List<float> comboDamageMultipliers = new List<float> { 1.0f, 1.2f, 1.5f, 2.0f };

        /// <summary>
        /// 데미지 범위 내에서 랜덤 데미지 값을 반환.
        /// </summary>
        public float GetRandomDamage() {
            return Random.Range(minDamage, maxDamage);
        }

        /// <summary>
        /// 공격 쿨타임(초)을 반환.
        /// </summary>
        public float GetAttackCooldown() {
            return attackRate > 0 ? 1f / attackRate : 0f;
        }

        /// <summary>
        /// 콤보 인덱스에 맞는 데미지 배율을 반환.
        /// </summary>
        /// <param name="comboIndex">콤보 인덱스 (0부터 시작)</param>
        public float GetComboDamageMultiplier(int comboIndex) {
            if (comboDamageMultipliers == null || comboDamageMultipliers.Count == 0) {
                return 1.0f;
            }

            // 인덱스 범위 체크
            if (comboIndex < 0 || comboIndex >= comboDamageMultipliers.Count) {
                return comboDamageMultipliers[comboDamageMultipliers.Count - 1];
            }

            return comboDamageMultipliers[comboIndex];
        }

        /// <summary>
        /// 콤보를 고려한 최종 데미지를 계산.
        /// </summary>
        /// <param name="comboIndex">현재 콤보 인덱스 (0부터 시작)</param>
        public float GetComboDamage(int comboIndex) {
            var randomDamage = GetRandomDamage();
            var multiplier = GetComboDamageMultiplier(comboIndex);
            return randomDamage * multiplier;
        }
    }
}
