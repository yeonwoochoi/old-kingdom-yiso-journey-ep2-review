# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Old Kingdom Yiso Journey Episode 2** is a Unity 2D top-down action RPG built with Unity 6000.0.62f1. The codebase follows a modular architecture with ScriptableObject-driven FSM and component-based abilities system. The development team uses Korean comments throughout the codebase.

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
4. **BlackboardModule** - FSM data storage (Blackboard pattern)
5. **StateModule** - FSM state management
6. **InputModule** - Player input (Player type only)
7. **AIModule** - AI pathfinding (AI types only)
8. **LifecycleModule** - Health/death management
9. **SaveModule** - Save/load functionality

**Module Initialization**: Two-phase process
1. **Initialize()** - Each module sets up independently
2. **LateInitialize()** - Modules link to each other after all are initialized

**Module Lifecycle**: OnEnable → OnUpdate (via RunIUpdateManager) → OnDisable → OnDestroy

**Character Types**: Player, Enemy, NPC, Pet, Ally - differentiated by which modules are active (InputModule vs AIModule)

### ScriptableObject-Based FSM

The FSM system separates data (ScriptableObjects) from runtime logic (C# classes), enabling designer-friendly workflows without code changes.

**Key Files**:
- `Assets/Scripts/Gameplay/Character/StateMachine/YisoCharacterStateMachineSO.cs`
- `Assets/Scripts/Gameplay/Character/StateMachine/YisoCharacterStateSO.cs`
- `Assets/Scripts/Gameplay/Character/StateMachine/YisoCharacterTransitionSO.cs`

**FSM Structure**:
- **States**: Define OnEnter/OnUpdate/OnExit actions, child states, and permissions (canMove, canCastAbility)
- **Transitions**: Decision-based routing to different states based on true/false results
- **Actions**: Executable behaviors (Movement, Attack, Patrol, Chase, etc.)
- **Decisions**: Boolean conditions (Distance checks, Target detection, Timers, etc.)
- **State Roles**: Idle, Move, Chase, Attack, SkillAttack, Hit, Died, Spawn, Custom

**Example Action/Decision Locations**:
- Actions: `Assets/Scripts/Gameplay/Character/StateMachine/Actions/`
- Decisions: `Assets/Scripts/Gameplay/Character/StateMachine/Decisions/`

### Blackboard Pattern for Data Isolation

The **YisoCharacterBlackboardModule** provides type-safe, centralized data storage for FSM components, preventing direct coupling between states.

**Key File**: `Assets/Scripts/Gameplay/Character/Core/Modules/YisoCharacterBlackboardModule.cs`

**Supported Types**: float, int, string, bool, Vector3, Object

**Usage**: ScriptableObject-based keys (`YisoBlackboardKeySO`) identify data, enabling reusable FSM logic across different characters.

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
- **YisoOrientationAbility**: Character facing direction control

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
- **Blackboard Pattern**: Centralized FSM data storage
- **Observer Pattern**: Event system for decoupled communication
- **Strategy Pattern**: Abilities as interchangeable behaviors
- **State Pattern**: FSM with ScriptableObjects

### Naming Conventions
- **"Yiso" prefix**: All game-specific classes (e.g., `YisoCharacter`, `YisoMovementAbility`)
- **"SO" suffix**: All ScriptableObjects (e.g., `YisoCharacterStateSO`, `YisoAbilitySO`)
- **Module naming**: `YisoCharacter[Function]Module` (e.g., `YisoCharacterInputModule`)

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
│   │   │   ├── StateMachine/      # FSM Actions, Decisions, States (SOs)
│   │   │   ├── Types/             # Enums and constants
│   │   │   └── Data/              # Blackboard keys
│   │   ├── Core/                  # TopDownController (physics)
│   │   ├── Health/                # Health/damage system
│   │   └── Tools/                 # Utility systems
│   │       ├── Event/             # YisoEventManager
│   │       ├── Movement/          # Movement utilities
│   │       ├── StateMachine/      # Generic FSM base classes
│   │       └── ...
│   ├── Managers/                  # Game-level managers
│   ├── Settings/                  # Game settings
│   ├── UI/                        # UI systems
│   └── Utils/                     # Math, Color utilities
├── Data/ScriptableObjects/        # ** DATA ASSETS **
│   ├── Ability/                   # Ability SO instances
│   └── AI/                        # FSM SO instances
│       ├── Action/
│       ├── Decision/
│       ├── State/
│       └── State Machine/
├── Editor/Tests/                  # ** UNIT TESTS **
│   └── Gameplay/Character/
│       ├── Abilities/             # Ability tests (Moq-based)
│       └── StateMachine/          # FSM tests
└── Plugins/
    ├── Sirenix/                   # Odin Inspector
    └── Demigiant/                 # DOTween
```

## Testing Infrastructure

### Test Framework
- **Location**: `Assets/Editor/Tests/`
- **Framework**: NUnit with Moq for mocking
- **Pattern**: Mock `IYisoCharacterContext` to test modules/abilities in isolation
- **Utilities**: `TestUtils.cs` provides reflection helpers for setting private ScriptableObject fields

### Test Files (5 total)
1. `YisoMovementAbilityTests.cs` - Movement ability unit tests
2. `YisoFSMActionTests.cs` - FSM action behavior tests
3. `YisoFSMDecisionTests.cs` - FSM decision logic tests
4. `YisoFSMTransitionTests.cs` - FSM transition tests
5. `TestUtils.cs` - Reflection-based test utilities

### Testing Approach
- **Arrange-Act-Assert** pattern consistently used
- **Mock-based isolation**: Use Moq to verify module interactions without dependencies
- **Reflection utilities**: `TestUtils.SetPrivateField()` sets private ScriptableObject fields for test setup
- **Behavior verification**: Use `Verify()` to ensure correct method calls

**Example**: `Assets/Editor/Tests/Gameplay/Character/Abilities/YisoMovementAbilityTests.cs`

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
2. **FSM changes**: Modify ScriptableObject assets, not runtime code
3. **Ability changes**: Separate SO settings from C# logic; use factory pattern
4. **State permissions**: Check `canMove`, `canCastAbility` in states before executing abilities
5. **Object pooling**: Properly implement OnEnable/OnDisable for module lifecycle support

### When Adding New Features
1. **New abilities**: Create `YisoAbilitySO` subclass + `IYisoCharacterAbility` implementation
2. **New FSM logic**: Create ScriptableObject-based Action/Decision, not hardcoded logic
3. **Module communication**: Use `IYisoCharacterContext` interface, not direct module references
4. **Shared data**: Use Blackboard pattern for FSM-accessible data
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
