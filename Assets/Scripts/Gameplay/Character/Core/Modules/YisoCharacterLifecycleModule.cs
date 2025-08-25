using System;
using Gameplay.Health;
using UnityEngine;

namespace Gameplay.Character.Core.Modules {
    public sealed class YisoCharacterLifecycleModule : YisoCharacterModuleBase {
        private Settings _settings;

        public float CurrentHealth { get; }
        public bool IsDead { get; }
        
        public YisoCharacterLifecycleModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
        }

        public void TakeDamage(float damage) {
            
        }

        [Serializable]
        public class Settings {
            [SerializeField] private YisoEntityHealth characterEntityHealth;
        }
    }
}