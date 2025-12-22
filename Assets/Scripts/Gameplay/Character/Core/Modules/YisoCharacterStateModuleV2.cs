using System;

namespace Gameplay.Character.Core.Modules {
    public class YisoCharacterStateModuleV2: YisoCharacterModuleBase {
        private readonly Settings _settings;
        
        public YisoCharacterStateModuleV2(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
        }
        
        [Serializable]
        public class Settings {
            
        }
    }
}