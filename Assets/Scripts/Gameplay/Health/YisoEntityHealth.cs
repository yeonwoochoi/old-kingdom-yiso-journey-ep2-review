using System;
using Core.Behaviour;
using Sirenix.OdinInspector;
using UnityEngine;

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
    
    [AddComponentMenu("Yiso/Health/Entity Health")]
    public class YisoEntityHealth: RunIBehaviour {
        [Tooltip("True: 인스펙터의 'Manual Max Health' 값으로 자체 초기화한다. (상자 등)\nFalse: 외부 시스템이 Initialize()를 호출해야 한다. (캐릭터 등)")]
        [SerializeField] private bool useManualInitialization = true;
        [SerializeField, ShowIf("useManualInitialization")] private float initialHealth = 100f;

        
        // --- 런타임 데이터 ---
        public float CurrentHealth { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;
        public bool IsInitialized { get; private set; } = false;

        // --- 핵심 이벤트 ---
        public event Action<float, float> OnHealthChanged;
        public event Action<DamageInfo> OnDamaged;
        public event Action OnDied;
        public event Action<float> OnHealed;
        public event Action OnRevived;

        private YisoDamageProcessor _damageProcessor;

        protected override void Awake() {
            base.Awake();
            _damageProcessor = GetComponent<YisoDamageProcessor>();
        }

        protected override void Start() {
            base.Start();
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

            _damageProcessor = GetComponent<YisoDamageProcessor>();
            
            IsInitialized = true;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void TakeDamage(DamageInfo damageInfo) {
            if (!IsInitialized || IsDead) {
                return;
            }
            
            var previousHealth = CurrentHealth;
            var finalDamage = _damageProcessor == null
                ? damageInfo.FinalDamage
                : _damageProcessor.FinalizeDamage(damageInfo);

            CurrentHealth = Math.Max(CurrentHealth - finalDamage, 0f);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
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