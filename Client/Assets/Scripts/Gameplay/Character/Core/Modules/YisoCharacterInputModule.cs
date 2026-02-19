using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Character.Core.Modules {
    public enum ActionMapType {
        Player, UI
    }
    
    // TODO: 추가 action 구현하셈 (지금은 OnMove, OnAttack만 구현됨)
    /// <summary>
    /// InputModule은 InputSystem의 이벤트를 받아 입력 데이터를 갱신하는 역할만 수행합니다.
    /// 로직 실행(Move 호출, State 변경 등)은 Ability나 FSM에서 이 데이터를 Pull하여 처리합니다.
    /// </summary>
    public sealed class YisoCharacterInputModule : YisoCharacterModuleBase {
        private Settings _settings;
        private InputSystem_Actions _inputActions;
        private YisoCharacterWeaponModule _weaponModule;

        /// <summary>
        /// 현재 프레임의 이동 입력 벡터 (WASD, 방향키 등)
        /// </summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>
        /// 공격 버튼이 현재 눌려있는지 여부 (performed: true, canceled: false)
        /// </summary>
        public bool AttackInput { get; private set; }

        public YisoCharacterInputModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
        }

        public override void Initialize() {
            base.Initialize();
            _inputActions = new InputSystem_Actions();
            _weaponModule = Context.GetModule<YisoCharacterWeaponModule>();
        }

        public override void OnEnable() {
            base.OnEnable();
            _inputActions.Player.Enable();

            _inputActions.Player.Move.performed += OnMove;
            _inputActions.Player.Move.canceled += OnMove;
            _inputActions.Player.Attack.performed += OnAttack;
            _inputActions.Player.Attack.canceled += OnAttack;
            _inputActions.Player.ChangeWeapon.performed += OnChangeWeapon; 
        }

        public override void OnDisable() {
            base.OnDisable();

            _inputActions.Player.Move.performed -= OnMove;
            _inputActions.Player.Move.canceled -= OnMove;
            _inputActions.Player.Attack.performed -= OnAttack;
            _inputActions.Player.Attack.canceled -= OnAttack;
            _inputActions.Player.ChangeWeapon.performed -= OnChangeWeapon;

            _inputActions.Player.Disable();
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
            // Pull 방식: 데이터만 갱신하고, 로직 실행은 MovementAbility가 담당
            MoveInput = context.ReadValue<Vector2>();
        }

        private void OnAttack(InputAction.CallbackContext context) {
            // Pull 방식: 버튼 상태만 갱신 (performed: true, canceled: false)
            // 단발/연속 입력 구분은 Ability에서 처리
            if (context.performed) {
                AttackInput = true;
            }
            else if (context.canceled) {
                AttackInput = false;
            }
        }
        
        private void OnChangeWeapon(InputAction.CallbackContext context)
        {
            if (context.performed) {
                _weaponModule.ChangeWeapon();
            }
        }
        
        [Serializable] 
        public class Settings {}
    }
}