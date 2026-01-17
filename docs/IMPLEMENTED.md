# Implemented Systems

This document describes currently implemented game systems in the prototype. This is a snapshot of working features.

## Table of Contents
- [Weapon System](#weapon-system)
- [Combat System](#combat-system)
- [Field of View System](#field-of-view-system)
- [Movement System](#movement-system)
- [Input System](#input-system)
- [Animation System](#animation-system)

---

## Weapon System

**Location**: `Assets/Scripts/Gameplay/Character/Weapon/`

The weapon system provides melee/ranged combat functionality integrated with the character module architecture.

### Key Components

#### YisoCharacterWeaponModule (Module #9)
**File**: `Assets/Scripts/Gameplay/Character/Core/Modules/YisoCharacterWeaponModule.cs`

Manages weapon lifecycle (creation, equipping, destruction).

**Key Methods**:
```csharp
void EquipWeapon(YisoWeaponDataSO weaponData);
void UnequipWeapon();
void ActivateWeapon();   // Start attack
void DeactivateWeapon(); // End attack
YisoWeaponInstance CurrentWeapon { get; }
```

**Lifecycle**:
1. EquipWeapon() creates weapon instance from SO data
2. ActivateWeapon() called from ability/animation event
3. Weapon deals damage while active
4. DeactivateWeapon() stops damage detection
5. UnequipWeapon() destroys weapon instance

---

#### YisoWeaponInstance
**File**: `Assets/Scripts/Gameplay/Character/Weapon/YisoWeaponInstance.cs`

Runtime weapon instance with active/inactive states.

**Key Properties**:
```csharp
bool IsActive { get; }
YisoWeaponAim WeaponAim { get; }
YisoDamageOnTouch DamageOnTouch { get; }
```

**States**:
- Inactive: Weapon equipped but not attacking
- Active: Weapon dealing damage (collider enabled)

---

#### YisoWeaponAim
**File**: `Assets/Scripts/Gameplay/Character/Weapon/YisoWeaponAim.cs`

Aiming logic for directional attacks. Integrates with YisoOrientationAbility.

**Key Methods**:
```csharp
void SetAimDirection(Vector2 direction);
Vector2 GetCurrentAim();
```

**Integration**:
- YisoOrientationAbility reads from WeaponAim
- Weapon rotation follows aim direction
- Supports both mouse and gamepad aiming

---

#### YisoDamageOnTouch
**File**: `Assets/Scripts/Gameplay/Character/Weapon/YisoDamageOnTouch.cs`

Collision-based damage detection and application.

**Key Features**:
- Collider2D-based damage detection
- Damage cooldown per target (prevents multi-hit)
- Layer mask filtering (only damages valid targets)
- Integration with Health system

**Configuration**:
```csharp
float damageAmount;
float damageCooldown;      // Per-target cooldown
LayerMask targetLayers;
```

---

#### YisoWeaponDataSO
**File**: `Assets/Scripts/Gameplay/Character/Weapon/YisoWeaponDataSO.cs`

ScriptableObject defining weapon stats and behavior.

**Properties**:
```csharp
string weaponName;
float damage;
float attackSpeed;
float range;
GameObject weaponPrefab;  // Visual representation
```

---

### Integration Points

#### WeaponModule Initialization
**Location**: `YisoCharacter.cs` (line 172)

```csharp
weaponModule = new YisoCharacterWeaponModule();
weaponModule.Initialize(this);
```

---

#### YisoOrientationAbility Integration
**Location**: `YisoOrientationAbility.cs`

Reads from WeaponAim for aim-based direction control:
```csharp
if (weaponModule?.CurrentWeapon != null) {
    Vector2 aimDirection = weaponModule.CurrentWeapon.WeaponAim.GetCurrentAim();
    ForceFace(aimDirection);
}
```

Priority: WeaponAim > Input > Current Facing

---

#### YisoMeleeAttackAbility Integration
**Location**: `YisoMeleeAttackAbility.cs`

Triggers weapon activation via animation events:
```csharp
public void OnAnimationEvent(string eventName) {
    if (eventName == "AttackStart") {
        context.WeaponModule.ActivateWeapon();
    } else if (eventName == "AttackEnd") {
        context.WeaponModule.DeactivateWeapon();
    }
}
```

---

## Combat System

### Health/Damage System

**Location**: `Assets/Scripts/Gameplay/Health/`

#### YisoHealth
**File**: `Assets/Scripts/Gameplay/Health/YisoHealth.cs`

Character health management component.

**Key Methods**:
```csharp
void TakeDamage(float damage);
void Heal(float amount);
void Die();
bool IsDead { get; }
float CurrentHealth { get; }
float MaxHealth { get; }
```

**Features**:
- Damage reduction calculation
- Invincibility frames
- Death event triggering
- Health bar integration

---

#### DamageInfo Struct
**File**: `Assets/Scripts/Gameplay/Health/DamageInfo.cs`

Struct containing damage information.

```csharp
public struct DamageInfo {
    public float damage;
    public DamageType damageType;
    public GameObject source;
    public Vector2 impactForce;
}
```

**DamageType Enum**:
- Physical
- Magical
- True (ignores defense)

---

### YisoMeleeAttackAbility

**Location**: `Assets/Scripts/Gameplay/Character/Abilities/YisoMeleeAttackAbility.cs`

Melee combat ability with input buffering and animation events.

**Key Features**:
- Input buffering (press attack before animation finishes)
- Animation event integration
- Combo system support (future)
- Attack cooldown management

**Input Buffering**:
```csharp
// If attack pressed during attack animation, buffer it
if (attackPressed && isAttacking) {
    bufferedAttack = true;
}

// Execute buffered attack when animation finishes
if (bufferedAttack && !isAttacking) {
    ExecuteAttack();
    bufferedAttack = false;
}
```

**Animation Events**:
- "AttackStart" - Activate weapon damage
- "AttackHit" - Impact timing for VFX/SFX
- "AttackEnd" - Deactivate weapon damage

---

## Field of View System

**Commits**: `4e569af` (FOV feature), `273592e` (Cone of Vision decision)

**Location**: `Assets/Scripts/Gameplay/Tools/Visual/YisoFieldOfViewRenderer.cs`

Mesh-based field of view visualization with raycasting for AI enemy detection.

### YisoFieldOfViewRenderer

Renders FOV mesh using configurable angle and range.

**Key Configuration**:
```csharp
float viewAngle = 90f;        // FOV angle in degrees
float viewRange = 10f;         // Detection range
int rayCount = 50;             // Mesh resolution
LayerMask obstacleLayer;       // What blocks vision
```

**Features**:
- Real-time mesh generation
- Raycast-based occlusion detection
- Visual debugging (Editor only)
- Performance optimized (mesh pooling)

---

### FSM Integration

#### YisoCharacterActionConeOfVision
**Location**: `Assets/Scripts/Gameplay/Character/StateMachine/Actions/YisoCharacterActionConeOfVision.cs`

FSM Action that updates FOV direction.

**Usage**:
```csharp
public override void PerformAction(IYisoCharacterContext context) {
    // Update FOV to face target
    if (target != null) {
        Vector2 direction = (target.position - transform.position).normalized;
        fovRenderer.SetDirection(direction);
    }
}
```

---

#### YisoCharacterDecisionDetectTargetConeOfVision
**Location**: `Assets/Scripts/Gameplay/Character/StateMachine/Decisions/YisoCharacterDecisionDetectTargetConeOfVision.cs`

FSM Decision detecting enemies within FOV cone.

**Logic**:
```csharp
public override bool Decide(IYisoCharacterContext context) {
    // Check if target is in FOV angle
    bool inFOV = fovRenderer.IsTargetInFOV(target);

    // Check if line of sight is clear (raycasting)
    bool lineOfSight = !Physics2D.Raycast(origin, direction, distance, obstacleLayer);

    return inFOV && lineOfSight;
}
```

---

### Usage in AI FSM

```
Idle State
  ↓ Transition (DetectTargetConeOfVision = true)
Chase State
  ↓ Action (ConeOfVision) - Update FOV direction toward player
  ↓ Action (MoveTowardTarget) - Move to player
```

---

## Movement System

### YisoMovementAbility

**Location**: `Assets/Scripts/Gameplay/Character/Abilities/YisoMovementAbility.cs`

Interpolated movement with acceleration/deceleration curves and speed multipliers.

**Key Features**:
- Smooth acceleration/deceleration
- Speed multipliers (running, walking, crouching)
- Animation parameter syncing
- State-based movement blocking

**Configuration**:
```csharp
float baseSpeed = 5f;
AnimationCurve accelerationCurve;
AnimationCurve decelerationCurve;
float accelerationTime = 0.2f;
float decelerationTime = 0.3f;
```

**Speed Multipliers**:
```csharp
enum MovementState {
    Walking,   // 1.0x
    Running,   // 1.5x
    Crouching  // 0.5x
}
```

**Ability Lifecycle**:
1. **PreProcessAbility()** - Read input, check permissions
2. **ProcessAbility()** - Calculate movement with interpolation
3. **PostProcessAbility()** - Apply to TopDownController
4. **UpdateAnimator()** - Sync speed parameter

---

### YisoOrientationAbility

**Location**: `Assets/Scripts/Gameplay/Character/Abilities/YisoOrientationAbility.cs`

Character facing direction control (always-enabled ability).

**Key Features**:
- Automatic facing based on movement
- Weapon aim integration
- Manual facing control via `ForceFace()`
- Cardinal direction conversion

**Priority System**:
1. Weapon Aim (if weapon equipped and aiming)
2. Input/Movement Direction
3. Current Facing (maintain if no input)

**ForceFace() Method**:
```csharp
public void ForceFace(Vector2 direction) {
    FacingDirections cardinalDir = ConvertToCardinal(direction);
    currentFacing = cardinalDir;
    UpdateAnimator();
}
```

---

### TopDownController

**Location**: `Assets/Scripts/Gameplay/Core/TopDownController.cs`

Rigidbody2D-based physics controller.

**Key Features**:
- Interpolated movement
- Acceleration/deceleration curves
- Knockback/impact system
- Collision handling

**Configuration**:
```csharp
float maxSpeed = 10f;
AnimationCurve accelerationCurve;
AnimationCurve decelerationCurve;
float impactFalloffCurve;  // Knockback decay
```

**Impact System**:
```csharp
public void AddImpact(Vector2 force) {
    currentImpact += force;
    // Decays over time using falloff curve
}
```

---

## Input System

**Location**: `Assets/Scripts/Gameplay/Character/Core/Modules/YisoCharacterInputModule.cs`

Unity's new Input System integration for player characters.

### Action Maps

#### Player Action Map
- Move (Vector2)
- Attack (Button)
- Dash (Button)
- Interact (Button)

#### UI Action Map
- Navigate (Vector2)
- Submit (Button)
- Cancel (Button)

**Configuration File**: `Assets/Settings/InputSystem/InputSystem_Actions.inputactions`

---

### InputModule Features

**Input Reading**:
```csharp
public Vector2 MovementInput { get; private set; }
public bool AttackPressed { get; private set; }
public bool AttackHeld { get; private set; }
```

**Runtime Action Map Switching**:
```csharp
public void SwitchToUIActionMap() {
    playerActionMap.Disable();
    uiActionMap.Enable();
}

public void SwitchToPlayerActionMap() {
    uiActionMap.Disable();
    playerActionMap.Enable();
}
```

**Input Buffering**:
- Attack input buffered during animations
- Configurable buffer window (default: 0.2s)

---

## Animation System

**Location**: `Assets/Scripts/Gameplay/Character/Core/Modules/YisoCharacterAnimationModule.cs`

### YisoCharacterAnimationModule

Integrates with Unity Animator for character animations.

**Key Features**:
- Centralized animation parameter management
- Animation event routing to AbilityModule
- State-based animation control

**Animation Parameters**:
```csharp
// Bool parameters
"IsWalking"
"IsAttacking"
"IsDead"

// Float parameters
"Speed"
"AttackSpeed"

// Triggers
"Attack"
"Hit"
"Die"
```

**Animation Event Routing**:
```csharp
// Called from Unity Animation Event
public void OnAnimationEvent(string eventName) {
    // Routes to AbilityModule
    context.OnAnimationEvent(eventName);
}
```

**Common Animation Events**:
- "AttackStart" - Begin damage detection
- "AttackHit" - Impact timing (VFX/SFX)
- "AttackEnd" - End damage detection
- "FootstepLeft" / "FootstepRight" - Footstep sounds

---

## Integration Summary

### Character Update Flow (Implemented)

```
YisoCharacter
  ↓
RunIUpdateManager.OnUpdate()
  ↓
InputModule (read player input)
  ↓
StateModule (evaluate FSM transitions)
  ↓
AbilityModule (process abilities)
  ├─ MovementAbility → calculate movement
  ├─ OrientationAbility → calculate facing
  └─ MeleeAttackAbility → process attacks
  ↓
AnimationModule (update animator)
  ↓
WeaponModule (weapon state update)
```

### Combat Flow (Implemented)

```
Player Input (Attack)
  ↓
MeleeAttackAbility.PreProcessAbility() (check can attack)
  ↓
MeleeAttackAbility.ProcessAbility() (start attack animation)
  ↓
Animation Event "AttackStart"
  ↓
WeaponModule.ActivateWeapon()
  ↓
YisoDamageOnTouch detects collision
  ↓
Target.YisoHealth.TakeDamage()
  ↓
Animation Event "AttackEnd"
  ↓
WeaponModule.DeactivateWeapon()
```

### AI Detection Flow (Implemented)

```
Enemy Idle State
  ↓
ConeOfVision Action (update FOV direction)
  ↓
DetectTargetConeOfVision Decision
  ├─ Check angle (IsTargetInFOV)
  ├─ Check line of sight (Raycast)
  └─ Returns true/false
  ↓ (true)
Transition to Chase State
```
