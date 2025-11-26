using Editor.Tests.Utils;

namespace Gameplay.Character.StateMachine.Decisions.Tests {
    using NUnit.Framework;
    using Moq;
    using UnityEngine;
    using Core;
    using Core.Modules;
    using Data;

    public class YisoFSMDecisionTests {
        private Mock<IYisoCharacterContext> _mockContext;
        private YisoCharacterBlackboardModule _blackboardModule;
        private YisoCharacterStateModule _stateModule;
        private YisoBlackboardKeySO _targetTimeKey;

        [SetUp]
        public void Setup() {
            _mockContext = new Mock<IYisoCharacterContext>();

            _blackboardModule = new YisoCharacterBlackboardModule(_mockContext.Object);
            _stateModule = new YisoCharacterStateModule(_mockContext.Object, new YisoCharacterStateModule.Settings());

            _mockContext.Setup(c => c.GetModule<YisoCharacterBlackboardModule>()).Returns(_blackboardModule);
            _mockContext.Setup(c => c.GetModule<YisoCharacterStateModule>()).Returns(_stateModule);

            _targetTimeKey = ScriptableObject.CreateInstance<YisoBlackboardKeySO>();
        }

        [Test]
        public void CheckWaitTime__WhenTimeNotPassed__ReturnsFalse() {
            // Arrange
            var decision = ScriptableObject.CreateInstance<YisoCharacterDecisionSetRandomWaitTimeSO>(); // 이름 수정필요 (CheckWaitTimeSO)
            TestUtils.SetPrivateField(decision, "targetTimeKey", _targetTimeKey); // private 필드 주입

            // 목표 시간 3초 설정
            _blackboardModule.SetFloat(_targetTimeKey, 3.0f);

            // 현재 시간 2초로 설정 (Reflection 사용)
            TestUtils.SetPrivateProperty(_stateModule, "TimeInCurrentState", 2.0f);

            // Act & Assert
            Assert.IsFalse(decision.Decide(_mockContext.Object));
        }

        [Test]
        public void CheckWaitTime__WhenTimePassed__ReturnsTrue() {
            // Arrange
            var decision = ScriptableObject.CreateInstance<YisoCharacterDecisionSetRandomWaitTimeSO>();
            TestUtils.SetPrivateField(decision, "targetTimeKey", _targetTimeKey);

            _blackboardModule.SetFloat(_targetTimeKey, 3.0f);
        
            // 현재 시간 3.1초로 설정
            TestUtils.SetPrivateProperty(_stateModule, "TimeInCurrentState", 3.1f);

            // Act & Assert
            Assert.IsTrue(decision.Decide(_mockContext.Object));
        }
    }
}