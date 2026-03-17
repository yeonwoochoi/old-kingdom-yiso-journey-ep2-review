# 시스템 아키텍처

## 1. 전체 레이어 구조

```
┌──────────────────────────────────────────────────────────────────┐
│  Layer 1: 최초 진입점                                             │
│  BootStrapper (= GameSystem)                                     │
│  └── Core 시스템을 순서대로 초기화 → Login Scene 로드             │
└────────────────────────────┬─────────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│  Layer 2: Core 구축  (DontDestroyOnLoad — 모든 씬에서 유지)       │
│                                                                  │
│  초기화 순서:                                                     │
│  LogSystem → ConfigSystem → PoolingSystem → EventSystem          │
│      → TimeSystem → InputSystem → SoundSystem                    │
│      → UISystem ─┬─ UIManager                                    │
│                  └─ HUDManager                                   │
│      → SceneSystem → LocalizationSystem → CameraSystem           │
│                                                                  │
└────────────────────────────┬─────────────────────────────────────┘
                             │ Login Scene 로드
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│  Layer 3: Infra 구축  (Login Scene)                              │
│                                                                  │
│  NetworkSystem → AuthSystem ──(로그인 성공)──► SaveSystem         │
│                                               └── ResourceSystem │
│                                                     ├── AddressableLoader  │
│                                                     └── (Built-in Loader)  │
└────────────────────────────┬─────────────────────────────────────┘
                             │ 캐릭터 선택 → Game Scene 진입
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│  Layer 4: World 환경 구성  (Game Scene)                          │
│                                                                  │
│  MapSystem ──────────────────────────────────────────────────►  │
│   ├── EnvironmentSystem                                          │
│   ├── SpawnSystem                                                │
│   ├── TriggerSystem                                              │
│   └── InteractionSystem                                          │
│                                                                  │
│  PlayerSystem (MapSystem 다음 초기화)                            │
│   ├── EntityBase ──► Player / Enemy / NPC                        │
│   └── Component  ──► AnimationModule / MovementModule /          │
│                       PhysicModule / FSMModule / NavigationModule │
│                                                                  │
│  ── [Player State 프레임 (의미적 그룹, 독립 시스템)] ───           │
│   QuestSystem · InventorySystem · AchievementSystem              │
│                          │                                       │
│                          ▼                                       │
│  ── [Character State 프레임] ───────────────────────────────     │
│   SkillSystem · BuffSystem · CombatSystem                        │
│   StatSystem  · EffectSystem · DamageSystem                      │
│                          │                                       │
│                          ▼                                       │
│  ── [Economy 프레임] ───────────────────────────────────────     │
│   ItemSystem · ShopSystem · EnhancementSystem                    │
│   DropSystem · CashShopSystem                                    │
│                          │ (데이터 기반 UI 바인딩/갱신)            │
│                          ▼                                       │
│   UIManager (Binding/Update) ──► CutsceneSystem                  │
│                                                                  │
│  ScriptingSystem (독립 — MapSystem 이후 초기화)                   │
│   ├── Core  (Lexer / Parser / Runner / Context)                  │
│   ├── AST   (블록 노드 타입)                                      │
│   └── API   (IScriptAPI → 각 시스템 브릿지)                      │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## 2. Layer별 상세 설명

### Layer 1 — BootStrapper (= GameSystem)

단순 진입점. 별도 GameSystem 없이 BootStrapper가 역할 통합.

- Core 시스템을 정해진 순서대로 초기화
- 완료 후 Login Scene 로드

### Layer 2 — Core 구축

**초기화 체인 (좌→우):**

```
LogSystem → ConfigSystem → PoolingSystem → EventSystem
    → TimeSystem → InputSystem → SoundSystem
    → UISystem → SceneSystem → LocalizationSystem → CameraSystem
```

UISystem이 초기화되면 UIManager / HUDManager를 하위에 생성.

| 시스템 | 역할 요약 |
|--------|-----------|
| LogSystem | 콘솔 로그, 에러 트래킹, 애널리틱스 |
| ConfigSystem | 볼륨/해상도/키맵 로컬 저장 |
| PoolingSystem | 몬스터/투사체/이펙트/드랍 아이템 오브젝트 풀 |
| EventSystem | 글로벌 이벤트 버스 (Pub/Sub) |
| TimeSystem | DeltaTime, TimeScale, 타이머 |
| InputSystem | 키/마우스/터치 → 게임 명령 변환 |
| SoundSystem | BGM 전환, SFX 재생, 오디오 풀링 |
| UISystem | 팝업 스택, Z-Order, UIManager/HUDManager 생성 |
| SceneSystem | 씬 비동기 로딩, 로딩 스크린, 메모리 해제 |
| LocalizationSystem | 언어별 텍스트 데이터 관리. UISystem 이후 초기화. |
| CameraSystem | 플레이어 추적, 컷씬 이동, 흔들림, Boundary, Zoom. EventSystem으로 씬 전환 이벤트 수신 후 씬 타입에 맞게 동작. 하위 레이어에 Public API 제공. |

### Layer 3 — Infra 구축 (Login Scene)

```
NetworkSystem → AuthSystem ──(로그인 성공)──► SaveSystem ──► ResourceSystem
                                                               ├── AddressableLoader
                                                               └── Built-in Loader
```

- **NetworkSystem → AuthSystem:** 게스트/플랫폼 로그인 처리
- **AuthSystem → SaveSystem:** 로그인 성공 후 유저 데이터 로드
- **SaveSystem → ResourceSystem:** 데이터 로드 완료 후 에셋 로딩 준비
- **ResourceSystem:** Addressables(챕터별 대형 에셋) + Built-in(Login/Loading Scene 에셋). DontDestroyOnLoad로 전역 접근.

### Layer 4 — World 환경 구성 (Game Scene)

**초기화 순서 (다이어그램 기준):**

```
MapSystem → PlayerSystem → [Player State] → [Character State] → [Economy] → UIManager Binding
    │
    └── EnvironmentSystem / SpawnSystem / TriggerSystem / InteractionSystem
```

**프레임(Frame) 표기의 의미:**
- `Player State`, `Character State`, `Economy`는 **UML 프레임으로 표기된 의미적 그룹**이다.
- 각 시스템은 독립적으로 초기화되는 별개 시스템이며, 소유/부모 관계가 아니다.
- 초기화 순서 보장을 위해 화살표로 연결되어 있다.

**UIManager Binding 표기의 의미:**
- Layer 4 끝에 있는 "UI System (Binding/Update)"는 별도 시스템이 아니다.
- Economy 시스템(ItemSystem 등)이 초기화 완료되면, 기존 UIManager가 데이터 바인딩 가능해지는 시점을 나타낸다. (예: InventoryUI의 슬롯에 아이템 이미지 바인딩)

---

## 3. 시스템 간 통신 규칙

| 방법 | 사용 상황 |
|------|-----------|
| **EventSystem (Pub/Sub)** | 시스템 간 결합이 없어야 할 때 (몬스터 처치 → 퀘스트 업데이트) |
| **직접 참조** | 명확한 소유 관계 또는 강한 의존 관계 (PlayerSystem → EntityBase) |
| **CameraSystem Public API** | 하위 레이어(컷씬, 트리거 등)가 카메라 제어가 필요할 때 |
| **인터페이스** | 구현체 교체 가능성이 있을 때 |

### 의존성 방향

```
UI Layer
   ↑
Economy
   ↑
Combat / Player State
   ↑
Entity & Components
   ↑
World (Map, Spawn, ...)
   ↑
Core & Infra
```

하위 레이어는 상위 레이어를 직접 참조하지 않고 EventSystem으로 통지한다.

---

## 4. Entity 계층

```
Entity (최상위 — ID, Transform, HP, 상태)
 ├── Character (공통 베이스 — 이동/애니메이션/물리 공유)
 │    ├── Player   — InputSystem 연결, InventorySystem 연결
 │    ├── Enemy    — FSMModule, NavigationModule, 어그로 타겟팅
 │    └── NPC      — Character 상속 (이동/애니메이션 공유), 상점 DB 연결, 퀘스트 마커
 ```

> NPC도 Character를 상속한다. 비적대적이지만 이동/애니메이션 모듈을 재사용.

### Character Component 구성

```
Character
 ├── AnimationModule   — 애니메이터 상태 관리, 이벤트 타이밍
 ├── MovementModule    — 2D 탑다운 이동, 대쉬, 넉백
 ├── PhysicModule      — Hitbox / Hurtbox
 ├── FSMModule         — AI 상태기 (Enemy/NPC용)
 └── NavigationModule  — 경로 탐색 A* (Enemy FSM 이동에 사용)
```

---

## 5. CameraSystem 상세

**초기화 위치:** Layer 2, SceneSystem 이후

**역할:**
- 플레이어 Transform 추적 (데드존, 스무딩)
- SceneSystem의 씬 전환 이벤트 수신 → 씬 타입에 맞게 동작 전환
- 컷씬 플로우에 따른 카메라 이동
- 카메라 흔들림 (Camera Shake)
- 영역(Area) 기반 Boundary 제한
- Orthographic Size 조절 (Area Trigger → Zoom In/Out)

**Public API (하위 레이어 사용):**

```csharp
public interface ICameraSystem
{
    // 추적 대상 설정 (PlayerSystem이 호출)
    void SetTarget(Transform target);

    // 컷씬 제어
    void MoveToPosition(Vector3 position, float duration);
    void ReleaseControl();          // 컷씬 종료 후 추적 복귀

    // 효과
    void Shake(float intensity, float duration);
    void ZoomTo(float orthographicSize, float duration);

    // Boundary
    void SetBoundary(Bounds boundary);
    void ClearBoundary();
}
```

**씬 타입별 기본 동작:**

| SceneType | 기본 동작 |
|-----------|-----------|
| BaseCamp | 플레이어 추적, Boundary 없음 |
| Chapter | 플레이어 추적, 필드/맵 경계 Boundary |
| InfiniteDojo | 플레이어 추적, 인스턴스 맵 Boundary |
| Login | 고정 or 설정된 기본 위치 |

---

## 6. 씬 구성

| 씬 | 역할 |
|----|------|
| Bootstrap Scene | Core 시스템 초기화. 이후 Login Scene 전환 |
| Login Scene | Infra(Auth/Save/Resource) 초기화, 캐릭터 선택 |
| Base Camp Scene | 중간 맵 — 강화, 무한 도장, 스테이지 선택 |
| Chapter Scene | 챕터 — 중심 마을 + 방사형 필드 |
| Infinite Dojo Scene | 무한 도장 인스턴스 (세션 단위) |

---

## 7. 기존 코드 통합 방향

| 기존 코드 | 새 시스템 내 위치 | 방향 |
|-----------|-----------------|------|
| YisoCharacter + 9모듈 | Entity/Character + Components | 모듈 → Component 재편 |
| YisoCharacterStateMachine | FSMModule | Enemy에 그대로 활용 |
| Ability 시스템 (SO 기반) | Component + SkillSystem 연동 | SkillSystem이 Ability 실행 |
| Health / Damage 시스템 | DamageSystem + Entity | DamageSystem이 EntityHealth 통해 HP 차감 |
| Weapon 시스템 | PhysicModule + SkillSystem | Ability 실행 결과로 Hitbox 활성화 |
| YisoEventManager | EventSystem | 글로벌 EventSystem으로 통합 |
