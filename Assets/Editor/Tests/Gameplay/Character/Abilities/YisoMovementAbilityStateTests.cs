using Editor.Tests.Utils;

namespace Gameplay.Character.Abilities.Tests {
    using NUnit.Framework;
    using Moq;
    using UnityEngine;
    using Core;
    using Definitions;
    using StateMachine;

    /// <summary>
    /// YisoMovementAbility의 상태 전이 요청 로직을 검증하는 테스트.
    /// 이동 입력에 따른 Idle ↔ Move 상태 동기화를 테스트합니다.
    /// </summary>
    public class YisoMovementAbilityStateTests {
        private Mock<IYisoCharacterContext> _mockContext;
        private YisoMovementAbilitySO _abilitySO;
        private YisoMovementAbility _ability;
        private YisoCharacterStateSO _idleState;  // Real instance (not mocked)
        private YisoCharacterStateSO _moveState;  // Real instance (not mocked)
        private YisoCharacterStateSO _attackState; // Real instance (not mocked)
        private YisoCharacterStateSO _hitState;   // Real instance (not mocked)

        [SetUp]
        public void Setup() {
            _mockContext = new Mock<IYisoCharacterContext>();
            _abilitySO = ScriptableObject.CreateInstance<YisoMovementAbilitySO>();

            // Settings
            TestUtils.SetPrivateField(_abilitySO, "baseMovementSpeed", 5f);
            TestUtils.SetPrivateField(_abilitySO, "idleThreshold", 0.01f);

            _ability = new YisoMovementAbility(_abilitySO);
            _ability.Initialize(_mockContext.Object);

            // Real State Instances (NO MOCKING!)
            _idleState = ScriptableObject.CreateInstance<YisoCharacterStateSO>();
            TestUtils.SetPrivateField(_idleState, "role", YisoStateRole.Idle);
            TestUtils.SetPrivateField(_idleState, "canMove", true);

            _moveState = ScriptableObject.CreateInstance<YisoCharacterStateSO>();
            TestUtils.SetPrivateField(_moveState, "role", YisoStateRole.Move);
            TestUtils.SetPrivateField(_moveState, "canMove", true);

            _attackState = ScriptableObject.CreateInstance<YisoCharacterStateSO>();
            TestUtils.SetPrivateField(_attackState, "role", YisoStateRole.Attack);
            TestUtils.SetPrivateField(_attackState, "canMove", false);

            _hitState = ScriptableObject.CreateInstance<YisoCharacterStateSO>();
            TestUtils.SetPrivateField(_hitState, "role", YisoStateRole.Hit);
            TestUtils.SetPrivateField(_hitState, "canMove", false);
        }

        [TearDown]
        public void TearDown() {
            // ScriptableObject 메모리 정리 필수
            if (_abilitySO != null) Object.DestroyImmediate(_abilitySO);
            if (_idleState != null) Object.DestroyImmediate(_idleState);
            if (_moveState != null) Object.DestroyImmediate(_moveState);
            if (_attackState != null) Object.DestroyImmediate(_attackState);
            if (_hitState != null) Object.DestroyImmediate(_hitState);
        }

        #region Idle to Move Transition

        [Test]
        public void ProcessAbility__WhenIdleAndMoving__RequestsMoveState() {
            // Arrange: Idle 상태 + 이동 입력
            _mockContext.Setup(c => c.GetCurrentState()).Returns(_idleState);
            _mockContext.Setup(c => c.MovementVector).Returns(new Vector2(1f, 0f)); // 이동 입력 있음

            // Act: PreProcess → Process
            _ability.PreProcessAbility();
            _ability.ProcessAbility();

            // Assert
            _mockContext.Verify(c => c.RequestStateChangeByRole(YisoStateRole.Move), Times.Once,
                "Idle 상태에서 이동 입력이 있으면 Move 상태를 요청해야 함");
        }

        [Test]
        public void ProcessAbility__WhenIdleAndNotMoving__DoesNotRequestMoveState() {
            // Arrange: Idle 상태 + 이동 입력 없음
            _mockContext.Setup(c => c.GetCurrentState()).Returns(_idleState);
            _mockContext.Setup(c => c.MovementVector).Returns(Vector2.zero); // 이동 입력 없음

            // Act
            _ability.PreProcessAbility();
            _ability.ProcessAbility();

            // Assert
            _mockContext.Verify(c => c.RequestStateChangeByRole(YisoStateRole.Move), Times.Never,
                "Idle 상태에서 이동 입력이 없으면 상태 변경을 요청하지 않아야 함");
        }

        #endregion

        #region Move to Idle Transition

        [Test]
        public void ProcessAbility__WhenMovingAndInputStops__RequestsIdleState() {
            // Arrange: Move 상태 + 이동 입력 중단
            _mockContext.Setup(c => c.GetCurrentState()).Returns(_moveState);
            _mockContext.Setup(c => c.MovementVector).Returns(Vector2.zero); // 입력 중단

            // Act
            _ability.PreProcessAbility();
            _ability.ProcessAbility();

            // Assert
            _mockContext.Verify(c => c.RequestStateChangeByRole(YisoStateRole.Idle), Times.Once,
                "Move 상태에서 이동 입력이 중단되면 Idle 상태를 요청해야 함");
        }

        [Test]
        public void ProcessAbility__WhenMovingAndInputContinues__DoesNotRequestIdleState() {
            // Arrange: Move 상태 + 이동 입력 계속
            _mockContext.Setup(c => c.GetCurrentState()).Returns(_moveState);
            _mockContext.Setup(c => c.MovementVector).Returns(new Vector2(0.5f, 0.5f)); // 계속 이동

            // Act
            _ability.PreProcessAbility();
            _ability.ProcessAbility();

            // Assert
            _mockContext.Verify(c => c.RequestStateChangeByRole(YisoStateRole.Idle), Times.Never,
                "Move 상태에서 이동 입력이 계속되면 Idle을 요청하지 않아야 함");
        }

        #endregion

        #region Non-Interfering States

        [Test]
        public void ProcessAbility__WhenAttacking__DoesNotRequestStateChange() {
            // Arrange: Attack 상태 + 이동 입력 (Attack 중에는 간섭하지 않음)
            _mockContext.Setup(c => c.GetCurrentState()).Returns(_attackState);
            _mockContext.Setup(c => c.MovementVector).Returns(new Vector2(1f, 0f));

            // Act
            _ability.PreProcessAbility();
            _ability.ProcessAbility();

            // Assert: Move 또는 Idle로의 전이 요청이 없어야 함
            _mockContext.Verify(c => c.RequestStateChangeByRole(YisoStateRole.Move), Times.Never,
                "Attack 상태에서는 Move 상태를 요청하지 않아야 함");
            _mockContext.Verify(c => c.RequestStateChangeByRole(YisoStateRole.Idle), Times.Never,
                "Attack 상태에서는 Idle 상태를 요청하지 않아야 함");
        }

        [Test]
        public void ProcessAbility__WhenHit__DoesNotRequestStateChange() {
            // Arrange: Hit 상태 (피격 중)
            _mockContext.Setup(c => c.GetCurrentState()).Returns(_hitState);
            _mockContext.Setup(c => c.MovementVector).Returns(Vector2.zero);

            // Act
            _ability.PreProcessAbility();
            _ability.ProcessAbility();

            // Assert
            _mockContext.Verify(c => c.RequestStateChangeByRole(It.IsAny<YisoStateRole>()), Times.Never,
                "Hit 상태에서는 상태 변경을 요청하지 않아야 함");
        }

        #endregion

        #region Threshold Tests

        [Test]
        public void ProcessAbility__WhenInputBelowThreshold__TreatedAsZero() {
            // Arrange: Idle 상태 + 매우 작은 입력 (threshold 이하)
            _mockContext.Setup(c => c.GetCurrentState()).Returns(_idleState);
            _mockContext.Setup(c => c.MovementVector).Returns(new Vector2(0.005f, 0.005f)); // sqrMagnitude < 0.01

            // Act
            _ability.PreProcessAbility();
            _ability.ProcessAbility();

            // Assert
            _mockContext.Verify(c => c.RequestStateChangeByRole(YisoStateRole.Move), Times.Never,
                "입력이 threshold 이하이면 이동으로 간주하지 않아야 함");
        }

        [Test]
        public void ProcessAbility__WhenInputAboveThreshold__TreatedAsMoving() {
            // Arrange: Idle 상태 + threshold 초과 입력
            _mockContext.Setup(c => c.GetCurrentState()).Returns(_idleState);
            _mockContext.Setup(c => c.MovementVector).Returns(new Vector2(0.1f, 0.1f)); // sqrMagnitude > 0.01

            // Act
            _ability.PreProcessAbility();
            _ability.ProcessAbility();

            // Assert
            _mockContext.Verify(c => c.RequestStateChangeByRole(YisoStateRole.Move), Times.Once,
                "입력이 threshold 초과이면 이동으로 간주해야 함");
        }

        #endregion

        #region Movement Permission

        [Test]
        public void ProcessAbility__WhenMovementNotPermitted__DoesNotMove() {
            // Arrange: CanMove = false 상태 (Attack 상태 사용)
            _mockContext.Setup(c => c.GetCurrentState()).Returns(_attackState);
            _mockContext.Setup(c => c.MovementVector).Returns(new Vector2(1f, 0f));

            // Act
            _ability.PreProcessAbility();
            _ability.ProcessAbility();

            // Assert: Move(Vector2.zero) 호출 확인
            _mockContext.Verify(c => c.Move(Vector2.zero), Times.Once,
                "이동이 허용되지 않으면 Move(zero)를 호출해야 함");
        }

        #endregion
    }
}
