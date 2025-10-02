using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Test {
    [RequireComponent(typeof(Rigidbody2D))]
    public class YisoPlayerMove : MonoBehaviour {
        public float speed = 10f;

        private Rigidbody2D _rigidbody2D;
        private Vector2 _moveInput;

        // InputSystem_Actions가 아닌, 그 자체인 InputSystem_Actions 타입으로 선언
        private InputSystem_Actions _inputActions;

        private void Awake() {
            // 1. 입력 시스템의 새 인스턴스(복사본)를 생성합니다.
            _inputActions = new InputSystem_Actions();
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        private void OnEnable() {
            // 2. 이벤트를 구독하기 전에, 'Player' 액션 맵을 반드시 활성화합니다.
            _inputActions.Player.Enable();

            // 3. 'Player' 액션 맵 안에 있는 'Move' 액션의 이벤트를 구독합니다.
            _inputActions.Player.Move.performed += OnMove;
            _inputActions.Player.Move.canceled += OnMove; // 키를 뗐을 때도 OnMove가 호출되도록 추가
        }

        private void OnDisable() {
            // 4. 비활성화 시에는 액션 맵을 끄고, 이벤트 구독을 해지합니다.
            _inputActions.Player.Move.performed -= OnMove;
            _inputActions.Player.Move.canceled -= OnMove;
            _inputActions.Player.Disable();
        }

        private void FixedUpdate() {
            // sqrMagnitude 체크는 더 이상 필요 없습니다. _moveInput이 0이면 이동량도 0이 됩니다.
            _rigidbody2D.MovePosition(_rigidbody2D.position + _moveInput * speed * Time.fixedDeltaTime);
        }

        private void OnMove(InputAction.CallbackContext context) {
            // 5. 콜백 함수에서는 ReadValue<Vector2>()로 현재 입력 값을 읽어오기만 하면 됩니다.
            _moveInput = context.ReadValue<Vector2>();
        }
    }
}