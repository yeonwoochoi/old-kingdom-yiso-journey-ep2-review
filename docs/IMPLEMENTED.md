# 구현 현황

새 시스템 설계를 기준으로 기존 코드의 통합 상태를 정리한다.

> **상태 표기**
> - ✅ 완료 — 새 시스템에 맞게 통합됨 (또는 그대로 사용 가능)
> - 🔧 통합 예정 — 코드 존재, 새 시스템과 연동 작업 필요
> - 📐 미구현 — 새로 구현해야 함
> - ❌ 제외 — 새 설계에서 제거 또는 대체됨

---

## Core 시스템

| 시스템 | 상태 | 비고 |
|--------|------|------|
| GameSystem | 📐 | 미구현 |
| ConfigSystem | 📐 | 미구현 |
| PoolingSystem | 📐 | 미구현 |
| EventSystem | 🔧 | `YisoEventManager` 기존 구현 → 글로벌 버스로 확장 필요 |
| TimeSystem | 📐 | 미구현 |
| InputSystem | 📐 | 미구현 (현재 Character 내부에서 처리 중 → 분리 필요) |
| SoundSystem | 📐 | 미구현 |
| UISystem | 📐 | 미구현 |
| SceneSystem | 📐 | 미구현 |

---

## Infra 시스템

| 시스템 | 상태 | 비고 |
|--------|------|------|
| AuthSystem | 🔧 | `YisoAuthService`, `YisoSessionManager` 기존 구현 있음 |
| NetworkSystem | 🔧 | TCP 핸들러 + Web HTTP 클라이언트 기존 구현 있음 |
| SaveSystem | 📐 | 미구현. 이원화 저장 로직 설계 필요 |
| ResourceSystem | 📐 | 미구현 |
| AddressableLoader | 📐 | 미구현 |

---

## World 시스템

| 시스템 | 상태 | 비고 |
|--------|------|------|
| MapSystem | 🔧 | `YisoMapController` 기본 틀 추가 (commit f02274a) — 상세 구현 필요 |
| SpawnSystem | 📐 | 미구현 |
| TriggerSystem | 📐 | 미구현 |
| InteractionSystem | 📐 | 미구현 |
| EnvironmentSystem | 📐 | 미구현 |
| CutsceneSystem | 📐 | 미구현 |

---

## Entity & Character Components

### Entity 계층

| 항목 | 상태 | 비고 |
|------|------|------|
| Entity Base 클래스 | 📐 | 미구현. 새로 정의 필요 |
| Player | 🔧 | `YisoCharacter` 기존 구현 → Entity 계층 아래로 재편 |
| Enemy | 🔧 | `YisoCharacter` 기반으로 구현됨 → 분리 및 재편 예정 |
| NPC | 📐 | 미구현 |

### Character Components (기존 구현 — 통합 예정)

| 컴포넌트 | 기존 클래스 | 상태 | 통합 계획 |
|---------|------------|------|-----------|
| MovementComponent | `YisoMovementAbility` | 🔧 | Ability → Component로 재배치 |
| AnimationComponent | `YisoCharacterAnimationModule` | 🔧 | 모듈 → Component로 재배치 |
| PhysicsComponent | `YisoHurtbox`, `PhysicsModule` | 🔧 | Hurtbox/Hitbox 로직 통합 |
| AbilityComponent | `YisoCharacterAbilityModule` | 🔧 | SkillSystem 연동 후 통합 |
| FSMComponent | `YisoCharacterStateMachine` | 🔧 | Enemy에 그대로 적용, Player 분리 검토 |

#### YisoCharacter 모듈 현황

| 모듈 | 파일 | 상태 |
|------|------|------|
| CoreModule | YisoCharacterCoreModule.cs | 🔧 |
| InputModule | YisoCharacterInputModule.cs | 🔧 → InputSystem으로 분리 예정 |
| AnimationModule | YisoCharacterAnimationModule.cs | 🔧 |
| AbilityModule | YisoCharacterAbilityModule.cs | 🔧 |
| StateModule | YisoCharacterStateModule.cs | 🔧 |
| LifecycleModule | YisoCharacterLifecycleModule.cs | 🔧 |
| SaveModule | YisoCharacterSaveModule.cs | 🔧 → SaveSystem으로 통합 예정 |
| WeaponModule | YisoCharacterWeaponModule.cs | 🔧 |

---

## Combat 시스템

| 시스템 | 상태 | 비고 |
|--------|------|------|
| DamageSystem | 🔧 | `YisoDamageProcessor`, `YisoEntityHealth` 기존 구현 — DamageSystem으로 통합 |
| StatSystem | 📐 | 미구현. 레벨업 테이블 및 스탯 합산 로직 |
| SkillSystem | 📐 | 미구현. 기존 Ability 구조 위에 구현 예정 |
| BuffSystem | 📐 | 미구현 |
| CombatSystem | 📐 | 미구현. 어그로, 타겟팅 |
| EffectSystem | 📐 | 미구현. 기존 HealthFeedback 참고 가능 |

#### Health / Damage 기존 구현

| 클래스 | 새 시스템 내 위치 |
|--------|----------------|
| YisoEntityHealth | DamageSystem / Entity |
| YisoDamageProcessor | DamageSystem |
| YisoHealthAnimator | AnimationComponent |
| YisoHealthFeedback | EffectSystem |
| YisoHealthPhysicsHandler | PhysicsComponent |
| YisoHealthUIController | UISystem / HUDSystem |
| YisoDeathLogicHandler | SpawnSystem / QuestSystem / DropSystem |
| YisoHurtbox | PhysicsComponent |
| YisoDamageOnTouch | PhysicsComponent |
| YisoFloatingText | EffectSystem (풀링) |
| YisoProgressBar | HUDSystem |

---

## FSM (기존 구현 — 통합 예정)

| 항목 | 상태 |
|------|------|
| YisoCharacterStateMachine | 🔧 Enemy FSMComponent로 그대로 활용 |
| YisoCharacterAction (Actions) | 🔧 FSMComponent 하위로 재배치 |
| YisoCharacterDecision (Decisions) | 🔧 FSMComponent 하위로 재배치 |

---

## Ability 시스템 (기존 구현 — SkillSystem과 연동 예정)

| 항목 | 상태 |
|------|------|
| YisoAbilitySO / YisoCharacterAbilityBase | 🔧 SkillSystem 연동 예정 |
| YisoMovementAbility | 🔧 MovementComponent로 이관 |
| YisoOrientationAbility | 🔧 유지 (ForceFace API 활용) |
| YisoMeleeAttackAbility | 🔧 AbilityComponent + SkillSystem 연동 |
| YisoProjectileAttackAbility | 🔧 AbilityComponent + SkillSystem 연동 |

---

## Weapon 시스템 (기존 구현 — 통합 예정)

| 클래스 | 새 시스템 내 위치 |
|--------|----------------|
| YisoWeaponInstance | AbilityComponent / CombatSystem |
| YisoWeaponAim | AbilityComponent (OrientationAbility 연동 유지) |
| YisoMeleeHitboxController | PhysicsComponent |
| YisoProjectile | PoolingSystem + PhysicsComponent |
| YisoWeaponDataSO | 그대로 유지 (정적 데이터 SO) |

---

## Player 시스템

| 시스템 | 상태 |
|--------|------|
| QuestSystem | 📐 |
| InventorySystem | 📐 |
| AchievementSystem | 📐 |

---

## Economy 시스템

| 시스템 | 상태 |
|--------|------|
| ItemSystem | 📐 |
| DropSystem | 📐 |
| ShopSystem | 📐 |
| EnhancementSystem | 📐 |
| CashShopSystem | 📐 |
| CraftSystem | ❌ 기획 방침상 스펙 아웃 권장 |

---

## Network (기존 구현 — 통합 예정)

| 항목 | 상태 | 비고 |
|------|------|------|
| YisoHttpClient / YisoApiEndpoints | 🔧 NetworkSystem/AuthSystem에 통합 |
| YisoAuthService, YisoSessionManager | 🔧 AuthSystem에 통합 |
| YisoRankService | 🔧 NetworkSystem 하위 |
| YisoTcpHandler / PacketDispatcher | 🔧 NetworkSystem (Game Server) |
| ServerShared DTOs | ✅ 그대로 유지 |

---

## Tools / 유틸리티 (기존 구현)

| 도구 | 상태 | 비고 |
|------|------|------|
| YisoStateMachine (범용 FSM) | ✅ 그대로 사용 |
| YisoFieldOfViewRenderer | ✅ 그대로 사용 |
| ArcCollider2D | ✅ 그대로 사용 |
| YisoFollowTarget | ✅ 그대로 사용 |
| YisoSurfaceModifierZone | ✅ 그대로 사용 |
| YisoEventManager | 🔧 EventSystem으로 흡수 |
| GridGenerator 시리즈 | ✅ 프로시저럴 맵 생성에 활용 |
| TilemapGenerator 시리즈 | ✅ 맵 생성에 활용 |
