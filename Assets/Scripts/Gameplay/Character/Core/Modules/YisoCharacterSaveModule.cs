using System;

namespace Gameplay.Character.Core.Modules {
    public sealed class YisoCharacterSaveModule : YisoCharacterModuleBase {
        private Settings _settings;
        
        public YisoCharacterSaveModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
        }

        private void SavePlayerData() {
            // TODO [YisoCharacterSaveModule.SavePlayerData] Save System 구현되면 연동
        }

        [Serializable] 
        public class Settings {}
    }
}