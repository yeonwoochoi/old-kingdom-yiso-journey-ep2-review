using System;
using Gameplay.Character.StateMachine;

namespace Gameplay.Character.Core.Modules {
    public sealed class YisoCharacterStateModule : YisoCharacterModuleBase {
        private Settings _settings;
        
        public YisoCharacterStateModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
        }

        public void RequestStateChange(string newStateName) {
            
        }

        public void RequestStateChange(YisoCharacterStateSO newState) {
            
        }
        
        [Serializable] 
        public class Settings {}
    }
}