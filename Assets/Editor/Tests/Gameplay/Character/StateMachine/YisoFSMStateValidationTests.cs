using Editor.Tests.Utils;

namespace Gameplay.Character.StateMachine.Tests {
    using NUnit.Framework;
    using UnityEngine;
    using System.Collections.Generic;

    /// <summary>
    /// YisoCharacterStateSO.CanTransitionTo() 메서드의 동작을 검증하는 테스트.
    /// 현재 상태에서 목표 상태로 전환이 가능한지 확인하는 로직을 테스트합니다.
    /// </summary>
    public class YisoFSMStateValidationTests {
        private YisoCharacterStateSO _currentState;
        private YisoCharacterStateSO _targetState;
        private YisoCharacterStateSO _unlinkedState;

        [SetUp]
        public void Setup() {
            _currentState = ScriptableObject.CreateInstance<YisoCharacterStateSO>();
            _currentState.name = "CurrentState";

            _targetState = ScriptableObject.CreateInstance<YisoCharacterStateSO>();
            _targetState.name = "TargetState";

            _unlinkedState = ScriptableObject.CreateInstance<YisoCharacterStateSO>();
            _unlinkedState.name = "UnlinkedState";
        }

        [TearDown]
        public void TearDown() {
            Object.DestroyImmediate(_currentState);
            Object.DestroyImmediate(_targetState);
            Object.DestroyImmediate(_unlinkedState);
        }

        [Test]
        public void CanTransitionTo__WhenTransitionExists__ReturnsTrue() {
            // Arrange: Transition이 targetState로 연결되도록 설정
            var transition = ScriptableObject.CreateInstance<YisoCharacterTransitionSO>();
            TestUtils.SetPrivateField(transition, "_isTrueStateRandom", false);
            TestUtils.SetPrivateField(transition, "_trueState", _targetState);

            TestUtils.SetPrivateField(_currentState, "transitions", new List<YisoCharacterTransitionSO> {
                transition
            });

            // Act
            var result = _currentState.CanTransitionTo(_targetState);

            // Assert
            Assert.IsTrue(result, "유효한 Transition이 존재하면 true를 반환해야 함");

            // Cleanup
            Object.DestroyImmediate(transition);
        }

        [Test]
        public void CanTransitionTo__WhenNoTransitionExists__ReturnsFalse() {
            // Arrange: Transition이 다른 상태로만 연결됨
            var transition = ScriptableObject.CreateInstance<YisoCharacterTransitionSO>();
            TestUtils.SetPrivateField(transition, "_isTrueStateRandom", false);
            TestUtils.SetPrivateField(transition, "_trueState", _unlinkedState);

            TestUtils.SetPrivateField(_currentState, "transitions", new List<YisoCharacterTransitionSO> {
                transition
            });

            // Act
            var result = _currentState.CanTransitionTo(_targetState);

            // Assert
            Assert.IsFalse(result, "연결되지 않은 상태로는 전환할 수 없어야 함");

            // Cleanup
            Object.DestroyImmediate(transition);
        }

        [Test]
        public void CanTransitionTo__WhenMultipleTransitions__FindsCorrectOne() {
            // Arrange: 여러 Transition 중 하나가 targetState로 연결됨
            var transition1 = ScriptableObject.CreateInstance<YisoCharacterTransitionSO>();
            TestUtils.SetPrivateField(transition1, "_isTrueStateRandom", false);
            TestUtils.SetPrivateField(transition1, "_trueState", _unlinkedState);

            var transition2 = ScriptableObject.CreateInstance<YisoCharacterTransitionSO>();
            TestUtils.SetPrivateField(transition2, "_isTrueStateRandom", false);
            TestUtils.SetPrivateField(transition2, "_trueState", _targetState);

            TestUtils.SetPrivateField(_currentState, "transitions", new List<YisoCharacterTransitionSO> {
                transition1,
                transition2
            });

            // Act
            var result = _currentState.CanTransitionTo(_targetState);

            // Assert
            Assert.IsTrue(result, "여러 Transition 중 하나라도 연결되어 있으면 true를 반환해야 함");

            // Cleanup
            Object.DestroyImmediate(transition1);
            Object.DestroyImmediate(transition2);
        }

        [Test]
        public void CanTransitionTo__WhenTransitionListIsEmpty__ReturnsFalse() {
            // Arrange: Transition 목록이 비어있음
            TestUtils.SetPrivateField(_currentState, "transitions", new List<YisoCharacterTransitionSO>());

            // Act
            var result = _currentState.CanTransitionTo(_targetState);

            // Assert
            Assert.IsFalse(result, "Transition이 없으면 false를 반환해야 함");
        }

        [Test]
        public void CanTransitionTo__WhenTransitionListIsNull__ReturnsFalse() {
            // Arrange: Transition 목록이 null
            TestUtils.SetPrivateField(_currentState, "transitions", null);

            // Act
            var result = _currentState.CanTransitionTo(_targetState);

            // Assert
            Assert.IsFalse(result, "Transition 목록이 null이면 false를 반환해야 함");
        }

        [Test]
        public void CanTransitionTo__WhenTargetStateIsNull__ReturnsFalse() {
            // Arrange
            var transition = ScriptableObject.CreateInstance<YisoCharacterTransitionSO>();
            TestUtils.SetPrivateField(_currentState, "transitions", new List<YisoCharacterTransitionSO> {
                transition
            });

            // Act
            var result = _currentState.CanTransitionTo(null);

            // Assert
            Assert.IsFalse(result, "Target이 null이면 false를 반환해야 함");

            // Cleanup
            Object.DestroyImmediate(transition);
        }
    }
}
