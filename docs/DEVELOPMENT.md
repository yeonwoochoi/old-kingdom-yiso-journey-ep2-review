# Development Guide

This document covers development environment setup, testing, building, and dependency management.

## Table of Contents
- [Common Commands](#common-commands)
- [Git Workflow](#git-workflow)
- [Testing Infrastructure](#testing-infrastructure)
- [Key Dependencies](#key-dependencies)
- [Important Development Notes](#important-development-notes)

---

## Common Commands

### Running Tests

Tests are Unity Editor tests using NUnit framework with Moq for mocking.

**Steps**:
1. Open Unity Editor (Unity 6000.0.62f1)
2. Navigate to: **Window → General → Test Runner**
3. Select **EditMode** tab to see all unit tests
4. Click **Run All** or select individual tests

**Test Location**: `Assets/Editor/Tests/`

**Note**: Unity Test Framework doesn't support command-line test execution without additional setup. Tests must be run within the Unity Editor.

---

### Building the Project

#### Via Unity Editor (Recommended)
1. Open Unity Editor and load the project
2. Navigate to: **File → Build Settings**
3. Select target platform (Windows, Mac, Linux, etc.)
4. Click **Build** or **Build and Run**

#### Via Command Line
```bash
Unity.exe -quit -batchmode -projectPath "." -executeMethod BuildScript.Build
```

**Note**: Requires a custom `BuildScript.Build` method to be implemented.

---

### Opening the Project

**Unity Version**: 6000.0.62f1

**Recommended Method**: Open with Unity Hub
1. Open Unity Hub
2. Click **Add** and select project directory
3. Select Unity version **6000.0.62f1**
4. Click on project to open

---

## Git Workflow

The project uses **automated versioning** via GitHub Actions.

### Automatic Version Bumping

- Push to `main` branch triggers automatic patch version bump
- Use semantic versioning keywords in commit messages:
  - `major:` - Increments major version (1.0.0 → 2.0.0)
  - `minor:` - Increments minor version (1.0.0 → 1.1.0)
  - Default (no keyword) - Increments patch version (1.0.0 → 1.0.1)

### Example Commit Messages

```bash
# Patch version bump (default)
git commit -m "Fix animation bug in melee attack"

# Minor version bump
git commit -m "minor: Add new dash ability system"

# Major version bump
git commit -m "major: Overhaul FSM architecture to Ver2"
```

### GitHub Releases

- GitHub releases are auto-generated with commit-based release notes
- Each release includes version tag and changelog

---

## Testing Infrastructure

### Test Framework

- **Location**: `Assets/Editor/Tests/`
- **Framework**: NUnit with Moq for mocking
- **Current Status**: Test files were deleted during Ver2 FSM migration (commit `2c61734`)
- **Utilities**: `TestUtils.cs` provides reflection helpers for future test development

---

### Test Utilities

**TestUtils.cs** (`Assets/Editor/Tests/Utils/TestUtils.cs`)

Provides reflection-based utilities for accessing private fields and properties in tests.

#### Available Methods

```csharp
// Field access
TestUtils.SetPrivateField(object target, string fieldName, object value);
T TestUtils.GetPrivateField<T>(object target, string fieldName);

// Property access
TestUtils.SetPrivateProperty(object target, string propertyName, object value);
T TestUtils.GetPrivateProperty<T>(object target, string propertyName);
```

#### Safe Reflection Pattern Example

```csharp
[Test]
public void TestAbilityInternalState() {
    var ability = new YisoMeleeAttackAbility();

    // Type-safe field reading
    var wasPressed = TestUtils.GetPrivateField<bool>(ability, "_wasAttackPressedLastFrame");
    Assert.IsFalse(wasPressed);

    // Setting private fields
    TestUtils.SetPrivateField(ability, "_attackCooldown", 0.5f);

    // Reading back to verify
    var cooldown = TestUtils.GetPrivateField<float>(ability, "_attackCooldown");
    Assert.AreEqual(0.5f, cooldown);
}
```

**Error Reporting**: Type-safe error reporting with available field/property names if not found.

---

### Testing Guidelines for Future Development

#### What Can Be Mocked

✅ **Interfaces**: `IYisoCharacterContext`, `IYisoCharacterModule`
```csharp
var mockContext = new Mock<IYisoCharacterContext>();
mockContext.Setup(x => x.IsMovementAllowed).Returns(true);
```

❌ **ScriptableObjects**: NEVER mock (use CreateInstance instead)
```csharp
// CORRECT
var abilitySO = ScriptableObject.CreateInstance<YisoMovementAbilitySO>();
TestUtils.SetPrivateField(abilitySO, "priority", 100);

// WRONG - Don't do this
var mockAbilitySO = new Mock<YisoMovementAbilitySO>(); // Will fail
```

❌ **Sealed Classes** (all Modules): NEVER mock
- All `YisoCharacter*Module` classes are sealed
- Focus on Ability logic tests or integration tests
- Use real module instances or mock the interface instead

---

#### ScriptableObject Testing Pattern

```csharp
[Test]
public void TestAbilitySO() {
    // Real instance creation (DO NOT mock)
    var abilitySO = ScriptableObject.CreateInstance<YisoMovementAbilitySO>();

    // Set private fields using TestUtils
    TestUtils.SetPrivateField(abilitySO, "priority", 100);
    TestUtils.SetPrivateField(abilitySO, "moveSpeed", 5.0f);

    // Test behavior
    var ability = abilitySO.CreateAbility();
    Assert.IsNotNull(ability);
}

[TearDown]
public void Cleanup() {
    // Memory cleanup - IMPORTANT!
    Object.DestroyImmediate(abilitySO);
}
```

---

#### Module Testing Pattern

Since modules are sealed, test their behavior through integration tests or by mocking `IYisoCharacterContext`.

```csharp
[Test]
public void TestAbilityModuleIntegration() {
    // Create real character and module
    var character = new GameObject().AddComponent<YisoCharacter>();

    // Initialize (this creates all modules internally)
    character.Initialize();

    // Test module behavior through character context
    var abilityModule = character.GetModule<YisoCharacterAbilityModule>();
    Assert.IsNotNull(abilityModule);

    // Cleanup
    Object.DestroyImmediate(character.gameObject);
}
```

---

## Key Dependencies

### NuGet Packages (packages.config)

- **Moq 4.20.72** - Mocking framework for unit tests
- **R3 1.3.0** - Reactive Extensions for Unity
- **Castle.Core 5.1.1** - Moq dependency (auto-installed)

**Location**: `Assets/Packages/`

---

### Unity Packages (critical)

#### Git Packages (manifest.json)
```json
"com.cysharp.r3": "https://github.com/Cysharp/R3.git",
"com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git",
"com.github-glitchenzo.nugetforunity": "https://github.com/GlitchEnzo/NuGetForUnity.git"
```

#### Standard Packages
- **com.unity.inputsystem** - New Input System (required for player input)
- **com.unity.addressables** - Asset management system
- **com.unity.localization** - Localization support
- **com.unity.test-framework** - Unit testing framework (NUnit)
- **com.unity.render-pipelines.universal** - URP rendering pipeline

**Configuration**: `Packages/manifest.json`

---

### Third-Party Assets

#### Odin Inspector (Sirenix)
**Location**: `Assets/Plugins/Sirenix/`

Enhanced Unity Inspector with powerful attributes and custom editors.

**Usage**:
```csharp
using Sirenix.OdinInspector;

public class MyClass : MonoBehaviour {
    [ShowInInspector, ReadOnly]
    private int calculatedValue;

    [Button("Run Action")]
    private void DoSomething() {
        // Action
    }
}
```

---

#### DOTween (Demigiant)
**Location**: `Assets/Plugins/Demigiant/`

Tweening/animation library for smooth interpolations.

**Usage**:
```csharp
using DG.Tweening;

transform.DOMove(targetPosition, 1.0f).SetEase(Ease.InOutQuad);
```

---

## Important Development Notes

### When Modifying Characters

1. **Module changes**: Ensure two-phase initialization is maintained (Initialize → LateInitialize)
2. **FSM changes**: Modify C# classes in `Assets/Scripts/Gameplay/Character/StateMachine/`
   - Actions: Extend `YisoCharacterAction` base class
   - Decisions: Extend `YisoCharacterDecision` base class
3. **Ability changes**: Separate SO settings from C# logic; use factory pattern
   - Create `YisoAbilitySO` subclass for data
   - Create `IYisoCharacterAbility` implementation for logic
4. **State permissions**: Check state constraints before executing abilities
   - Use `IsMovementAllowed`, `IsAttackAllowed` properties
5. **Object pooling**: Properly implement OnEnable/OnDisable for module lifecycle support

---

### When Adding New Features

1. **New abilities**:
   - Create `YisoAbilitySO` subclass in `Assets/Scripts/Gameplay/Character/Abilities/Definitions/`
   - Implement `IYisoCharacterAbility` in `Assets/Scripts/Gameplay/Character/Abilities/`
   - Use factory pattern: `CreateAbility()` method in SO

2. **New FSM Actions**:
   - Extend `YisoCharacterAction` base class
   - Implement `PerformAction(IYisoCharacterContext context)`
   - Place in `Assets/Scripts/Gameplay/Character/StateMachine/Actions/`

3. **New FSM Decisions**:
   - Extend `YisoCharacterDecision` base class
   - Implement `Decide(IYisoCharacterContext context)` returning bool
   - Place in `Assets/Scripts/Gameplay/Character/StateMachine/Decisions/`

4. **Module communication**:
   - Use `IYisoCharacterContext` interface, not direct module references
   - Access modules through context facade

5. **Events**:
   - Use `YisoEventManager` for decoupled communication between systems
   - Define struct-based events for performance

---

### Code Language

Korean comments are prevalent throughout the codebase. When adding comments, follow existing patterns:
- **Korean** for team members and internal logic explanations
- **English** for public APIs and documentation

---

### Physics & Movement

- **TopDownController** (`Assets/Scripts/Gameplay/Core/TopDownController.cs`) handles Rigidbody2D-based movement
- Movement uses interpolation with acceleration/deceleration curves
- Impact/knockback system with falloff curves for physics-based reactions
- Always use `IPhysicsControllable` interface for movement control

---

### Input System

- Unity's **new Input System** is used (not legacy Input Manager)
- **Action maps**: Player and UI
- **Configuration**: `Assets/Settings/InputSystem/InputSystem_Actions.inputactions`
- Runtime switching between action maps is supported
- Input is processed in `YisoCharacterInputModule`

---

## Performance Considerations

### Update Loop Optimization

All game components inherit from `RunIBehaviour` instead of MonoBehaviour directly:
- Centralized update management through `RunIUpdateManager`
- 60-70% reduction in update loop overhead in high entity count scenarios
- Use `OnUpdate()`, `OnFixedUpdate()`, `OnLateUpdate()` instead of Unity's native callbacks

### Event System Performance

Use struct-based events for zero-allocation event triggering:
```csharp
// Good - struct-based event
public struct PlayerDiedEvent {
    public GameObject player;
}

// Bad - class-based event (causes allocations)
public class PlayerDiedEvent {
    public GameObject player;
}
```
