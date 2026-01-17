# API Reference

This document provides API reference for developing FSM Actions, Decisions, and Abilities.

## Table of Contents
- [IYisoCharacterContext API](#iyisocharactercontext-api)
- [Core Interfaces](#core-interfaces)
- [Module APIs](#module-apis)

---

## IYisoCharacterContext API

The `IYisoCharacterContext` interface provides the core API for FSM Actions and Abilities to interact with characters.

**Implementation**: `Assets/Scripts/Gameplay/Character/Core/YisoCharacter.cs` (lines 216-235)

### Movement & Direction Control

```csharp
void Move(Vector2 finalMovementVector);
```
Set character movement vector. This will be processed by the TopDownController.

**Parameters**:
- `finalMovementVector` - Normalized direction vector multiplied by desired speed

**Usage**:
```csharp
// In FSM Action or Ability
context.Move(direction.normalized * speed);
```

---

```csharp
void Face(FacingDirections direction);
```
Face a cardinal direction (North, South, East, West, NorthEast, NorthWest, SouthEast, SouthWest).

**Parameters**:
- `direction` - Target cardinal direction enum

**Usage**:
```csharp
// Face east
context.Face(FacingDirections.East);
```

---

```csharp
void Face(Vector2 directionVector);
```
Face a direction vector (auto-converts to nearest cardinal direction).

**Parameters**:
- `directionVector` - Target direction as Vector2

**Usage**:
```csharp
// Face toward target
Vector2 direction = (target.position - transform.position).normalized;
context.Face(direction);
```

---

### State & Animation

```csharp
void PlayAnimation(YisoCharacterAnimationState state, bool value);
void PlayAnimation(YisoCharacterAnimationState state, float value);
void PlayAnimation(YisoCharacterAnimationState state, int value);
```
Update animator parameters.

**Parameters**:
- `state` - Animation state enum (e.g., YisoCharacterAnimationState.Idle, Attack, Walking)
- `value` - Parameter value (bool, float, or int depending on animator parameter type)

**Usage**:
```csharp
// Set walking animation
context.PlayAnimation(YisoCharacterAnimationState.Walking, true);

// Set speed parameter
context.PlayAnimation(YisoCharacterAnimationState.Speed, 5.0f);
```

---

```csharp
void OnAnimationEvent(string eventName);
```
Routes animation events to the AbilityModule. Used by Unity Animation Events.

**Parameters**:
- `eventName` - Event identifier string

**Usage**:
```csharp
// Called from Unity Animation Event
context.OnAnimationEvent("AttackHit");
```

---

### Health & Lifecycle

```csharp
float GetCurrentHealth();
```
Get character's current health value.

**Returns**: Current health as float

---

```csharp
bool IsDead();
```
Check if character is dead.

**Returns**: True if dead, false otherwise

---

```csharp
void TakeDamage(DamageInfo damage);
```
Apply damage to character. Handles health reduction, death check, and damage events.

**Parameters**:
- `damage` - DamageInfo struct containing damage amount, type, source, etc.

**Usage**:
```csharp
var damageInfo = new DamageInfo {
    damage = 50f,
    damageType = DamageType.Physical,
    source = attackerGameObject
};
context.TakeDamage(damageInfo);
```

---

### Permissions

```csharp
bool IsMovementAllowed { get; }
```
Check if movement is currently allowed. Checks death state and ability blocks.

**Returns**: True if movement is allowed, false otherwise

**Usage**:
```csharp
if (context.IsMovementAllowed) {
    context.Move(movementVector);
}
```

---

```csharp
bool IsAttackAllowed { get; }
```
Check if attack is currently allowed. Checks death state and ability blocks.

**Returns**: True if attack is allowed, false otherwise

**Usage**:
```csharp
if (context.IsAttackAllowed) {
    // Perform attack logic
}
```

---

### Properties

```csharp
Vector2 MovementVector { get; }
```
Get current input/AI movement direction (normalized).

**Returns**: Movement vector as Vector2

---

```csharp
FacingDirections FacingDirection { get; }
```
Get current facing direction (from OrientationAbility).

**Returns**: Current facing direction enum

---

```csharp
Vector2 FacingDirectionVector { get; }
```
Get current facing direction as Vector2.

**Returns**: Facing direction as normalized Vector2

---

## Core Interfaces

### IYisoCharacterModule

Base interface for all character modules.

**Location**: `Assets/Scripts/Gameplay/Character/Core/IYisoCharacterModule.cs`

```csharp
public interface IYisoCharacterModule {
    void Initialize(IYisoCharacterContext context);
    void LateInitialize();
    void OnEnable();
    void OnUpdate();
    void OnDisable();
    void OnDestroy();
}
```

**Key Methods**:
- `Initialize()` - First initialization phase, receives character context
- `LateInitialize()` - Second initialization phase, after all modules are initialized
- `OnUpdate()` - Called every frame by RunIUpdateManager

---

### IYisoCharacterAbility

Base interface for all abilities.

**Location**: `Assets/Scripts/Gameplay/Character/Abilities/IYisoCharacterAbility.cs`

```csharp
public interface IYisoCharacterAbility {
    void PreProcessAbility();
    void ProcessAbility();
    void PostProcessAbility();
    void UpdateAnimator();
    void ResetAbility();
}
```

**Key Methods**:
- `PreProcessAbility()` - Setup, read input, check permissions
- `ProcessAbility()` - Core ability logic
- `PostProcessAbility()` - Apply results, trigger events
- `UpdateAnimator()` - Sync animation parameters

---

### IPhysicsControllable

Interface for physics/movement control.

**Location**: `Assets/Scripts/Gameplay/Core/IPhysicsControllable.cs`

```csharp
public interface IPhysicsControllable {
    void SetMovement(Vector2 movement);
    void AddForce(Vector2 force);
    Vector2 GetVelocity();
}
```

Used by TopDownController for movement control.

---

## Module APIs

### StateModule API

**Location**: `Assets/Scripts/Gameplay/Character/Core/Modules/YisoCharacterStateModule.cs`

```csharp
// Change FSM state
void ChangeState(string stateName);

// Get current state
YisoCharacterState GetCurrentState();

// Get target
GameObject GetTarget();

// Set target
void SetTarget(GameObject target);
```

---

### AbilityModule API

**Location**: `Assets/Scripts/Gameplay/Character/Core/Modules/YisoCharacterAbilityModule.cs`

```csharp
// Register ability
void RegisterAbility(IYisoCharacterAbility ability);

// Unregister ability
void UnregisterAbility(IYisoCharacterAbility ability);

// Get ability by type
T GetAbility<T>() where T : IYisoCharacterAbility;

// Block/unblock abilities
void BlockAbilities();
void UnblockAbilities();
```

---

### WeaponModule API

**Location**: `Assets/Scripts/Gameplay/Character/Core/Modules/YisoCharacterWeaponModule.cs`

```csharp
// Get current weapon
YisoWeaponInstance CurrentWeapon { get; }

// Equip weapon
void EquipWeapon(YisoWeaponDataSO weaponData);

// Unequip weapon
void UnequipWeapon();

// Activate weapon (start attack)
void ActivateWeapon();

// Deactivate weapon (end attack)
void DeactivateWeapon();
```

---

## Example Usage

### Creating a Custom FSM Action

```csharp
using UnityEngine;
using Character.StateMachine;

public class MyCustomAction : YisoCharacterAction {
    public float moveSpeed = 5f;

    public override void PerformAction(IYisoCharacterContext context) {
        if (!context.IsMovementAllowed) return;

        // Get target from StateModule
        var target = context.GetTarget();
        if (target == null) return;

        // Calculate direction
        Vector2 direction = (target.transform.position - context.transform.position).normalized;

        // Move toward target
        context.Move(direction * moveSpeed);

        // Face target
        context.Face(direction);
    }
}
```

---

### Creating a Custom FSM Decision

```csharp
using UnityEngine;
using Character.StateMachine;

public class MyCustomDecision : YisoCharacterDecision {
    public float detectionRange = 10f;

    public override bool Decide(IYisoCharacterContext context) {
        var target = context.GetTarget();
        if (target == null) return false;

        float distance = Vector2.Distance(
            context.transform.position,
            target.transform.position
        );

        return distance <= detectionRange;
    }
}
```

---

### Creating a Custom Ability

```csharp
using UnityEngine;
using Character.Abilities;

public class MyCustomAbility : IYisoCharacterAbility {
    private IYisoCharacterContext context;

    public void Initialize(IYisoCharacterContext ctx) {
        context = ctx;
    }

    public void PreProcessAbility() {
        // Read input, check permissions
    }

    public void ProcessAbility() {
        // Core ability logic
        if (context.IsMovementAllowed) {
            // Do something
        }
    }

    public void PostProcessAbility() {
        // Apply results, trigger events
    }

    public void UpdateAnimator() {
        // Sync animation parameters
        context.PlayAnimation(YisoCharacterAnimationState.Custom, true);
    }

    public void ResetAbility() {
        // Reset state
    }
}
```
