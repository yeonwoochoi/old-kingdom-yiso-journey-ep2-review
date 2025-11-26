using Editor.Tests.Utils;

namespace Gameplay.Character.StateMachine.Tests {
    using NUnit.Framework;
    using Moq;
    using UnityEngine;
    using Core;
    using System.Collections.Generic;
    using Core.Modules;
    using Data;

    public class YisoFSMTransitionTests {
        private Mock<IYisoCharacterContext> _mockContext;
        private YisoCharacterTransitionSO _transition;

        private YisoCharacterStateSO _trueState;
        private YisoCharacterStateSO _falseState;

        [SetUp]
        public void Setup() {
            _mockContext = new Mock<IYisoCharacterContext>();
            _transition = ScriptableObject.CreateInstance<YisoCharacterTransitionSO>();

            _trueState = ScriptableObject.CreateInstance<YisoCharacterStateSO>();
            _trueState.name = "TrueState";

            _falseState = ScriptableObject.CreateInstance<YisoCharacterStateSO>();
            _falseState.name = "FalseState";

            TestUtils.SetPrivateField(_transition, "_trueState", _trueState);
            TestUtils.SetPrivateField(_transition, "_falseState", _falseState);
        }

        [Test]
        public void CheckTransition__WhenDecisionIsTrue__ReturnsTrueState() {
            // Arrange
            // 1. 가짜 Decision 만들기 (무조건 True 반환)
            var mockDecision = new Mock<YisoCharacterDecisionSO>();
            mockDecision.Setup(d => d.Decide(_mockContext.Object)).Returns(true);

            // 2. Transition에 Decision 리스트 주입 (Reflection)
            var decisions = new List<YisoCharacterDecisionSO> {mockDecision.Object};
            TestUtils.SetPrivateField(_transition, "_decisions", decisions);

            // Act
            var result = _transition.CheckTransition(_mockContext.Object, out var nextState);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(_trueState, nextState);
        }

        [Test]
        public void CheckTransition__WhenDecisionIsFalse__ReturnsFalseState() {
            // Arrange
            // 1. 가짜 Decision 만들기 (무조건 False 반환)
            var mockDecision = new Mock<YisoCharacterDecisionSO>();
            mockDecision.Setup(d => d.Decide(_mockContext.Object)).Returns(false);

            // 2. Transition 설정
            var decisions = new List<YisoCharacterDecisionSO> {mockDecision.Object};
            TestUtils.SetPrivateField(_transition, "_decisions", decisions);

            // Act
            bool result = _transition.CheckTransition(_mockContext.Object, out var nextState);

            // Assert
            // FalseState가 설정되어 있다면 result는 true여야 함 (상태 변경이 일어났으므로)
            Assert.IsTrue(result);
            Assert.AreEqual(_falseState, nextState);
        }

        [Test]
        public void CheckTransition__WhenAllDecisionsTrue__ReturnsTrueState() {
            // Arrange (여러 Decision이 모두 True여야 하는 경우)
            var d1 = new Mock<YisoCharacterDecisionSO>();
            d1.Setup(d => d.Decide(It.IsAny<IYisoCharacterContext>())).Returns(true);

            var d2 = new Mock<YisoCharacterDecisionSO>();
            d2.Setup(d => d.Decide(It.IsAny<IYisoCharacterContext>())).Returns(true);

            TestUtils.SetPrivateField(_transition, "_decisions", new List<YisoCharacterDecisionSO> {d1.Object, d2.Object});

            // Act
            _transition.CheckTransition(_mockContext.Object, out var nextState);

            // Assert
            Assert.AreEqual(_trueState, nextState);
        }
    }
}