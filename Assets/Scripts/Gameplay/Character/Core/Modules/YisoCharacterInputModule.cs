using System;

namespace Gameplay.Character.Core.Modules {
    public sealed class YisoCharacterInputModule : YisoCharacterModuleBase {
        private Settings _settings;
        
        public YisoCharacterInputModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
        }
        
        [Serializable] 
        public class Settings {}
    }
}