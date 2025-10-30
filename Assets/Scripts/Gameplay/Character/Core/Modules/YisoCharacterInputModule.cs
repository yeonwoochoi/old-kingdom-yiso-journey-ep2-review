using System;
using Gameplay.Character.StateMachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Character.Core.Modules {
    public enum ActionMapType {
        Player, UI
    }
    
    // TODO: 추가 action 구현하셈 (지금은 OnMove, OnAttack만 구현됨)
    public sealed class YisoCharacterInputModule : YisoCharacterModuleBase {
        private Settings _settings;
        private InputSystem_Actions _inputActions;
        
        private YisoCharacterStateModule _stateModule;
        
        public Vector2 MoveInput { get; private set; }

        public YisoCharacterInputModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
        }

        public override void Initialize() {
            base.Initialize();
            _inputActions = new InputSystem_Actions();
        }

        public override void LateInitialize() {
            base.LateInitialize();
            _stateModule = Context.GetModule<YisoCharacterStateModule>();
        }

        public override void OnEnable() {
            base.OnEnable();
            _inputActions.Player.Enable();
            
            _inputActions.Player.Move.performed += OnMove;
            _inputActions.Player.Move.canceled += OnMove;
            _inputActions.Player.Attack.performed += OnAttack;
        }

        public override void OnDisable() {
            base.OnDisable();

            _inputActions.Player.Move.performed -= OnMove;
            _inputActions.Player.Move.canceled -= OnMove;
            _inputActions.Player.Attack.performed -= OnAttack;
            
            _inputActions.Player.Disable();
        }

        public override void OnUpdate() {
            base.OnUpdate();
            if (_stateModule?.CurrentState.role == YisoStateRole.Move) {
                Context?.Move(MoveInput);
            }
        }

        public void SwitchActionMap(ActionMapType mapName) {
            switch (mapName) {
                case ActionMapType.Player:
                    _inputActions.UI.Disable();
                    _inputActions.Player.Enable();
                    break;
                case ActionMapType.UI:
                    _inputActions.Player.Disable();
                    _inputActions.UI.Enable();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mapName), mapName, null);
            }
        }

        private void OnMove(InputAction.CallbackContext context) {
            Debug.Log("InputModule: Move input received!");
            MoveInput = context.ReadValue<Vector2>();
            _stateModule?.RequestStateChangeByRole(MoveInput.sqrMagnitude > 0.01f
                ? YisoStateRole.Move
                : YisoStateRole.Idle);
        }

        private void OnAttack(InputAction.CallbackContext context) {
            Debug.Log("InputModule: Attack input received!");
            _stateModule?.RequestStateChangeByRole(YisoStateRole.Attack);
        }
        
        [Serializable] 
        public class Settings {}
    }
}