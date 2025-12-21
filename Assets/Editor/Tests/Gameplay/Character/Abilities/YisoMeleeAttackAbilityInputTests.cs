using Editor.Tests.Utils;

namespace Gameplay.Character.Abilities.Tests {
    using NUnit.Framework;
    using Moq;
    using UnityEngine;
    using Core;
    using Definitions;
    using Types;
    using StateMachine;

    /// <summary>
    /// YisoMeleeAttackAbility의 단발/연속 입력 모드를 검증하는 테스트.
    ///
    /// 제약사항: sealed Module 클래스들은 Mock 불가하므로,
    /// Ability의 내부 상태와 설정만 검증합니다.
    /// </summary>
    public class YisoMeleeAttackAbilityInputTests {
        private Mock<IYisoCharacterContext> _mockContext;
        private YisoMeleeAttackAbilitySO _abilitySO;
        private YisoMeleeAttackAbility _ability;
        private YisoCharacterStateSO _idleState;

        [SetUp]
        public void Setup() {
            _mockContext = new Mock<IYisoCharacterContext>();
            _mockContext.Setup(c => c.Type).Returns(CharacterType.Player);

            // Real State Instance (NO MOCKING!)
            _idleState = ScriptableObject.CreateInstance<YisoCharacterStateSO>();
            TestUtils.SetPrivateField(_idleState, "role", YisoStateRole.Idle);
            TestUtils.SetPrivateField(_idleState, "canCastAbility", true);
            _mockContext.Setup(c => c.GetCurrentState()).Returns(_idleState);

            // Ability SO Setup
            _abilitySO = ScriptableObject.CreateInstance<YisoMeleeAttackAbilitySO>();
        }

        [TearDown]
        public void TearDown() {
            if (_abilitySO != null) Object.DestroyImmediate(_abilitySO);
            if (_idleState != null) Object.DestroyImmediate(_idleState);
        }

        #region Configuration Tests

        [Test]
        public void AbilitySO__DefaultContinuousMode__IsFalse() {
            // Arrange & Act: 기본 설정 확인
            var defaultSO = ScriptableObject.CreateInstance<YisoMeleeAttackAbilitySO>();

            // Assert: 기본값은 단발 모드 (false)
            var continuousSetting = TestUtils.GetPrivateField<bool>(defaultSO, "continuousPressAttack");
            Assert.IsFalse(continuousSetting, "기본값은 단발 모드(false)여야 함");

            // Cleanup
            Object.DestroyImmediate(defaultSO);
        }

        [Test]
        public void AbilitySO__SetContinuousMode__IsStoredCorrectly() {
            // Arrange: 연속 모드 설정
            TestUtils.SetPrivateField(_abilitySO, "continuousPressAttack", true);

            // Act: Ability 생성
            _ability = new YisoMeleeAttackAbility(_abilitySO);

            // Assert: 설정이 Ability 내부에 저장되었는지 확인
            var settings = TestUtils.GetPrivateField<YisoMeleeAttackAbilitySO>(_ability, "_settings");
            Assert.IsNotNull(settings, "Settings가 null이면 안됨");

            var continuousSetting = TestUtils.GetPrivateField<bool>(settings, "continuousPressAttack");
            Assert.IsTrue(continuousSetting, "연속 모드 설정이 올바르게 저장되어야 함");
        }

        #endregion

        #region Internal State Tests

        [Test]
        public void Ability__InitialEdgeDetectionFlag__IsFalse() {
            // Arrange: 단발 모드로 설정
            TestUtils.SetPrivateField(_abilitySO, "continuousPressAttack", false);
            _ability = new YisoMeleeAttackAbility(_abilitySO);
            _ability.Initialize(_mockContext.Object);

            // Act: Edge Detection 플래그 확인
            var wasPressed = TestUtils.GetPrivateField<bool>(_ability, "_wasAttackPressedLastFrame");

            // Assert: 초기값은 false
            Assert.IsFalse(wasPressed, "Edge Detection 플래그의 초기값은 false여야 함");
        }

        [Test]
        public void Ability__InitialAttackingState__IsFalse() {
            // Arrange
            TestUtils.SetPrivateField(_abilitySO, "continuousPressAttack", false);
            _ability = new YisoMeleeAttackAbility(_abilitySO);
            _ability.Initialize(_mockContext.Object);

            // Act
            var isAttacking = TestUtils.GetPrivateField<bool>(_ability, "_isAttacking");

            // Assert
            Assert.IsFalse(isAttacking, "초기 공격 상태는 false여야 함");
        }

        #endregion

        #region IsAbilityEnabled Tests

        [Test]
        public void IsAbilityEnabled__WhenNotAttacking__ReturnsTrue() {
            // Arrange: 공격 중이 아닌 상태
            TestUtils.SetPrivateField(_abilitySO, "continuousPressAttack", false);
            _ability = new YisoMeleeAttackAbility(_abilitySO);
            _ability.Initialize(_mockContext.Object);

            // _isAttacking = false (기본값)

            // Act
            var isEnabled = _ability.IsAbilityEnabled;

            // Assert
            Assert.IsTrue(isEnabled, "공격 중이 아닐 때는 Ability가 활성화되어야 함");
        }

        [Test]
        public void IsAbilityEnabled__WhenAttacking__ReturnsFalse() {
            // Arrange: 공격 중인 상태로 설정
            TestUtils.SetPrivateField(_abilitySO, "continuousPressAttack", false);
            _ability = new YisoMeleeAttackAbility(_abilitySO);
            _ability.Initialize(_mockContext.Object);

            TestUtils.SetPrivateField(_ability, "_isAttacking", true);

            // Act
            var isEnabled = _ability.IsAbilityEnabled;

            // Assert
            Assert.IsFalse(isEnabled, "공격 중일 때는 Ability가 비활성화되어야 함");
        }

        [Test]
        public void IsAbilityEnabled__WhenStateDoesNotAllowCast__ReturnsFalse() {
            // Arrange: CanCastAbility = false 상태
            var restrictedState = ScriptableObject.CreateInstance<YisoCharacterStateSO>();
            TestUtils.SetPrivateField(restrictedState, "role", YisoStateRole.Hit);
            TestUtils.SetPrivateField(restrictedState, "canCastAbility", false);
            _mockContext.Setup(c => c.GetCurrentState()).Returns(restrictedState);

            TestUtils.SetPrivateField(_abilitySO, "continuousPressAttack", false);
            _ability = new YisoMeleeAttackAbility(_abilitySO);
            _ability.Initialize(_mockContext.Object);

            // Act
            var isEnabled = _ability.IsAbilityEnabled;

            // Assert
            Assert.IsFalse(isEnabled, "State에서 Ability 사용을 허용하지 않으면 비활성화되어야 함");

            // Cleanup
            Object.DestroyImmediate(restrictedState);
        }

        #endregion

        #region Integration Limitation Tests

        [Test]
        public void IntegrationTest__InputModuleDependency__RequiresRealModule() {
            // 이 테스트는 sealed Module 의존성 때문에 실행 불가
            // Integration Test 환경에서 실행 필요
            Assert.Ignore("Sealed Module classes (YisoCharacterInputModule, YisoCharacterWeaponModule) " +
                         "cannot be mocked. This test requires integration test setup with real modules.");
        }

        #endregion
    }
}
