using System;
using Core.Behaviour;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils;

namespace Gameplay.Health {
    // TODO: Core 서비스에서 구현되면 지워야함.
    public struct DamageInfo {
        public float FinalDamage;
        public GameObject Attacker;
        public Vector3 DamageDirection;
        public Vector3 HitPoint;
        public float KnockbackForce;
        public bool IsCritical;
    }
    
    /// <summary>
    /// Entity의 체력을 관리하는 컴포넌트.
    ///
    /// 초기화 모드:
    /// - useManualInitialization = true: Awake()에서 자동으로 initialHealth 값으로 초기화 (상자, NPC 등)
    /// - useManualInitialization = false: 외부에서 Initialize() 호출 필요 (플레이어 캐릭터 등)
    ///
    /// 중요: 다른 Health 관련 컴포넌트들(YisoHealthUIController, YisoHealthAnimator 등)은
    ///       Start()에서 EntityHealth에 접근하므로, Manual 모드에서는 Awake()에서 초기화하여 순서 보장.
    /// </summary>
    [AddComponentMenu("Yiso/Health/Entity Health")]
    public class YisoEntityHealth: RunIBehaviour {
        [Tooltip("True: Awake()에서 'Initial Health' 값으로 자동 초기화 (상자, NPC 등)\nFalse: 외부 시스템이 Initialize()를 호출해야 함 (플레이어 캐릭터 등)")]
        [SerializeField] private bool useManualInitialization = true;
        [SerializeField, ShowIf("useManualInitialization")] private float initialHealth = 100f;

        
        // --- 런타임 데이터 ---
        public float CurrentHealth { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;
        public bool IsInitialized { get; private set; } = false;

        // --- 핵심 이벤트 ---
        public event Action<float, float> OnHealthChanged; // 데미지가 0이면 호출 안됨
        public event Action<DamageInfo> OnDamaged; // Damage가 0이어도 호출됨 (floating text 같은건 이걸로)
        public event Action OnDied;
        public event Action<float> OnHealed;
        public event Action OnRevived;

        private YisoDamageProcessor _damageProcessor;

        protected override void Awake() {
            base.Awake();
            _damageProcessor = GetComponentInChildren<YisoDamageProcessor>();

            // Manual 초기화 모드면 Awake에서 즉시 초기화
            // (다른 컴포넌트들이 Start에서 EntityHealth에 접근하므로 순서 보장 필요)
            if (useManualInitialization) {
                Initialize(initialHealth);
            }
        }

        public void Initialize(float newMaxHealth, float? startingHealth = null) {
            if (IsInitialized) {
                return;
            }
            
            MaxHealth = newMaxHealth;
            CurrentHealth = startingHealth ?? newMaxHealth;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);

            _damageProcessor = GetComponentInChildren<YisoDamageProcessor>();
            
            IsInitialized = true;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void TakeDamage(DamageInfo damageInfo) {
            if (!IsInitialized || IsDead) {
                return;
            }
            
            if (_damageProcessor == null) {
                YisoLogger.LogWarning($"[{gameObject.name}] DamageProcessor is null!");
            }

            // 무적 체크 - 무적이면 아무것도 하지 않고 리턴
            if (_damageProcessor != null && !_damageProcessor.CanTakeDamage()) {
                return;
            }

            var previousHealth = CurrentHealth;
            var finalDamage = _damageProcessor == null
                ? damageInfo.FinalDamage
                : _damageProcessor.FinalizeDamage(damageInfo);

            CurrentHealth = Math.Max(CurrentHealth - finalDamage, 0f);
            if (!Mathf.Approximately(previousHealth, CurrentHealth)) {
                OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            }
            OnDamaged?.Invoke(damageInfo);

            if (CurrentHealth <= 0f && previousHealth > 0f) {
                OnDied?.Invoke();
            }
        }

        public void Heal(float amount) {
            if (!IsInitialized || IsDead || amount <= 0 || CurrentHealth >= MaxHealth) {
                return;
            }

            CurrentHealth = Math.Min(CurrentHealth + amount, MaxHealth);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            OnHealed?.Invoke(amount);
        }

        public void Revive() {
            if (!IsInitialized || !IsDead) {
                return;
            }

            CurrentHealth = MaxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            OnRevived?.Invoke();
        }
    }
}