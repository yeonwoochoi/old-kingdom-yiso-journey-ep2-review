using System;
using Gameplay.Character.StateMachine;
using Gameplay.Health;
using UnityEngine;

namespace Gameplay.Character.Core.Modules {
    public sealed class YisoCharacterLifecycleModule : YisoCharacterModuleBase {
        private readonly Settings _settings;
        private YisoEntityHealth _entityHealth;
        private YisoCharacterStateModule _stateModule;

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
        }

        public override void LateInitialize() {
            base.LateInitialize();
            _stateModule = Context.GetModule<YisoCharacterStateModule>();
        }

        public void TakeDamage(DamageInfo damageInfo) {
            if (_entityHealth == null || _entityHealth.IsDead) return;
            _entityHealth.TakeDamage(damageInfo);
        }

        private void OnCharacterDied() {
            Debug.Log($"[{Context.GameObject.name}] 사망! LifecycleModule이 감지함.");
    
            _stateModule?.RequestStateChangeByRole(YisoStateRole.Died);
        }

        public override void OnDestroy() {
            base.OnDestroy();
            _entityHealth.OnDied -= OnCharacterDied;
        }

        [Serializable]
        public class Settings {
            public YisoEntityHealth characterEntityHealth;
        }
    }
}