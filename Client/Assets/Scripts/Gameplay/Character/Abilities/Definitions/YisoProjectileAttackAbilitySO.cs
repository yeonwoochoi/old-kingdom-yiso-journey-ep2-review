using System;
using Gameplay.Character.Types;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay.Character.Abilities.Definitions {
    /// <summary>
    /// 원거리 투사체 공격 Ability의 설정을 정의하는 ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Ability_ProjectileAttack", menuName = "Yiso/Abilities/Projectile Attack")]
    public class YisoProjectileAttackAbilitySO : YisoAbilitySO {
        [Header("Input Settings")]
        [Tooltip("연속 입력 모드: true = 버튼을 누르고 있는 동안 연속 공격, false = 버튼을 누를 때마다 한 번씩 공격")]
        public bool continuousPressAttack = false;

        [Tooltip("공격 중 이동 가능 여부")]
        public bool canMoveWhileAttacking = false;

        [Header("Projectile Settings")]
        [Tooltip("투사체 프리팹 (YisoDamageOnTouch 컴포넌트 포함 필수)")]
        public GameObject projectilePrefab;

        [Tooltip("투사체 속도 (units/second)")]
        public float projectileSpeed = 10f;

        [Tooltip("투사체 최대 사거리")]
        public float maxRange = 15f;

        [Tooltip("한 번 발사 시 투사체 개수")]
        [Range(1, 20)]
        public int projectileCount = 1;

        [Tooltip("여러 발 발사 시 발사 간격 (초)")]
        public float fireInterval = 0.1f;

        [Header("Spread Settings")]
        [Tooltip("타겟 없을 때 산발 각도 (도). 0이면 정면으로만 발사")]
        [Range(0f, 180f)]
        public float spreadAngle = 15f;

        [Tooltip("여러 발 발사 시 산발 패턴. true = 부채꼴로 균등 분배, false = 랜덤 산발")]
        public bool evenSpread = true;

        [Header("Target Detection Settings")]
        [Tooltip("타겟 감지 레이어 마스크")]
        public LayerMask targetLayerMask = -1;

        [Tooltip("타겟 감지 거리 (이 범위 내의 적만 자동 타겟팅)")]
        public float detectionRange = 10f;

        [Tooltip("캐릭터 바라보는 방향 기준 타겟 감지 각도 (도). 이 각도 내의 적만 타겟으로 선택")]
        [Range(0f, 360f)]
        public float detectionAngle = 90f;

        [Header("Damage Settings")]
        [Tooltip("기본 데미지 (WeaponModule이 없을 때 사용)")]
        public float baseDamage = 10f;

        [Tooltip("최소 데미지")]
        public float minDamage = 8f;

        [Tooltip("최대 데미지")]
        public float maxDamage = 12f;
        
        [Header("Spawn Settings")]
        [Tooltip("Spawn 오프셋")]
        public Vector2 eastSpawnOffset = new Vector2(0.29f, 0.52f);
        public Vector2 westSpawnOffset = new Vector2(0.29f, -0.52f);
        public Vector2 southSpawnOffset = new Vector2(-0.023f, -0.056f);
        public Vector2 northSpawnOffset = new Vector2(0.45f, 0f);

        /// <summary>
        /// 데미지 범위 내에서 랜덤 데미지 값을 반환.
        /// </summary>
        public float GetRandomDamage() {
            return Random.Range(minDamage, maxDamage);
        }

        public Vector2 GetSpawnOffset(FacingDirections direction) {
            switch (direction) {
                case FacingDirections.Up:
                    Debug.Log($"{direction} {northSpawnOffset}");
                    return northSpawnOffset;
                case FacingDirections.Down:
                    Debug.Log($"{direction} {southSpawnOffset}");
                    return southSpawnOffset;
                case FacingDirections.Left:
                    Debug.Log($"{direction} {westSpawnOffset}");
                    return westSpawnOffset;
                case FacingDirections.Right:
                    Debug.Log($"{direction} {eastSpawnOffset}");
                    return eastSpawnOffset;
                default:
                    Debug.Log($"{direction} {Vector2.zero}");
                    return Vector2.zero;
            }
        }

        public override IYisoCharacterAbility CreateAbility() {
            return new YisoProjectileAttackAbility(this);
        }
    }
}
