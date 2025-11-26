using Editor.Tests.Utils;

namespace Gameplay.Character.Abilities.Tests {
    using NUnit.Framework;
    using Moq;
    using UnityEngine;
    using Core;
    using Definitions;
    using StateMachine;
    using Core.Modules;
    
    public class YisoMovementAbilityTests {
        private Mock<IYisoCharacterContext> _mockContext;
        private YisoMovementAbilitySO _settings;
        private YisoMovementAbility _movementAbility;

        [SetUp]
        public void Setup() {
            _settings = ScriptableObject.CreateInstance<YisoMovementAbilitySO>();
            _settings.acceleration = 999f;
            _settings.deceleration = 999f;
            _settings.idleThreshold = 0.01f;
            _settings.baseMovementSpeed = 5f;
            
            _mockContext = new Mock<IYisoCharacterContext>();
            _movementAbility = new YisoMovementAbility(_settings);
            _movementAbility.Initialize(_mockContext.Object);
        }

        [Test]
        public void ProcessAbility__WhenCharacterCanMoveAndHasInput__CallsMoveWithCorrectVector() {
            // --- 1. Arrange (준비) ---
            var inputVector = new Vector2(1, 0);

            // Context.MovementVector를 호출하면 inputVector를 반환하도록 설정
            _mockContext.Setup(ctx => ctx.MovementVector).Returns(inputVector);

            // Context.GetCurrentState().canMove가 true를 반환하도록 설정
            var mockState = ScriptableObject.CreateInstance<YisoCharacterStateSO>();
            TestUtils.SetPrivateField(mockState, "canMove", true);
            _mockContext.Setup(ctx => ctx.GetCurrentState()).Returns(mockState);


            // --- 2. Act (실행) ---
            // 실제 AbilityModule이 호출하는 순서대로 실행
            _movementAbility.PreProcessAbility();
            _movementAbility.ProcessAbility();


            // --- 3. Assert (검증) ---
            // Context.Move가 '한 번' 호출되었는지, 그리고 인자로 전달된 벡터의 x값이 0보다 큰지 검증합니다.
            var expectedSpeed = _settings.baseMovementSpeed;
            _mockContext.Verify(ctx => ctx.Move(It.Is<Vector2>(v => v.x == expectedSpeed && v.y == 0)), Times.Once());
        }

        [Test]
        public void ProcessAbility__WhenCharacterCannotMove__CallsMoveWithZeroVector() {
            var inputVector = new Vector2(1, 0);
            _mockContext.Setup(ctx => ctx.MovementVector).Returns(inputVector);
            
            var mockState = ScriptableObject.CreateInstance<YisoCharacterStateSO>();
            TestUtils.SetPrivateField(mockState, "canMove", false);
            _mockContext.Setup(ctx => ctx.GetCurrentState()).Returns(mockState);
            
            _movementAbility.PreProcessAbility();
            _movementAbility.ProcessAbility();
            
            _mockContext.Verify(ctx => ctx.Move(Vector2.zero), Times.Once());
        }

        [Test]
        public void UpdateAnimator__WhenMoving__CallsPlayAnimationWithCorrectMoveSpeed() {
            var inputVector = new Vector2(1, 0);
            _mockContext.Setup(ctx => ctx.MovementVector).Returns(inputVector);
            
            var mockState = ScriptableObject.CreateInstance<YisoCharacterStateSO>();
            TestUtils.SetPrivateField(mockState, "canMove", true);
            _mockContext.Setup(ctx => ctx.GetCurrentState()).Returns(mockState);
            
            _movementAbility.PreProcessAbility();
            _movementAbility.ProcessAbility();
            _movementAbility.UpdateAnimator();
            
            _mockContext.Verify(ctx => ctx.PlayAnimation(
                YisoCharacterAnimationState.MoveSpeed,
                It.Is<float>(speed => speed > 0 && speed <= 1.0f)
            ), Times.Once());
        }
    }
}