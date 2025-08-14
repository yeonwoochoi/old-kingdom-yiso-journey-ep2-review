using System;

namespace Gameplay.Character.Core.Modules {
    public sealed class YisoCharacterSaveModule : YisoCharacterModuleBase {
        private Settings _settings;
        
        public YisoCharacterSaveModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
        }

        private void SavePlayerData() {
            if (!Context.IsPlayer) return;
            // TODO: Save Service 연동
        }

        [Serializable] 
        public class Settings {}
    }
}