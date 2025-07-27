using System;

namespace Gameplay.Character.Core.Modules {
    public sealed class YisoCharacterStateModule : YisoCharacterModuleBase {
        private Settings _settings;
        
        public YisoCharacterStateModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
        }
        
        [Serializable] 
        public class Settings {}
    }
}