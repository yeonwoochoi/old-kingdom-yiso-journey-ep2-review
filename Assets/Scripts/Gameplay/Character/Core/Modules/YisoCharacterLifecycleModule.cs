using System;
using Gameplay.Character.StateMachine;
using Gameplay.Health;
using UnityEngine;

namespace Gameplay.Character.Core.Modules {
    public sealed class YisoCharacterLifecycleModule : YisoCharacterModuleBase {
        private readonly Settings _settings;
        private YisoEntityHealth _entityHealth;
        private YisoCharacterStateModule _stateModule;
        private YisoCharacterAbilityModule _abilityModule;

        public float CurrentHealth => _entityHealth != null ? _entityHealth.CurrentHealth : 0f;
        public float MaxHealth => _entityHealth != null ? _entityHealth.MaxHealth : 0f;
        public bool IsDead => _entityHealth == null || _entityHealth.IsDead;
        
        public YisoCharacterLifecycleModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
        }

        public override void Initialize() {
            base.Initialize();
            _entityHealth = _settings.characterEntityHealth;

            if (_entityHealth == null) {
                if (!Context.GameObject.TryGetComponent(out _entityHealth)) {
                    Debug.LogError($"[{Context.GameObject.name}] LifecycleModule에 YisoEntityHealth가 주입되지 않았습니다!", Context.GameObject);
                    return;
                }
            }

            _entityHealth.OnDied += OnCharacterDied;
            _entityHealth.OnRevived += OnCharacterRevived;
        }

        public override void LateInitialize() {
            base.LateInitialize();
            _stateModule = Context.GetModule<YisoCharacterStateModule>();
            _abilityModule = Context.GetModule<YisoCharacterAbilityModule>();
        }

        public void TakeDamage(DamageInfo damageInfo) {
            if (_entityHealth == null || _entityHealth.IsDead) return;
            _entityHealth.TakeDamage(damageInfo);
        }

        private void OnCharacterDied() {
            Debug.Log($"[{Context.GameObject.name}] 사망 처리 시작.");

            // 1. 어빌리티들에게 사망 알림 (이펙트 끄기, 로직 중단 등)
            _abilityModule?.OnDeath();

            // 2. FSM 상태 전환
            _stateModule?.RequestStateChangeByRole(YisoStateRole.Died, true);
        }

        private void OnCharacterRevived() {
            Debug.Log($"[{Context.GameObject.name}] 부활 처리 시작.");

            // 1. 어빌리티들에게 부활 알림 (초기화, 잠금 해제)
            _abilityModule?.OnRevive();

            // 2. FSM을 Idle 상태로 전환
            _stateModule?.RequestStateChangeByRole(YisoStateRole.Idle);
        }

        public override void OnDestroy() {
            base.OnDestroy();
            if (_entityHealth != null) {
                _entityHealth.OnDied -= OnCharacterDied;
                _entityHealth.OnRevived -= OnCharacterRevived;
            }
        }

        [Serializable]
        public class Settings {
            public YisoEntityHealth characterEntityHealth;
        }
    }
}