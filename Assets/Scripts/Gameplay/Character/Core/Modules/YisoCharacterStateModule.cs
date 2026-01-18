using System;
using Gameplay.Character.StateMachine;
using Gameplay.Character.Types;
using UnityEngine;
using Utils;

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
                if (_settings.stateMachinePrefab != null) {
                    var stateMachineObj = Context.Transform.Instantiate(_settings.stateMachinePrefab);
                    _stateMachine = stateMachineObj.GetComponent<YisoCharacterStateMachine>();
                }
            
                if (_stateMachine == null) {
                    YisoLogger.LogError($"StateMachine을 찾을 수 없습니다! {Context.GameObject.name}");
                } else {
                    YisoLogger.Log($"StateModule 초기화: FSM={_stateMachine.name}");
                }
            
                _stateMachine.PreInitialize(Context);
            }
        }

        public override void LateInitialize() {
            base.LateInitialize();
            _stateMachine?.Initialize();
        }

        [Serializable]
        public class Settings {
            public YisoCharacterStateMachine stateMachinePrefab;
            public Transform parent;
        }
    }
}