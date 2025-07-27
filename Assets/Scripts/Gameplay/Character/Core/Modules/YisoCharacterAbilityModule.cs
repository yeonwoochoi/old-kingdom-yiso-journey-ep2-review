using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Character.Abilities;

namespace Gameplay.Character.Core.Modules {
    public sealed class YisoCharacterAbilityModule : YisoCharacterModuleBase {
        private Dictionary<Type, IYisoCharacterAbility> _abilities;
        private Settings _settings;
        
        public IReadOnlyList<IYisoCharacterAbility> Abilities => _abilities.Values.ToList();

        public YisoCharacterAbilityModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
            _abilities = new Dictionary<Type, IYisoCharacterAbility>();
        }

        public override void Initialize() {
            base.Initialize();
            // TODO
        }

        public override void OnUpdate() {
            base.OnUpdate();
            UpdateAbilities();
        }

        private void UpdateAbilities() {
            if (Context.Animator == null) return;
            // TODO
        }
        
        [Serializable]
        public class Settings {
        }
    }
}