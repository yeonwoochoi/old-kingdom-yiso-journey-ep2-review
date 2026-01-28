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
        [Tooltip("공격 간의 최소 대기 시간 (초). 콤보 입력 허용 간격.")]
        public float minAttackInterval = 0.2f;
        [Tooltip("공격 속도 (초당 공격 횟수)")]
        public float attackSpeed = 1f;

        [Tooltip("공격 지속 시간 (초)")]
        public List<float> baseAttackDurations = new List<float>();

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

        //// <summary>
        /// 공격 쿨타임(최소 입력 간격)을 반환.
        /// 속도와 관계없이 입력 제어용으로 사용.
        /// </summary>
        public float GetAttackCooldown()
        {
            return minAttackInterval;
        }

        public float GetAttackDuration(int comboIndex)
        {
            if (baseAttackDurations == null || baseAttackDurations.Count == 0) return 0f;

            // 인덱스 안전 처리
            int index = Mathf.Clamp(comboIndex, 0, baseAttackDurations.Count - 1);

            float baseDuration = baseAttackDurations[index];

            // 속도가 0이면 무한대나 다름없으므로 방어 코드
            if (attackSpeed <= 0.01f) return baseDuration;

            // [핵심] 속도가 2배면 시간은 1/2로 줄어듦
            return baseDuration / attackSpeed;
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
