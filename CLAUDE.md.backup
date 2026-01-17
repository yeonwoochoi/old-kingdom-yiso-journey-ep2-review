# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Old Kingdom Yiso Journey Episode 2** is a Unity 2D top-down action RPG built with Unity 6000.0.62f1. The codebase follows a modular architecture with C# class-based FSM (Ver2) and ScriptableObject-driven abilities system. The development team uses Korean comments throughout the codebase.

## Common Commands

### Running Tests
Tests are Unity Editor tests using NUnit framework with Moq for mocking. To run tests:
- Open Unity Editor → Window → General → Test Runner
- Select "EditMode" tab to see all unit tests
- Tests are located in `Assets/Editor/Tests/`

**Note**: Unity Test Framework doesn't support command-line test execution without additional setup. Tests must be run within the Unity Editor.

### Building the Project
- Open Unity Editor and load the project
- File → Build Settings
- Select target platform and click "Build"
- Or use Unity command line: `Unity.exe -quit -batchmode -projectPath "." -executeMethod BuildScript.Build`

### Opening the Project
```bash
# Open with Unity Hub (recommended)
# Unity Version: 6000.0.62f1
```

### Git Workflow
The project uses automated versioning via GitHub Actions:
- Push to `main` branch triggers automatic patch version bump
- Semantic versioning keywords in commits: `major:`, `minor:`, or defaults to `patch`
- GitHub releases are auto-generated with commit-based release notes

## Core Architecture

### Modular Character System

The character system is the heart of the game architecture. **YisoCharacter** acts as a central facade coordinating independent modules through the **IYisoCharacterContext** interface.

**Key Files**:
- `Assets/Scripts/Gameplay/Character/Core/YisoCharacter.cs` - Central character hub
- `Assets/Scripts/Gameplay/Character/Core/Modules/` - All character modules

**Character Modules** (9 total):
1. **CoreModule** - Basic functionality
2. **AbilityModule** - Manages character abilities lifecycle
3. **AnimationModule** - Animation control
4. **StateModule** - FSM state management
5. **InputModule** - Player input (Player type only)
6. **AIModule** - AI pathfinding (AI types only)
7. **LifecycleModule** - Health/death management
8. **SaveModule** - Save/load functionality
9. **WeaponModule** - Weapon creation, equipping, and management

**Module Initialization**: Two-phase process
1. **Initialize()** - Each module sets up independently
2. **LateInitialize()** - Modules link to each other after all are initialized

**Module Lifecycle**: OnEnable → OnUpdate (via RunIUpdateManager) → OnDisable → OnDestroy

**Character Types**: Player, Enemy, NPC, Pet, Ally - differentiated by which modules are active (InputModule vs AIModule)

### C# Class-Based FSM (Ver2)

The FSM system uses C# classes for states, actions, and decisions. Ver1 (ScriptableObject-based FSM) was completely removed in commit `8fa6bcf`.

**Key Files**:
- `Assets/Scripts/Gameplay/Character/StateMachine/YisoCharacterStateMachine.cs` - FSM runtime manager (MonoBehaviour)
- `Assets/Scripts/Gameplay/Character/StateMachine/YisoCharacterState.cs` - Serializable state class
- `Assets/Scripts/Gameplay/Character/StateMachine/YisoCharacterTransition.cs` - Transition logic
- `Assets/Scripts/Gameplay/Character/StateMachine/YisoCharacterAction.cs` - Abstract action base class
- `Assets/Scripts/Gameplay/Character/StateMachine/YisoCharacterDecision.cs` - Decision base class

**FSM Structure**:
- **States**: Serializable classes with OnEnter/OnUpdate/OnExit actions and child states
- **Transitions**: Decision-based routing between states (true/false evaluation)
- **Actions (11 implementations)**:
  - Move: MoveTowardTarget, MoveRandomly, Patrol, StopMovement, ReturnToSpawn
  - Attack: Attack, ChangeWeapon
  - Orientation: FaceTowardTarget, ConeOfVision
  - General: DoNothing, SetAnimator
- **Decisions (6 implementations)**: DetectTargetConeOfVision, DetectTargetInRadius, DistanceToTarget, DistanceToSpawn, TargetIsNull, TimeInState
- **Frequency-based Updates**: Configurable action frequency via `actionFrequency` or `actionFrequencyRange`
- **Target System**: Multi-target support with `maxTargetCount` slots

**Action/Decision Locations**:
- Actions: `Assets/Scripts/Gameplay/Character/StateMachine/Actions/`
- Decisions: `Assets/Scripts/Gameplay/Character/StateMachine/Decisions/`

### Ability System

Abilities follow a **separation of data and logic** pattern:
- **ScriptableObject Definitions** (Data): `YisoAbilitySO` subclasses define settings/configuration
- **Pure C# Classes** (Logic): `IYisoCharacterAbility` implementations contain runtime behavior
- **Factory Pattern**: Each SO creates its corresponding ability instance via `CreateAbility()`

**Ability Lifecycle**: `PreProcessAbility()` → `ProcessAbility()` → `PostProcessAbility()` → `UpdateAnimator()`

**Key Files**:
- `Assets/Scripts/Gameplay/Character/Abilities/` - Ability implementations
- `Assets/Scripts/Gameplay/Character/Abilities/Definitions/` - SO definitions

**Example Abilities**:
- **YisoMovementAbility**: Interpolated movement with acceleration/deceleration, speed multipliers
- **YisoOrientationAbility**: Character facing direction control (always-enabled ability)
- **YisoMeleeAttackAbility**: Melee combat with input buffering and animation events

### Update Loop Optimization

The project uses a custom update manager to reduce MonoBehaviour update overhead.

**Key File**: `Assets/Scripts/Core/Manager/RunIUpdateManager.cs`

**Pattern**: All game components inherit from `RunIBehaviour` (not MonoBehaviour directly) and register with `RunIUpdateManager` for centralized update management (OnUpdate, OnFixedUpdate, OnLateUpdate).

### Event System

Type-safe event system using generics and struct-based events for performance.

**Key File**: `Assets/Scripts/Gameplay/Tools/Event/YisoEventManager.cs`

**Usage**: Extension methods provide easy subscribe/unsubscribe pattern for decoupled communication.

## Important Patterns & Conventions

### Design Patterns Used
- **Module Pattern**: Character decomposed into independent, reusable modules
- **Facade Pattern**: YisoCharacter provides simple API hiding module complexity
- **Factory Pattern**: AbilitySO creates ability instances
- **Observer Pattern**: Event system for decoupled communication
- **Strategy Pattern**: Abilities as interchangeable behaviors
- **State Pattern**: FSM with C# classes (Ver2)

### Naming Conventions
- **"Yiso" prefix**: All game-specific classes (e.g., `YisoCharacter`, `YisoMovementAbility`)
- **"SO" suffix**: ScriptableObjects for Abilities and Data (e.g., `YisoAbilitySO`, `YisoWeaponDataSO`)
  - **Note**: FSM no longer uses ScriptableObjects (Ver2 uses C# classes)
- **Module naming**: `YisoCharacter[Function]Module` (e.g., `YisoCharacterInputModule`, `YisoCharacterWeaponModule`)

### Interface-First Design
Key interfaces define contracts between systems:
- `IYisoCharacterContext` - Character hub interface for modules
- `IYisoCharacterModule` - Base interface for all character modules
- `IYisoCharacterAbility` - Base interface for all abilities
- `IPhysicsControllable` - Physics/movement interface

### Code Language
Korean comments are prevalent throughout the codebase. When adding comments, follow existing patterns (Korean for team members, English for public APIs/documentation).

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── Behaviour/              # RunIBehaviour base class
│   │   └── Manager/                # RunIUpdateManager (centralized updates)
│   ├── Gameplay/
│   │   ├── Character/              # ** MAIN CHARACTER SYSTEM **
│   │   │   ├── Core/              # YisoCharacter + 9 Modules
│   │   │   ├── Abilities/         # Ability implementations (C# classes)
│   │   │   ├── StateMachine/      # FSM Actions, Decisions, States (C# classes)
│   │   │   ├── Weapon/            # Weapon system (instance, aim, damage)
│   │   │   ├── Types/             # Enums and constants
│   │   │   └── Data/              # Character data definitions
│   │   ├── Core/                  # TopDownController (physics)
│   │   ├── Health/                # Health/damage system
│   │   └── Tools/                 # Utility systems
│   │       ├── Event/             # YisoEventManager
│   │       ├── Movement/          # Movement utilities
│   │       ├── StateMachine/      # Generic FSM base classes
│   │       ├── Visual/            # Field of View renderer
│   │       └── ...
│   ├── Managers/                  # Game-level managers
│   ├── Settings/                  # Game settings
│   ├── UI/                        # UI systems
│   └── Utils/                     # Math, Color utilities
├── Data/ScriptableObjects/        # ** DATA ASSETS **
│   ├── Ability/                   # Ability SO instances
│   └── Weapon/                    # Weapon data SO instances
├── Editor/Tests/                  # ** UNIT TESTS **
│   └── Utils/                     # TestUtils (reflection helpers)
└── Plugins/
    ├── Sirenix/                   # Odin Inspector
    └── Demigiant/                 # DOTween
```

## Testing Infrastructure

### Test Framework
- **Location**: `Assets/Editor/Tests/`
- **Framework**: NUnit with Moq for mocking
- **Current Status**: Test files were deleted during Ver2 FSM migration (commit `2c61734`)
- **Utilities**: `TestUtils.cs` provides reflection helpers for future test development

### Test Utilities
**TestUtils.cs** (`Assets/Editor/Tests/Utils/TestUtils.cs`):
- **SetPrivateField()** / **GetPrivateField<T>()**: Reflection-based field access
- **SetPrivateProperty()** / **GetPrivateProperty<T>()**: Reflection-based property access
- Type-safe error reporting with available field/property names

**Safe Reflection Pattern Example:**
```csharp
// Type-safe field reading
var wasPressed = TestUtils.GetPrivateField<bool>(_ability, "_wasAttackPressedLastFrame");
Assert.IsFalse(wasPressed);

// Setting private fields
TestUtils.SetPrivateField(_abilitySO, "priority", 100);
```

### Testing Guidelines for Future Development

#### What Can Be Mocked
- **Interfaces**: `IYisoCharacterContext`, `IYisoCharacterModule` ✅
- **ScriptableObjects**: ❌ NEVER (use CreateInstance instead)
- **Sealed Classes** (all Modules): ❌ NEVER
  - All `YisoCharacter*Module` classes are sealed
  - Focus on Ability logic tests or integration tests

#### ScriptableObject Testing Pattern
```csharp
// Real instance creation (DO NOT mock)
var abilitySO = ScriptableObject.CreateInstance<YisoAbilitySO>();
TestUtils.SetPrivateField(abilitySO, "priority", 100);

// Memory cleanup in TearDown
Object.DestroyImmediate(abilitySO);
```

## Key Dependencies

### NuGet Packages (packages.config)
- **Moq 4.20.72** - Mocking framework for unit tests
- **R3 1.3.0** - Reactive Extensions for Unity
- **Castle.Core 5.1.1** - Moq dependency

### Unity Packages (critical)
- **com.cysharp.r3** - Reactive Extensions (git package)
- **com.cysharp.unitask** - async/await for Unity (git package)
- **com.github-glitchenzo.nugetforunity** - NuGet package manager (git package)
- **com.unity.inputsystem** - New Input System
- **com.unity.addressables** - Asset management
- **com.unity.localization** - Localization support
- **com.unity.test-framework** - Unit testing framework
- **com.unity.render-pipelines.universal** - URP rendering

### Third-Party Assets
- **Odin Inspector (Sirenix)** - Enhanced Unity Inspector with attributes
- **DOTween (Demigiant)** - Tweening/animation library

## System Interactions

### Character Update Flow
```
YisoCharacter (MonoBehaviour)
  ↓ registers with
RunIUpdateManager
  ↓ calls OnUpdate()
YisoCharacter.OnUpdate()
  ↓ delegates to
Modules (StateModule, InputModule/AIModule, AbilityModule, AnimationModule)
  ↓
StateModule evaluates FSM transitions
AbilityModule processes abilities (Pre → Process → Post → UpdateAnimator)
AnimationModule updates Animator parameters
```

### FSM Evaluation Flow
```
StateModule.OnUpdate()
  ↓
Evaluate Transitions (in order)
  ↓
Execute Decision.Decide()
  ↓ returns true/false
Transition to corresponding state
  ↓
Current State OnExit() → New State OnEnter()
  ↓
New State OnUpdate() → Execute Actions
```

### Ability Processing Flow
```
AbilityModule.ProcessAbilities()
  ↓ for each ability
PreProcessAbility()    // Setup, read input
  ↓
ProcessAbility()       // Core logic (e.g., movement calculation)
  ↓
PostProcessAbility()   // Apply results, trigger events
  ↓
UpdateAnimator()       // Sync animation parameters
```

## Important Notes

### When Modifying Characters
1. **Module changes**: Ensure two-phase initialization is maintained (Initialize → LateInitialize)
2. **FSM changes**: Modify C# classes in `Assets/Scripts/Gameplay/Character/StateMachine/`
3. **Ability changes**: Separate SO settings from C# logic; use factory pattern
4. **State permissions**: Check state constraints before executing abilities
5. **Object pooling**: Properly implement OnEnable/OnDisable for module lifecycle support

### When Adding New Features
1. **New abilities**: Create `YisoAbilitySO` subclass + `IYisoCharacterAbility` implementation
2. **New FSM Actions**: Extend `YisoCharacterAction` base class, implement `PerformAction()`
3. **New FSM Decisions**: Extend `YisoCharacterDecision` base class, implement `Decide()`
4. **Module communication**: Use `IYisoCharacterContext` interface, not direct module references
5. **Events**: Use `YisoEventManager` for decoupled communication between systems

### Physics & Movement
- **TopDownController** (`Assets/Scripts/Gameplay/Core/TopDownController.cs`) handles Rigidbody2D-based movement
- Movement uses interpolation with acceleration/deceleration curves
- Impact/knockback system with falloff curves for physics-based reactions

### Input System
- Unity's new Input System is used (not legacy)
- Action maps: **Player** and **UI**
- Configuration: `Assets/Settings/InputSystem/InputSystem_Actions.inputactions`
- Runtime switching between action maps is supported

## New Systems (Recently Added)

### Weapon System
**Location**: `Assets/Scripts/Gameplay/Character/Weapon/`

The weapon system provides melee/ranged combat functionality integrated with the character module architecture.

**Key Components**:
- **YisoCharacterWeaponModule** (Module #9): Manages weapon lifecycle (creation, equipping, destruction)
- **YisoWeaponInstance**: Runtime weapon instance with active/inactive states
- **YisoWeaponAim**: Aiming logic for directional attacks, integrates with YisoOrientationAbility
- **YisoDamageOnTouch**: Collision-based damage detection and application
- **YisoWeaponDataSO**: ScriptableObject defining weapon stats and behavior

**Integration**:
- WeaponModule initialized in YisoCharacter (line 172)
- YisoOrientationAbility reads from WeaponAim for aim-based direction control
- YisoMeleeAttackAbility triggers weapon activation via animation events

### Field of View (FOV) System
**Commits**: `4e569af` (FOV feature), `273592e` (Cone of Vision decision)

**Location**: `Assets/Scripts/Gameplay/Tools/Visual/YisoFieldOfViewRenderer.cs`

Mesh-based field of view visualization with raycasting for AI enemy detection.

**Key Components**:
- **YisoFieldOfViewRenderer**: Renders FOV mesh using configurable angle and range
- **YisoCharacterActionConeOfVision**: FSM Action that updates FOV direction
- **YisoCharacterDecisionDetectTargetConeOfVision**: FSM Decision detecting enemies within FOV cone

**Usage in FSM**:
- AI states use ConeOfVision action to visualize detection range
- DetectTargetConeOfVision decision triggers state transitions (e.g., Idle → Chase)

### IYisoCharacterContext API

The `IYisoCharacterContext` interface provides the core API for FSM Actions and Abilities to interact with characters.

**Movement & Direction Control**:
```csharp
void Move(Vector2 finalMovementVector);  // Set character movement
void Face(FacingDirections direction);   // Face a cardinal direction
void Face(Vector2 directionVector);      // Face a direction vector (auto-converts to cardinal)
```

**State & Animation**:
```csharp
void PlayAnimation(YisoCharacterAnimationState state, bool/float/int value);
void OnAnimationEvent(string eventName);  // Routes to AbilityModule
```

**Health & Lifecycle**:
```csharp
float GetCurrentHealth();
bool IsDead();
void TakeDamage(DamageInfo damage);
```

**Permissions**:
```csharp
bool IsMovementAllowed { get; }  // Checks death, ability blocks
bool IsAttackAllowed { get; }    // Checks death, ability blocks
```

**Properties**:
```csharp
Vector2 MovementVector { get; }           // Input/AI direction
FacingDirections FacingDirection { get; } // Current facing (from OrientationAbility)
Vector2 FacingDirectionVector { get; }    // Current facing as Vector2
```

**Implementation**: `Assets/Scripts/Gameplay/Character/Core/YisoCharacter.cs` (lines 216-235)

## Recent Changes

### Ver2 FSM Migration (Commit `8fa6bcf`)
- **Removed**: Ver1 FSM (ScriptableObject-based), BlackboardModule, all Ver1 SO files
- **Added**: Ver2 FSM (C# class-based) with Serializable states and improved performance
- **Breaking Change**: All FSM SO assets became obsolete; states now defined in Inspector

### Face API Addition (Latest)
- **Added**: `Face(FacingDirections)` and `Face(Vector2)` methods to IYisoCharacterContext
- **Purpose**: Enable FSM Actions to control character direction via Facade pattern
- **Implementation**: Delegates to YisoOrientationAbility.ForceFace()

### WeaponModule Integration
- **Added**: 9th character module for weapon management
- **Integration**: YisoOrientationAbility reads WeaponAim for aim-based direction priority
- **Pattern**: Ability → WeaponModule.CurrentWeapon.WeaponAim → Orientation updates
