# API 레퍼런스

시스템 간 인터페이스 계약 및 주요 API 정의.
현재 구현된 API와 설계 중인 API를 함께 기술한다.

> **구현 상태 표기**
> - ✅ 구현 완료
> - 🔧 기존 코드 있음 (새 시스템에 맞게 통합 예정)
> - 📐 설계 중 (미구현)

---

## 1. Core 시스템 API

### EventSystem 📐

```csharp
public interface IEventSystem
{
    void Publish<T>(T evt) where T : struct;
    void Subscribe<T>(Action<T> handler) where T : struct;
    void Unsubscribe<T>(Action<T> handler) where T : struct;
}

// 주요 이벤트 타입 (설계 예정)
public struct EnemyDefeatedEvent   { public Entity Enemy; public Entity Killer; }
public struct QuestUpdatedEvent    { public int QuestId; public QuestStatus Status; }
public struct ItemAcquiredEvent    { public ItemData Item; public int Amount; }
public struct PlayerLevelUpEvent   { public int NewLevel; }
public struct ChapterClearedEvent  { public int ChapterId; }
public struct RetreatRequestedEvent { }
```

### SaveSystem 📐

이원화 저장 로직의 핵심 API.

```csharp
public interface ISaveSystem
{
    // 일반 저장 — 모든 상태 포함
    Task SaveAllAsync();

    // 후퇴 저장 — 챕터 진행도 제외
    // 레벨, 장비, 골드, 스킬만 보존. 챕터 퀘스트/위치 파기.
    Task SaveForRetreatAsync();

    // 로드
    Task<SaveData> LoadAsync();
}
```

### SceneSystem 📐

```csharp
public interface ISceneSystem
{
    Task LoadSceneAsync(SceneType scene, LoadingScreenType loading = LoadingScreenType.Default);
    Task UnloadCurrentSceneAsync();
    SceneType CurrentScene { get; }
}

public enum SceneType
{
    Bootstrap, Login, BaseCamp, Chapter, InfiniteDojo
}
```

---

## 2. World 시스템 API

### MapSystem 📐

```csharp
public interface IMapSystem
{
    // 현재 맵 정보
    MapData CurrentMap { get; }
    bool IsInSafeZone { get; }  // 마을 판별

    // 필드 해금
    void UnlockField(int fieldId);
    bool IsFieldUnlocked(int fieldId);

    // 미니맵/월드맵에 데이터 제공
    MapNodeData[] GetAllNodes();
    Vector3 GetPlayerPosition();
}
```

### SpawnSystem 📐

```csharp
public interface ISpawnSystem
{
    Entity SpawnEnemy(EnemyData data, Vector3 position);
    Entity SpawnNPC(NpcData data, Vector3 position);
    void SpawnPortal(PortalData data, Vector3 position);

    // 무한 도장 특수 스폰
    Entity SpawnDojoTarget(DojoMissionData mission);
}
```

---

## 3. Combat 시스템 API

### DamageSystem 📐

```csharp
public interface IDamageSystem
{
    DamageResult CalculateDamage(DamageContext ctx);
    void ApplyDamage(Entity target, DamageResult result);
}

public struct DamageContext
{
    public Entity Attacker;
    public Entity Defender;
    public float BaseDamage;
    public DamageType Type;        // Physical, Skill, ...
    public bool IgnoreDefense;
}

public struct DamageResult
{
    public float FinalDamage;
    public bool IsCritical;
    public bool IsKill;
}
```

### StatSystem 📐

```csharp
public interface IStatSystem
{
    // 스탯 조회
    float GetStat(Entity entity, StatType type);

    // 경험치 / 레벨
    void AddExp(Entity entity, int amount);
    int GetLevel(Entity entity);

    // 레벨업 테이블 기반 자동 상승 (유저 직접 분배 없음)
    StatSnapshot GetBaseStats(int level);

    // 장비 강화 수치 포함 최종 합산
    StatSnapshot GetFinalStats(Entity entity);
}

public enum StatType
{
    MaxHP, Attack, Defense, Speed, CriticalRate, CriticalDamage
}
```

### SkillSystem 📐

```csharp
public interface ISkillSystem
{
    // 스킬 해금 (보스 처치 시 호출)
    void UnlockSkill(int skillId);
    bool IsSkillUnlocked(int skillId);

    // 장착
    void EquipSkill(int skillId, int slotIndex);

    // 실행
    void ExecuteSkill(Entity caster, int slotIndex);

    // 쿨타임
    float GetCooldownRemaining(int slotIndex);
}
```

---

## 4. Player 시스템 API

### QuestSystem 📐

```csharp
public interface IQuestSystem
{
    // 수주 / 완료 / 보상
    void AcceptQuest(int questId);
    void UpdateQuestProgress(int questId, int objectiveIndex, int amount);
    void CompleteQuest(int questId);

    // 후퇴 시 챕터 퀘스트 롤백
    void RollbackChapterQuests(int chapterId);

    // 무한 도장 세션 퀘스트 발동
    void StartDojoQuest(DojoMissionData mission);

    QuestStatus GetStatus(int questId);
}
```

### InventorySystem 📐

```csharp
public interface IInventorySystem
{
    // 아이템
    void AddItem(int itemId, int amount = 1);
    void RemoveItem(int itemId, int amount = 1);
    bool HasItem(int itemId, int amount = 1);

    // 장비
    void EquipItem(int itemId, EquipSlot slot);
    void UnequipItem(EquipSlot slot);

    // 골드
    int Gold { get; }
    bool SpendGold(int amount);
    void AddGold(int amount);

    // 아이템 사용 (귀환 주문서 등)
    void UseItem(int itemId);
}
```

---

## 5. Economy 시스템 API

### EnhancementSystem 📐

```csharp
public interface IEnhancementSystem
{
    // 강화 비용 조회 (장비 현재 레벨 기반)
    int GetEnhanceCost(int itemId);

    // 강화 성공률 조회
    float GetSuccessRate(int itemId);

    // 강화 실행 — 골드만 소모
    EnhanceResult TryEnhance(int itemId);
}

public struct EnhanceResult
{
    public bool Success;
    public int GoldSpent;
    public StatDelta StatIncrease;  // 성공 시 상승한 수치
}
```

---

## 6. Character Component API (기존 코드 통합)

> 🔧 기존 `YisoCharacter` + 9개 모듈 구조를 새 시스템과 연동하며 통합 예정.
> 아래 API는 현재 구현된 인터페이스 기준.

### IYisoCharacterContext

FSM Actions/Decisions가 Character를 제어하는 인터페이스.

```csharp
public interface IYisoCharacterContext
{
    // 이동
    void Move(Vector2 direction);
    void StopMovement();

    // 방향
    void Face(FacingDirections direction);
    void Face(Vector2 direction);       // YisoOrientationAbility.ForceFace()로 위임

    // 공격
    void Attack();
    void ChangeWeapon(YisoWeaponDataSO weaponData);

    // 상태 조회
    bool IsAttacking    { get; }
    bool IsDead         { get; }
    Transform TargetTransform { get; }
    Vector3 SpawnPosition     { get; }
}

public enum FacingDirections { North, South, East, West }
```

### FSM Action 구조 🔧

```csharp
// Assets/Scripts/Gameplay/Character/StateMachine/Actions/
public abstract class YisoCharacterAction
{
    public virtual void OnEnter(IYisoCharacterContext ctx)  { }
    public virtual void OnUpdate(IYisoCharacterContext ctx) { }
    public virtual void OnExit(IYisoCharacterContext ctx)   { }
}
```

**구현된 Actions:**

| 분류 | 클래스 |
|------|--------|
| 이동 | MoveTowardTarget, MoveRandomly, Patrol, ReturnToSpawn, StopMovement |
| 공격 | Attack, ChangeWeapon |
| 방향 | ConeOfVision, FaceTowardTarget |
| 기타 | DoNothing, SetAnimator |

### FSM Decision 구조 🔧

```csharp
public abstract class YisoCharacterDecision
{
    public abstract bool Decide(IYisoCharacterContext ctx);
}
```

**구현된 Decisions:**

| 클래스 | 조건 |
|--------|------|
| DetectTargetInRadius | 반경 내 타겟 감지 |
| DetectTargetConeOfVision | 시야 콘 내 타겟 감지 |
| DistanceToTarget / DistanceToSpawn | 거리 비교 |
| TargetIsAlive / TargetIsNull | 타겟 상태 |
| IsNotAttacking | 공격 중 여부 |
| TimeInState | 현재 상태 체류 시간 |

### Ability 시스템 🔧

```csharp
// 데이터 레이어 (ScriptableObject)
public abstract class YisoAbilitySO : ScriptableObject
{
    public abstract YisoCharacterAbilityBase CreateAbility();
}

// 로직 레이어
public abstract class YisoCharacterAbilityBase
{
    public abstract void Initialize(YisoCharacter character);
    public abstract void Execute();
    public abstract void Terminate();
}
```

**구현된 Abilities:**

| SO | Ability |
|----|---------|
| YisoMovementAbilitySO | YisoMovementAbility |
| YisoOrientationAbilitySO | YisoOrientationAbility (ForceFace 포함) |
| YisoMeleeAttackAbilitySO | YisoMeleeAttackAbility |
| YisoProjectileAttackAbilitySO | YisoProjectileAttackAbility |

> **통합 계획:** 보스 해금 스킬도 Ability 구조 위에 구현하여 SkillSystem과 연동할 예정.

---

## 7. 네이밍 컨벤션

| 규칙 | 예시 |
|------|------|
| 모든 게임 클래스 `Yiso` 접두사 | `YisoCharacter`, `YisoMapSystem` |
| ScriptableObject `SO` 접미사 | `YisoAbilitySO`, `YisoWeaponDataSO` |
| 인터페이스 `I` 접두사 | `IEventSystem`, `ISaveSystem` |
| 시스템 클래스 `System` 접미사 | `YisoQuestSystem`, `YisoStatSystem` |
| FSM Action | `YisoCharacterAction[행동]` |
| FSM Decision | `YisoCharacterDecision[조건]` |
| 한국어 주석 | 팀 내부 코드 주석은 한국어 사용 |
