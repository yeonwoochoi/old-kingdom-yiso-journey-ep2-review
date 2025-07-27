using System;

namespace Gameplay.Character.Core.Modules {
    public sealed class YisoCharacterCoreModule: YisoCharacterModuleBase {
        private Settings _settings;
        public YisoCharacterCoreModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
        }
        
        [Serializable] 
        public class Settings {}
    }
}