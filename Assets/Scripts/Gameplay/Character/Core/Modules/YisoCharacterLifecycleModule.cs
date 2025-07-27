using System;
using Gameplay.Health;
using UnityEngine;

namespace Gameplay.Character.Core.Modules {
    public sealed class YisoCharacterLifecycleModule : YisoCharacterModuleBase {
        private Settings _settings;
        
        public YisoCharacterLifecycleModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
        }

        [Serializable]
        public class Settings {
            [SerializeField] private YisoHealth characterHealth;
        }
    }
}