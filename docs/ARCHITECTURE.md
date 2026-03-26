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

### 씬 내 오브젝트 구성 (극도로 미니멀)

각 씬(Login, GameMap 계열)의 Hierarchy에는 오브젝트가 3개뿐이다.

```
[Scene Root]
├── MainCamera
├── Global Light 2D
└── SceneLogin   (또는 SceneField, SceneBaseCamp 등 — SceneBase 서브클래스)
```

모든 나머지 오브젝트는 SceneBase 서브클래스가 Awake/Start에서 동적으로 생성한다.

---

## 7. GameApp — DontDestroyOnLoad 구조

```
GameApp  (DontDestroyOnLoad)
 ├── LogSystem
 ├── ConfigSystem
 ├── PoolingSystem
 ├── EventSystem
 ├── TimeSystem
 ├── InputSystem
 ├── SoundSystem
 ├── UISystem
 ├── SceneSystem
 ├── LocalizationSystem
 ├── CameraSystem
 ├── PlatformSystem
 │
 ├── NetworkSystem   ┐
 ├── AuthSystem      │  Infra (Login Scene 이후 초기화)
 ├── SaveSystem      │
 └── ResourceSystem  ┘
```

- 각 시스템은 **개별 Addressable 프리팹**으로 등록된다.
- GameApp이 부팅 시 Addressable을 통해 각 프리팹을 로드하고, 자신의 하위 오브젝트로 Instantiate한 뒤 순서대로 초기화한다.
- 이후 씬이 바뀌어도 GameApp과 그 하위 시스템은 유지된다.

---

## 8. World 구조 — 데이터·풀·오브젝트 계층

Layer 4(Game Scene)의 동적 오브젝트 생성 구조.

### 8-1. 세 계층

```
┌─────────────────────────────────────────────────────┐
│  SO Layer  (Addressable 등록 — 정적 데이터)           │
│  MapDataSO, NpcSO, MobSO, PortalSO, ReactorSO ...   │
└──────────────────────┬──────────────────────────────┘
                       │  SO → Instance 변환
                       ▼
┌─────────────────────────────────────────────────────┐
│  Instance Layer  (런타임 데이터 객체)                  │
│  MapData, Npc, Mob, Portal, Reactor ...              │
└──────────────────────┬──────────────────────────────┘
                       │  Instance + Prefab → GameObject
                       ▼
┌─────────────────────────────────────────────────────┐
│  Pool Layer  (Singleton — 오브젝트 풀 관리)            │
│  UserPool, NpcPool, MobPool, DropPool,               │
│  EffectPool, DamageFontPool, PortalPool, ReactorPool │
└─────────────────────────────────────────────────────┘
```

- **SO Layer:** Addressable로 등록된 ScriptableObject. 맵·몬스터·NPC 등 정적 스펙 정의.
- **Instance Layer:** SO를 기반으로 생성된 런타임 데이터 객체. 인게임 상태(HP, 위치 등) 보유.
- **Pool Layer:** 싱글턴. Instance 데이터 + Addressable 프리팹(오브젝트 타입당 1개)을 결합해 GameObject를 Instantiate하고 풀링 관리.

### 8-2. 프리팹 규칙

동적 오브젝트는 **타입당 프리팹 1개** 원칙을 따른다. 모두 Addressable에 등록.

| 프리팹 | 설명 |
|--------|------|
| `User.prefab` | 플레이어 캐릭터 |
| `Mob.prefab` | 모든 몬스터 공통 프리팹 |
| `Npc.prefab` | 모든 NPC 공통 프리팹 |
| `Drop.prefab` | 드랍 아이템 |
| `Effect.prefab` | 이펙트 |
| `DamageFont.prefab` | 데미지 플로팅 텍스트 |
| `Portal.prefab` | 포탈 |
| `Reactor.prefab` | 반응 오브젝트 (함정, 스위치 등) |

### 8-3. Prefab 구조 — Mob / Npc / User

동적 오브젝트 프리팹은 **Base 프리팹 1개 + Prefab Variant** 방식.

```
Mob.prefab (Base)
├── MobController       ← 비주얼, 물리, 스탯
│   ├── SpriteRenderer
│   ├── SpriteAnimator
│   ├── Collider2D
│   └── MobStats
└── FSM (child)         ← 별도 FSM 프리팹을 Instantiate하여 child로 배치

Mob_Goblin.prefab  (Prefab Variant — Inspector에서 값 오버라이드)
Mob_Orc.prefab     (Prefab Variant)
Mob_Boss.prefab    (Prefab Variant — 추가 컴포넌트 가능)
```

**FSM 프리팹** (MobController가 참조, 런타임에 child로 Instantiate):

```
FSM_Patrol.prefab
├── YisoCharacterStateMachine    ← FSM 컨트롤러
├── [States]  YisoCharacterState 컴포넌트들
├── [Actions] MonoBehaviour 컴포넌트 (MoveTowardTarget, Attack, Patrol ...)
└── [Decisions] MonoBehaviour 컴포넌트 (DetectTargetInRadius, DistanceToTarget ...)
```

FSM 프리팹 종류:

| 프리팹 | 용도 |
|--------|------|
| `FSM_Passive.prefab` | 비전투 NPC |
| `FSM_Patrol.prefab` | 일반 몬스터 (순찰 → 추격 → 공격) |
| `FSM_Aggressive.prefab` | 항상 추격형 |
| `FSM_Boss.prefab` | 페이즈 기반 보스 패턴 |

**결합도 분리 효과:**
- FSM 로직 변경 → FSM 프리팹만 수정, Mob 프리팹 무관
- 동일 FSM을 여러 몹이 공유 가능
- 기획자가 Inspector에서 파라미터(감지 범위, 공격 범위 등) 직접 조정

### 8-4. WorldManager

- **역할:** 씬 내 모든 동적 오브젝트의 생성·관리·제거 총괄. 서버 소켓 통신의 Handler 진입점.
- **초기화 흐름:**

```
서버로부터 mapId + 캐릭터 위치 수신
       │
       ▼
Addressable로 MapDataSO 동적 로드 (address = mapId)
       │
       ▼
MapDataSO → MapData 변환 (Instance Layer)
       │
       ▼
WorldManager에 MapData 등록
       │
       ▼
각 Pool을 통해 동적 오브젝트 생성 (Mob, Npc, Portal, Reactor ...)
```

- **씬 내 동적 오브젝트 계층 (WorldManager가 생성):**

```
[Scene Root]
├── User       ← UserPool이 관리하는 플레이어 인스턴스
├── Npc        ← NpcPool이 관리하는 NPC 인스턴스들
├── Mob        ← MobPool이 관리하는 몬스터 인스턴스들
├── Drop       ← DropPool
├── Effect     ← EffectPool
├── DamageFont ← DamageFontPool
├── Portal     ← PortalPool
└── Reactor    ← ReactorPool
```

각 부모 오브젝트(User, Mob 등) 하위에 해당 타입의 인스턴스들이 Instantiate된다.

### 8-4. SceneField 정적 오브젝트 계층 (레이어 구조)

SceneField(또는 SceneLogin) 하위에 **11개의 레이어 오브젝트 (Layer0~Layer10)** 가 있다. 각 레이어는 SortingGroup을 가진다.
**동적 오브젝트(캐릭터 등)와 같은 Sorting Layer는 Layer5** 로 고정한다.

```
SceneField
├── Layer0   (SortingGroup)   ┐
├── Layer1   (SortingGroup)   │  동적 오브젝트보다 아래 (배경)
├── Layer2   (SortingGroup)   │
├── Layer3   (SortingGroup)   │
├── Layer4   (SortingGroup)   ┘
│
├── Layer5   (SortingGroup)   ← 동적 오브젝트와 같은 Sorting Layer (캐릭터 depth)
│
├── Layer6   (SortingGroup)   ┐
├── Layer7   (SortingGroup)   │  동적 오브젝트보다 위 (오버레이)
├── Layer8   (SortingGroup)   │
├── Layer9   (SortingGroup)   │
└── Layer10  (SortingGroup)   ┘
```

각 레이어 하위 구조:

```
LayerN  (SortingGroup)
├── Tile  (Tilemap — 정적 타일. 필요 시 TilemapCollider2D 포함)
└── Obj   (정적 MapObject 인스턴스들)
```

| 레이어 | Sorting 위치 | 용도 예시 |
|--------|-------------|---------|
| Layer0~4 | 동적 오브젝트 **아래** | 바닥 타일, 지형 배경, 그림자 |
| Layer5 | 동적 오브젝트와 **동일** | 캐릭터와 같은 depth의 정적 요소 (바위, 나무 등) |
| Layer6~10 | 동적 오브젝트 **위** | 지붕, 나뭇가지, 안개, UI 오버레이 |

---

## 9. 정적 오브젝트 데이터화

### 9-1. Tile 데이터화

타일은 **Tilemap 유지. 런타임 `SetTile()` 동적 그리기** 방식.
TileBase 에셋은 Addressable에 등록(address = tileId), 맵 데이터에는 레이어·위치·tileId만 저장.

```csharp
public class TilemapLayerData {
    public int layer;              // 0~10
    public bool hasCollider;       // true면 런타임에 TilemapCollider2D AddComponent
    public List<TilePlacement> tiles;
}

public class TilePlacement {
    public string tileId;          // Addressable address
    public Vector3Int position;
}
```

- Collider 여부는 **타일 단위가 아닌 Tilemap 레이어 단위** 로 관리
- `hasCollider = true` 인 레이어는 런타임에 `TilemapCollider2D` + `CompositeCollider2D` 자동 부착

### 9-2. MapObject 데이터화

정적 오브젝트는 **`MapObject.prefab` 단일 프리팹 + Sprite 교체** 방식. Addressable 등록.

```csharp
public class MapObjectData {
    public string objectId;
    public string spriteAddress;   // Addressable address (SpriteAtlas 사용 권장)
    public Vector3 position;
    public bool hasCollider;
    public int layer;              // 0~10
    public int sortingOrder;
}
```

- SpriteAtlas로 묶어 DrawCall Batching 보장

### 9-3. 애니메이션 데이터화 (커스텀 SpriteAnimator)

Animator/AnimationController 미사용. Texture2D(Multiple 모드) + 인덱스 방식.

```csharp
public enum AnimationState {
    Idle_N, Idle_S, Idle_E, Idle_W,
    Walk_N, Walk_S, Walk_E, Walk_W,
    Attack_N, Attack_S, Attack_E, Attack_W,
    Die
}

public class AnimationClip {
    public AnimationState state;
    public int startIndex;   // Texture2D 슬라이스 시작 인덱스
    public int frameCount;   // 프레임 수
    public float fps;
    public bool loop;
}
```

- **인덱스 범위가 아닌 `startIndex + frameCount`** 로 저장 — 특정 상태 프레임 수 변경 시 다른 상태에 영향 없음
- 런타임: `SpriteAnimator` 컴포넌트가 Update에서 `sprites[startIndex + currentFrame]` 교체
- `loop = false` + 완료 콜백으로 Attack 등 단발 애니메이션 처리

---

## 10. 기존 코드 통합 방향

| 기존 코드 | 새 시스템 내 위치 | 방향 |
|-----------|-----------------|------|
| YisoCharacter + 9모듈 | Entity/Character + Components | 모듈 → Component 재편 |
| YisoCharacterStateMachine | FSMModule | Enemy에 그대로 활용 |
| Ability 시스템 (SO 기반) | Component + SkillSystem 연동 | SkillSystem이 Ability 실행 |
| Health / Damage 시스템 | DamageSystem + Entity | DamageSystem이 EntityHealth 통해 HP 차감 |
| Weapon 시스템 | PhysicModule + SkillSystem | Ability 실행 결과로 Hitbox 활성화 |
| YisoEventManager | EventSystem | 글로벌 EventSystem으로 통합 |
