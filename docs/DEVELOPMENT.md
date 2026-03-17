# 개발 환경 가이드

---

## 1. 환경 요구사항

| 항목 | 버전 |
|------|------|
| Unity | 6000.0.62f1 |
| 플랫폼 | Windows 10 / macOS |
| 언어 | C# (.NET Standard 2.1) |
| 렌더링 | 2D 탑다운 |

---

## 2. 프로젝트 구조

```
old-kingdom-yiso-journey-ep2/
 ├── Client/              ← Unity 프로젝트 루트
 │   └── Assets/
 │       ├── Scripts/     ← 게임 스크립트
 │       └── Editor/      ← 에디터 전용
 ├── Protocol/            ← Protobuf 스키마
 ├── Server/              ← 서버 프로젝트
 └── docs/                ← 문서
```

---

## 3. 스크립트 폴더 구조 (목표 구조)

새 시스템 설계 기준 폴더 구조. 기존 코드는 점진적으로 이 구조에 맞게 재배치한다.

```
Assets/Scripts/
 ├── Core/                   ← Layer 2 Core 시스템
 │   ├── Bootstrap/          ← BootStrapper
 │   ├── Event/              ← EventSystem
 │   ├── Pool/               ← PoolingSystem
 │   ├── Scene/              ← SceneSystem
 │   ├── Input/              ← InputSystem
 │   ├── Sound/              ← SoundSystem
 │   ├── Time/               ← TimeSystem
 │   ├── Config/             ← ConfigSystem
 │   └── Log/                ← LogSystem
 │
 ├── Infra/                  ← Layer 3 Infra 시스템
 │   ├── Auth/               ← AuthSystem
 │   ├── Save/               ← SaveSystem
 │   ├── Network/            ← NetworkSystem
 │   └── Resource/           ← ResourceSystem, AddressableLoader
 │
 ├── World/                  ← Layer 4 World 시스템
 │   ├── Map/                ← MapSystem
 │   ├── Spawn/              ← SpawnSystem
 │   ├── Trigger/            ← TriggerSystem
 │   ├── Interaction/        ← InteractionSystem
 │   ├── Environment/        ← EnvironmentSystem
 │   └── Cutscene/           ← CutsceneSystem
 │
 ├── Entity/                 ← Entity 계층
 │   ├── Base/               ← Entity, Character 베이스
 │   ├── Player/             ← Player 구현체
 │   ├── Enemy/              ← Enemy 구현체
 │   ├── NPC/                ← NPC 구현체
 │   └── Components/         ← Movement, Animation, Physics, Ability, FSM
 │
 ├── Combat/                 ← Combat 시스템
 │   ├── Damage/             ← DamageSystem
 │   ├── Stat/               ← StatSystem
 │   ├── Skill/              ← SkillSystem
 │   ├── Buff/               ← BuffSystem
 │   └── Effect/             ← EffectSystem
 │
 ├── Player/                 ← Player 상태 시스템
 │   ├── Quest/              ← QuestSystem
 │   ├── Inventory/          ← InventorySystem
 │   └── Achievement/        ← AchievementSystem
 │
 ├── Economy/                ← Economy 시스템
 │   ├── Item/               ← ItemSystem
 │   ├── Drop/               ← DropSystem
 │   ├── Shop/               ← ShopSystem
 │   └── Enhancement/        ← EnhancementSystem
 │
 ├── UI/                     ← UI 시스템
 │   ├── System/             ← UISystem (팝업 스택)
 │   └── HUD/                ← HUDSystem
 │
 └── Settings/               ← 레이어, 상수 등 전역 설정
```

### 현재 기존 코드 위치 (이전 예정)

```
Assets/Scripts/
 ├── Gameplay/Character/   → Entity/Player, Entity/Enemy, Entity/Components 로 이전
 ├── Gameplay/Health/      → Combat/Damage, Entity/Components 로 이전
 ├── Gameplay/Tools/       → 각 시스템 폴더로 이전 or 그대로 유지
 ├── Network/              → Infra/Network, Infra/Auth 로 이전
 ├── Managers/             → 각 시스템으로 흡수
 └── Core/                 → Core/ 로 이전
```

---

## 4. 코드 컨벤션

### 네이밍

| 규칙 | 예시 |
|------|------|
| 게임 클래스 `Yiso` 접두사 | `YisoMapSystem`, `YisoQuestSystem` |
| ScriptableObject `SO` 접미사 | `YisoAbilitySO`, `YisoEnemyDataSO` |
| 인터페이스 `I` 접두사 | `IEventSystem`, `ISaveSystem` |
| 시스템 `System` 접미사 | `YisoStatSystem` |
| 컴포넌트 `Component` 접미사 | `YisoMovementComponent` |
| FSM Action `YisoCharacterAction[행동]` | `YisoCharacterActionAttack` |
| FSM Decision `YisoCharacterDecision[조건]` | `YisoCharacterDecisionTargetIsAlive` |

### 주석
- **한국어:** 팀 내부 로직 주석 (기존 스타일 유지)
- **XML 주석:** 공개 인터페이스, 시스템 간 API에 권장

### 파일 배치
- 각 스크립트는 `.meta` 파일 쌍으로 유지
- 에디터 전용 코드는 `Editor` 서브폴더 또는 `#if UNITY_EDITOR` 가드

---

## 5. 새 시스템 구현 시 체크리스트

새 시스템을 만들 때 따라야 할 기준.

1. **인터페이스 먼저 정의** → `I[SystemName]` 인터페이스를 API.md에 추가
2. **EventSystem 통해 통신** → 다른 시스템과 직접 참조 대신 이벤트 발행
3. **SO 분리** → 정적 데이터는 ScriptableObject로 분리
4. **SaveSystem 연동** → 저장 대상 데이터가 있다면 SaveData 구조에 포함
5. **IMPLEMENTED.md 업데이트** → 구현 상태 갱신

---

## 6. 테스트

```
Unity Editor → Window → General → Test Runner → EditMode 탭
```

- 시스템 로직은 EditMode 테스트로 작성
- 씬 의존적인 테스트는 PlayMode 사용

---

## 7. 빌드

```
Unity Editor → File → Build Settings
```

대상 플랫폼: Android / iOS / PC (순서대로 지원 예정)

---

## 8. 네트워크 프로토콜

Protobuf 스키마는 `Protocol/Schemas/` 에서 관리.

```
Protocol/Schemas/
 ├── common.proto
 ├── game_enum.proto
 └── game_packet.proto
```

---

## 9. 참고 문서

| 문서 | 용도 |
|------|------|
| [GAME_DESIGN.md](GAME_DESIGN.md) | 게임 기획서 |
| [ARCHITECTURE.md](ARCHITECTURE.md) | 4레이어 시스템 구조 |
| [SYSTEMS.md](SYSTEMS.md) | 각 시스템 역할 상세 |
| [API.md](API.md) | 시스템 인터페이스 계약 |
| [IMPLEMENTED.md](IMPLEMENTED.md) | 구현 현황 및 통합 상태 |
| [ROADMAP.md](ROADMAP.md) | 개발 Phase 계획 |
