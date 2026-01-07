using System;
using Gameplay.Character.StateMachine;
using Gameplay.Character.Types;
using UnityEngine;

namespace Gameplay.Character.Core.Modules {
    public class YisoCharacterStateModule: YisoCharacterModuleBase {
        private readonly Settings _settings;
        private YisoCharacterStateMachine _stateMachine;
        
        public string CurrentState => _stateMachine?.CurrentState?.StateName ?? "None";
        
        public YisoCharacterStateModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
        }

        public override void Initialize() {
            base.Initialize();
            if (Context.Type != CharacterType.Player) {
                if (_settings.stateMachine != null) {
                    _stateMachine = _settings.stateMachine;
                }
            
                if (_stateMachine == null) {
                    Debug.LogError($"[YisoCharacterStateModule] StateMachine을 찾을 수 없습니다! {Context.GameObject.name}");
                }
            
                _settings.stateMachine.PreInitialize(Context);
            }
        }

        public override void LateInitialize() {
            base.LateInitialize();
            _stateMachine?.Initialize();
        }

        [Serializable]
        public class Settings {
            public YisoCharacterStateMachine stateMachine;
        }
    }
}