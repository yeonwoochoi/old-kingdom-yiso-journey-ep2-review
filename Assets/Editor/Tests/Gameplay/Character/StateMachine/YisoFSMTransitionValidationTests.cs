using Editor.Tests.Utils;

namespace Gameplay.Character.StateMachine.Tests {
    using NUnit.Framework;
    using UnityEngine;
    using System.Collections.Generic;

    /// <summary>
    /// YisoCharacterTransitionSO.IsLinkedTo() 메서드의 동작을 검증하는 테스트.
    /// Transition이 특정 목표 상태로 연결되어 있는지 확인하는 로직을 테스트합니다.
    /// </summary>
    public class YisoFSMTransitionValidationTests {
        private YisoCharacterTransitionSO _transition;
        private YisoCharacterStateSO _targetState;
        private YisoCharacterStateSO _otherState;

        [SetUp]
        public void Setup() {
            _transition = ScriptableObject.CreateInstance<YisoCharacterTransitionSO>();

            _targetState = ScriptableObject.CreateInstance<YisoCharacterStateSO>();
            _targetState.name = "TargetState";

            _otherState = ScriptableObject.CreateInstance<YisoCharacterStateSO>();
            _otherState.name = "OtherState";
        }

        [TearDown]
        public void TearDown() {
            Object.DestroyImmediate(_transition);
            Object.DestroyImmediate(_targetState);
            Object.DestroyImmediate(_otherState);
        }

        #region True Path Tests

        [Test]
        public void IsLinkedTo__WhenTrueStateSingle__ReturnsTrue() {
            // Arrange: True 경로에 단일 상태 설정
            TestUtils.SetPrivateField(_transition, "_isTrueStateRandom", false);
            TestUtils.SetPrivateField(_transition, "_trueState", _targetState);

            // Act
            var result = _transition.IsLinkedTo(_targetState);

            // Assert
            Assert.IsTrue(result, "단일 True 상태가 target과 일치하면 true를 반환해야 함");
        }

        [Test]
        public void IsLinkedTo__WhenTrueStateList__ReturnsTrue() {
            // Arrange: True 경로에 랜덤 목록 설정
            TestUtils.SetPrivateField(_transition, "_isTrueStateRandom", true);
            TestUtils.SetPrivateField(_transition, "_trueStates", new List<YisoCharacterStateSO> {
                _otherState,
                _targetState
            });

            // Act
            var result = _transition.IsLinkedTo(_targetState);

            // Assert
            Assert.IsTrue(result, "True 상태 목록에 target이 포함되어 있으면 true를 반환해야 함");
        }

        #endregion

        #region False Path Tests

        [Test]
        public void IsLinkedTo__WhenFalseStateSingle__ReturnsTrue() {
            // Arrange: False 경로에 단일 상태 설정
            TestUtils.SetPrivateField(_transition, "_isFalseStateRandom", false);
            TestUtils.SetPrivateField(_transition, "_falseState", _targetState);

            // Act
            var result = _transition.IsLinkedTo(_targetState);

            // Assert
            Assert.IsTrue(result, "단일 False 상태가 target과 일치하면 true를 반환해야 함");
        }

        [Test]
        public void IsLinkedTo__WhenFalseStateList__ReturnsTrue() {
            // Arrange: False 경로에 랜덤 목록 설정
            TestUtils.SetPrivateField(_transition, "_isFalseStateRandom", true);
            TestUtils.SetPrivateField(_transition, "_falseStates", new List<YisoCharacterStateSO> {
                _targetState,
                _otherState
            });

            // Act
            var result = _transition.IsLinkedTo(_targetState);

            // Assert
            Assert.IsTrue(result, "False 상태 목록에 target이 포함되어 있으면 true를 반환해야 함");
        }

        #endregion

        #region Negative Tests

        [Test]
        public void IsLinkedTo__WhenNotLinked__ReturnsFalse() {
            // Arrange: True/False 모두 다른 상태로 설정
            TestUtils.SetPrivateField(_transition, "_isTrueStateRandom", false);
            TestUtils.SetPrivateField(_transition, "_trueState", _otherState);
            TestUtils.SetPrivateField(_transition, "_isFalseStateRandom", false);
            TestUtils.SetPrivateField(_transition, "_falseState", _otherState);

            // Act
            var result = _transition.IsLinkedTo(_targetState);

            // Assert
            Assert.IsFalse(result, "연결되지 않은 상태는 false를 반환해야 함");
        }

        [Test]
        public void IsLinkedTo__WhenTargetIsNull__ReturnsFalse() {
            // Arrange
            TestUtils.SetPrivateField(_transition, "_trueState", _targetState);

            // Act
            var result = _transition.IsLinkedTo(null);

            // Assert
            Assert.IsFalse(result, "null target은 항상 false를 반환해야 함");
        }

        [Test]
        public void IsLinkedTo__WhenListIsEmpty__ReturnsFalse() {
            // Arrange: 랜덤 모드이지만 목록이 비어있음
            TestUtils.SetPrivateField(_transition, "_isTrueStateRandom", true);
            TestUtils.SetPrivateField(_transition, "_trueStates", new List<YisoCharacterStateSO>());

            // Act
            var result = _transition.IsLinkedTo(_targetState);

            // Assert
            Assert.IsFalse(result, "빈 목록에서는 false를 반환해야 함");
        }

        #endregion
    }
}
