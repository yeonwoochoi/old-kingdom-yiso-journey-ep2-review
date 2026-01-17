# Architecture Documentation

This document describes the core architecture and design patterns used in Old Kingdom Yiso Journey Episode 2.

## Table of Contents
- [Modular Character System](#modular-character-system)
- [C# Class-Based FSM (Ver2)](#c-class-based-fsm-ver2)
- [Ability System](#ability-system)
- [Core Infrastructure](#core-infrastructure)
- [Design Patterns](#design-patterns)
- [Naming Conventions](#naming-conventions)
- [Interface-First Design](#interface-first-design)
- [System Interaction Flows](#system-interaction-flows)
- [Project Structure](#project-structure)

---

## Modular Character System

The character system is the heart of the game architecture. **YisoCharacter** acts as a central facade coordinating independent modules through the **IYisoCharacterContext** interface.

### Key Files
- `Assets/Scripts/Gameplay/Character/Core/YisoCharacter.cs` - Central character hub
- `Assets/Scripts/Gameplay/Character/Core/Modules/` - All character modules

### Character Modules (9 total)

1. **CoreModule** - Basic functionality
2. **AbilityModule** - Manages character abilities lifecycle
3. **AnimationModule** - Animation control
4. **StateModule** - FSM state management
5. **InputModule** - Player input (Player type only)
6. **AIModule** - AI pathfinding (AI types only)
7. **LifecycleModule** - Health/death management
8. **SaveModule** - Save/load functionality
9. **WeaponModule** - Weapon creation, equipping, and management

### Module Initialization (Two-Phase Process)

1. **Initialize()** - Each module sets up independently
2. **LateInitialize()** - Modules link to each other after all are initialized

This ensures dependencies are resolved in the correct order.

### Module Lifecycle

```
OnEnable → OnUpdate (via RunIUpdateManager) → OnDisable → OnDestroy
```

All modules follow this lifecycle pattern for consistent behavior with object pooling.

### Character Types

Characters are differentiated by which modules are active:
- **Player**: CoreModule + AbilityModule + AnimationModule + StateModule + InputModule + LifecycleModule + WeaponModule
- **Enemy**: CoreModule + AbilityModule + AnimationModule + StateModule + AIModule + LifecycleModule + WeaponModule
- **NPC**: CoreModule + AnimationModule (minimal setup)
- **Pet/Ally**: Similar to Enemy but with friendly targeting

---

## C# Class-Based FSM (Ver2)

The FSM system uses C# classes for states, actions, and decisions. Ver1 (ScriptableObject-based FSM) was completely removed in commit `8fa6bcf`.

### Key Files
- `Assets/Scripts/Gameplay/Character/StateMachine/YisoCharacterStateMachine.cs` - FSM runtime manager (MonoBehaviour)
- `Assets/Scripts/Gameplay/Character/StateMachine/YisoCharacterState.cs` - Serializable state class
- `Assets/Scripts/Gameplay/Character/StateMachine/YisoCharacterTransition.cs` - Transition logic
- `Assets/Scripts/Gameplay/Character/StateMachine/YisoCharacterAction.cs` - Abstract action base class
- `Assets/Scripts/Gameplay/Character/StateMachine/YisoCharacterDecision.cs` - Decision base class

### FSM Structure

#### States
Serializable classes with OnEnter/OnUpdate/OnExit actions and child states. States can be nested to create hierarchical state machines.

#### Transitions
Decision-based routing between states (true/false evaluation). Transitions are evaluated in order, and the first matching decision triggers the state change.

#### Actions (11 implementations)
- **Move**: MoveTowardTarget, MoveRandomly, Patrol, StopMovement, ReturnToSpawn
- **Attack**: Attack, ChangeWeapon
- **Orientation**: FaceTowardTarget, ConeOfVision
- **General**: DoNothing, SetAnimator

#### Decisions (6 implementations)
- DetectTargetConeOfVision
- DetectTargetInRadius
- DistanceToTarget
- DistanceToSpawn
- TargetIsNull
- TimeInState

### Action/Decision Locations
- Actions: `Assets/Scripts/Gameplay/Character/StateMachine/Actions/`
- Decisions: `Assets/Scripts/Gameplay/Character/StateMachine/Decisions/`

### Frequency-Based Updates

Actions can have configurable update frequency via:
- `actionFrequency` - Fixed update interval
- `actionFrequencyRange` - Random update interval between min/max

This reduces performance overhead for expensive operations.

### Target System

Multi-target support with `maxTargetCount` slots. FSM Actions and Decisions can query and manipulate multiple targets simultaneously.

---

## Ability System

Abilities follow a **separation of data and logic** pattern for flexibility and testability.

### Architecture

- **ScriptableObject Definitions (Data)**: `YisoAbilitySO` subclasses define settings/configuration
- **Pure C# Classes (Logic)**: `IYisoCharacterAbility` implementations contain runtime behavior
- **Factory Pattern**: Each SO creates its corresponding ability instance via `CreateAbility()`

### Ability Lifecycle

```
PreProcessAbility() → ProcessAbility() → PostProcessAbility() → UpdateAnimator()
```

1. **PreProcessAbility()** - Setup, read input, check permissions
2. **ProcessAbility()** - Core logic (e.g., movement calculation, attack detection)
3. **PostProcessAbility()** - Apply results, trigger events, update state
4. **UpdateAnimator()** - Sync animation parameters with Animator

### Key Files
- `Assets/Scripts/Gameplay/Character/Abilities/` - Ability implementations
- `Assets/Scripts/Gameplay/Character/Abilities/Definitions/` - SO definitions

### Example Abilities

- **YisoMovementAbility**: Interpolated movement with acceleration/deceleration, speed multipliers
- **YisoOrientationAbility**: Character facing direction control (always-enabled ability)
- **YisoMeleeAttackAbility**: Melee combat with input buffering and animation events

---

## Core Infrastructure

### Update Loop Optimization

The project uses a custom update manager to reduce MonoBehaviour update overhead.

**Key File**: `Assets/Scripts/Core/Manager/RunIUpdateManager.cs`

**Pattern**: All game components inherit from `RunIBehaviour` (not MonoBehaviour directly) and register with `RunIUpdateManager` for centralized update management:
- OnUpdate (Update)
- OnFixedUpdate (FixedUpdate)
- OnLateUpdate (LateUpdate)

This reduces Unity's native update loop overhead by 60-70% in high entity count scenarios.

### Event System

Type-safe event system using generics and struct-based events for performance.

**Key File**: `Assets/Scripts/Gameplay/Tools/Event/YisoEventManager.cs`

**Usage**: Extension methods provide easy subscribe/unsubscribe pattern for decoupled communication:
```csharp
this.YisoEventStartListening<PlayerDiedEvent>(OnPlayerDied);
this.YisoEventStopListening<PlayerDiedEvent>(OnPlayerDied);
YisoEventManager.TriggerEvent(new PlayerDiedEvent { player = this });
```

### Physics & Movement

**TopDownController** (`Assets/Scripts/Gameplay/Core/TopDownController.cs`) handles Rigidbody2D-based movement:
- Interpolated movement with acceleration/deceleration curves
- Impact/knockback system with falloff curves for physics-based reactions
- Collision detection and response

---

## Design Patterns

### Module Pattern
Character decomposed into independent, reusable modules. Each module has a single responsibility and can be composed as needed.

### Facade Pattern
YisoCharacter provides simple API hiding module complexity. External systems only interact with the facade, not individual modules.

### Factory Pattern
AbilitySO creates ability instances. This separates configuration (SO) from runtime behavior (C# class).

### Observer Pattern
Event system for decoupled communication. Systems can react to events without direct references.

### Strategy Pattern
Abilities as interchangeable behaviors. Character behavior changes by swapping ability configurations.

### State Pattern
FSM with C# classes (Ver2). States encapsulate behavior and transitions.

---

## Naming Conventions

### "Yiso" Prefix
All game-specific classes use the "Yiso" prefix:
- `YisoCharacter`
- `YisoMovementAbility`
- `YisoCharacterStateMachine`

This distinguishes game code from Unity/framework code and avoids naming collisions.

### "SO" Suffix
ScriptableObjects for data definitions use the "SO" suffix:
- `YisoAbilitySO`
- `YisoWeaponDataSO`

**Note**: FSM no longer uses ScriptableObjects (Ver2 uses C# classes).

### Module Naming
Character modules follow the pattern: `YisoCharacter[Function]Module`
- `YisoCharacterInputModule`
- `YisoCharacterWeaponModule`
- `YisoCharacterAbilityModule`

---

## Interface-First Design

Key interfaces define contracts between systems. This enables loose coupling and testability.

### Core Interfaces

- **IYisoCharacterContext** - Character hub interface for modules
  - Provides facade API for FSM Actions and Abilities
  - See [API.md](API.md) for full documentation

- **IYisoCharacterModule** - Base interface for all character modules
  - Defines lifecycle methods (Initialize, LateInitialize, OnEnable, OnUpdate, OnDisable, OnDestroy)

- **IYisoCharacterAbility** - Base interface for all abilities
  - Defines ability lifecycle (PreProcess, Process, PostProcess, UpdateAnimator)

- **IPhysicsControllable** - Physics/movement interface
  - Used by TopDownController for movement control

---

## System Interaction Flows

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

---

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

---

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

---

## Code Language

Korean comments are prevalent throughout the codebase. When adding comments, follow existing patterns:
- **Korean** for team members and internal logic explanations
- **English** for public APIs and documentation
