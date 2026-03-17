# ScriptingSystem 설계

기획자가 Unity EditorWindow 비주얼 빌더로 콘텐츠를 구성하면,
툴이 `.yiso` 텍스트 파일을 자동 생성하고 런타임에 ScriptingSystem이 파싱·실행한다.

---

## 역할 & 책임범위

### ScriptingSystem
- **역할:** `.yiso` 스크립트 파일을 런타임에 파싱·실행하는 콘텐츠 스크립팅 엔진
- **책임:**
  - `.yiso` 파일 Lexing → Parsing → AST 생성
  - Coroutine 기반 블록 실행 (WAIT, 분기, 순서 보장)
  - 런타임 변수·플래그 상태 유지 (`ScriptContext`)
  - 각 시스템 호출 브릿지 (`ScriptAPI`) 제공
  - Unity EditorWindow 비주얼 빌더 — UI 조작으로 `.yiso` 자동 생성 (기획자가 DSL 직접 작성 불필요)
- **스크립팅 대상 시스템:**

| 스크립트 타입 | 연동 시스템 | 도입 Phase |
|--------------|------------|-----------|
| `@dialogue`  | InteractionSystem → UIManager | Phase 6 |
| `@quest`     | QuestSystem | Phase 6 |
| `@trigger`   | TriggerSystem | Phase 5 |
| `@wave`      | SpawnSystem | Phase 5 |
| `@cutscene`  | CutsceneSystem | Phase 9 |

- **스크립팅 비대상:** StatSystem, DamageSystem, SaveSystem, AuthSystem 등 엔진 레벨 시스템
  → 수치 계산 정밀도·신뢰성이 필요하므로 C# 하드코딩 유지

---

## `.yiso` 파일 포맷

파일 확장자: `.yiso`
블록 단위로 작성. 하나의 파일에 여러 블록 혼재 가능.

### 공통 문법

```
// 주석

@<타입> <id>          // 블록 선언
  <명령어 또는 대화>   // 들여쓰기로 블록 범위 구분
  END                  // 블록 종료 (분기 종료 시)
```

---

### @dialogue — 대화 스크립트

NPC 대화 트리. InteractionSystem이 실행 요청 → ScriptRunner가 UIManager에 렌더링 위임.

```
@dialogue village_elder_intro

  [마을장로]: 어서오게, 용사여.
  [마을장로]: 우리 마을에 위기가 닥쳤다네.

  ? HasQuest("deliver_letter")
    [마을장로]: 편지를 가져왔군. 고맙네.
    SET flag.elder_met = true
    END

  ? default
    [마을장로]: 먼저 마을 경비병을 만나보게.
    GIVE_QUEST("village_defense")
    END
```

| 문법 | 설명 |
|------|------|
| `[화자]: 텍스트` | 대화 한 줄 |
| `? <조건>` | 분기 조건 (`HasQuest`, `HasFlag`, `HasItem` 등) |
| `? default` | 조건 미충족 시 기본 분기 |
| `SET flag.<key> = <value>` | 플래그 변수 설정 |
| `GIVE_QUEST("<id>")` | 퀘스트 수주 |
| `END` | 현재 분기 종료 |

---

### @quest — 퀘스트 스크립트

퀘스트 정의. QuestSystem이 로드하여 진행도 관리.

```
@quest village_defense

  TITLE "마을을 지켜라"
  DESC  "고블린들로부터 마을을 지켜라."

  OBJECTIVE kill("goblin")    count(10) label("고블린 처치")
  OBJECTIVE talk("마을장로")              label("장로에게 보고")

  ON_START
    EVENT("SpawnGoblins")

  ON_COMPLETE
    REWARD exp(500) gold(200)
    UNLOCK_QUEST("chapter1_main")
```

| 문법 | 설명 |
|------|------|
| `TITLE / DESC` | 퀘스트 표시 이름·설명 |
| `OBJECTIVE kill("<id>") count(<n>)` | 처치 목표 |
| `OBJECTIVE talk("<npc_id>")` | 대화 목표 |
| `OBJECTIVE reach("<zone_id>")` | 구역 도달 목표 |
| `ON_START / ON_COMPLETE` | 이벤트 훅 |
| `REWARD exp(<n>) gold(<n>)` | 보상 |
| `UNLOCK_QUEST("<id>")` | 다음 퀘스트 해금 |

---

### @trigger — 트리거 스크립트

구역 진입 시 실행할 명령 시퀀스. TriggerSystem이 감지 후 ScriptRunner에 위임.

```
@trigger boss_room_enter

  CAMERA.ZoomTo(4.0, 0.5)
  SOUND.Play("boss_bgm")
  EVENT("LockDoor")
  CUTSCENE("boss_intro")
```

---

### @wave — 웨이브 스크립트

무한 도장 또는 보스방 몬스터 소환 패턴. SpawnSystem이 읽어 실행.

```
@wave dojo_session_1

  WAVE(1)
    SPAWN "goblin"  count(5) interval(1.0)

  WAVE(2)
    SPAWN "orc"     count(3)
    SPAWN "archer"  count(2)

  WAVE(3) [boss_wave]
    SPAWN "goblin_chief" count(1)
```

---

### @cutscene — 컷씬 스크립트

카메라·사운드·대화·이벤트를 순서대로 실행하는 연출 시퀀스.

```
@cutscene boss_intro

  INPUT.Disable()
  CAMERA.MoveTo(boss_room_center, 1.5)
  WAIT(1.0)
  [보스]: 드디어 왔군...
  CAMERA.Shake(0.3, 0.5)
  SOUND.Play("boss_bgm")
  INPUT.Enable()
  END
```

---

### 공통 커맨드 목록

| 커맨드 | 설명 |
|--------|------|
| `WAIT(<sec>)` | 지정 시간 대기 |
| `EVENT("<id>")` | EventSystem 이벤트 발행 |
| `CAMERA.MoveTo(<pos>, <dur>)` | 카메라 이동 |
| `CAMERA.Shake(<intensity>, <dur>)` | 카메라 흔들림 |
| `CAMERA.ZoomTo(<size>, <dur>)` | 카메라 줌 |
| `SOUND.Play("<id>")` | BGM/SFX 재생 |
| `INPUT.Enable() / Disable()` | 입력 활성/차단 |
| `CUTSCENE("<id>")` | 컷씬 블록 실행 |
| `SET flag.<key> = <value>` | 플래그 변수 쓰기 |
| `GET flag.<key>` | 플래그 변수 읽기 |

---

## 아키텍처

### 컴포넌트 구성

```
ScriptingSystem
 │
 ├── Core
 │    ├── YisoScriptAsset       // TextAsset 래퍼 + 블록 타입 메타데이터
 │    ├── YisoScriptLexer       // 문자열 → 토큰 스트림
 │    ├── YisoScriptParser      // 토큰 → AST
 │    ├── YisoScriptRunner      // Coroutine 기반 AST 실행 엔진
 │    └── YisoScriptContext     // 런타임 변수·플래그 상태
 │
 ├── AST
 │    ├── ScriptBlockNode       // @dialogue, @quest, @cutscene, @wave, @trigger
 │    ├── DialogueLineNode      // [화자]: 텍스트
 │    ├── BranchNode            // ? condition / ? default
 │    ├── CommandNode           // CAMERA.MoveTo(…), EVENT(…) 등
 │    ├── ObjectiveNode         // OBJECTIVE kill/talk/reach
 │    └── WaveNode              // WAVE(n) + SPAWN
 │
 └── API (시스템 브릿지)
      ├── IScriptAPI            // 공통 인터페이스
      ├── DialogueScriptAPI     // → UIManager (대화 렌더링)
      ├── CameraScriptAPI       // → CameraSystem
      ├── QuestScriptAPI        // → QuestSystem
      ├── EventScriptAPI        // → EventSystem
      ├── SoundScriptAPI        // → SoundSystem
      ├── SpawnScriptAPI        // → SpawnSystem
      └── InputScriptAPI        // → InputSystem
```

### 실행 흐름

```
.yiso 파일
  → YisoScriptLexer   (문자열 → 토큰)
  → YisoScriptParser  (토큰 → AST)
  → YisoScriptRunner.Execute("<block_id>")   ← StartCoroutine
       └── 노드 순회
            ├── DialogueLineNode  → DialogueScriptAPI → UIManager.ShowDialogue()
            ├── CommandNode       → CameraScriptAPI   → CameraSystem.MoveTo()
            ├── BranchNode        → ScriptContext 조건 평가 → 분기
            └── WaveNode          → SpawnScriptAPI    → SpawnSystem.Spawn()
```

### 시스템 연동 방식

ScriptingSystem은 각 게임 시스템을 직접 참조하지 않는다.
`IScriptAPI` 인터페이스를 통해 브릿지하여 결합도를 낮춘다.

```csharp
public interface IScriptAPI
{
    void Register(YisoScriptRunner runner);
}

// 예: CameraScriptAPI
public class CameraScriptAPI : IScriptAPI
{
    private ICameraSystem _camera;

    public void Register(YisoScriptRunner runner)
    {
        runner.RegisterCommand("CAMERA.MoveTo",  OnMoveTo);
        runner.RegisterCommand("CAMERA.Shake",   OnShake);
        runner.RegisterCommand("CAMERA.ZoomTo",  OnZoomTo);
    }
}
```

---

## Unity EditorWindow — 비주얼 빌더

기획자가 DSL을 직접 작성하지 않는다. UI 조작으로 콘텐츠를 구성하면 툴이 `.yiso` 파일을 자동 생성한다.

### 주요 기능

| 기능 | 설명 |
|------|------|
| 비주얼 빌더 | 버튼 클릭·드롭다운·입력필드로 스크립트 블록 구성 |
| .yiso 자동 생성 | 저장 시 UI 데이터 → `.yiso` 텍스트 직렬화 |
| 파일 브라우저 | 타입별 `.yiso` 파일 목록 (Dialogue / Quest / Trigger / Wave / Cutscene) |
| 실시간 검증 | 저장 시 생성된 `.yiso` 파싱 검증 → 오류 즉시 표시 |
| 플레이모드 실행 | `▶ 실행` 버튼으로 선택 블록 즉시 테스트 (빌드 불필요) |
| ID 드롭다운 | 등록된 Quest ID, NPC ID, Zone ID 선택 → 오타 없음 |

### 레이아웃 (대화 빌더 예시)

```
┌─────────────────────────────────────────────────────┐
│  Yiso Script Builder                     [▶ 실행]   │
├──────────────┬──────────────────────────────────────┤
│ 📁 Dialogue  │  Dialogue ID: [village_elder_intro]  │
│   elder.yiso │                                      │
│   shop.yiso  │  [+ 대화 추가]  [+ 분기 추가]         │
│ 📁 Quest     │                                      │
│   main.yiso  │  ① [마을장로 ▼] 어서오게, 용사여.     │
│ 📁 Cutscene  │  ② [마을장로 ▼] 우리 마을에 위기가... │
│   boss.yiso  │                                      │
│              │  ▼ 분기                              │
│              │    조건 [HasQuest ▼] ["deliver_… ▼]  │
│              │    ├ ③ [마을장로 ▼] 편지를 가져왔군.  │
│              │    └ [+ 액션] [SET flag ▼] [END ▼]   │
│              │                                      │
│              │    조건 [default]                    │
│              │    ├ ④ [마을장로 ▼] 경비병을 만나보게 │
│              │    └ [+ 액션] [GIVE_QUEST ▼] [END ▼] │
│              ├──────────────────────────────────────┤
│              │  [💾 저장]  ✅ 파싱 성공 | 오류 없음  │
└──────────────┴──────────────────────────────────────┘
```

---

## 스크립트 파일 로딩 전략

`StreamingAssets`는 기기에서 직접 접근 가능한 폴더이므로, 릴리즈 빌드에서 유저가 퀘스트 플로우 등을 임의로 수정할 수 있다. 빌드 타입에 따라 로딩 경로를 분리한다.

### 빌드 타입별 로딩 경로

| 빌드 타입 | 로딩 경로 | 이유 |
|-----------|----------|------|
| `UNITY_EDITOR` / `DEVELOPMENT_BUILD` | `StreamingAssets/Scripts/` | 파일 수정 즉시 반영 — 빌드 없이 핫리로드 |
| Release | Addressables 번들 내부 | 기기에서 직접 접근 불가 — 유저 수정 차단 |

### ScriptRunner 로딩 분기

```csharp
private string LoadScriptText(string scriptId)
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // StreamingAssets에서 직접 읽기 (핫리로드)
    var path = Path.Combine(Application.streamingAssetsPath, "Scripts", scriptId + ".yiso");
    return File.ReadAllText(path);
#else
    // Addressables 번들에서 로드 (릴리즈)
    return _resourceSystem.Load<TextAsset>(scriptId).text;
#endif
}
```

ScriptRunner는 문자열만 받아서 파싱하므로 로딩 방식에 무관하다.

### 워크플로우

```
[개발 중]
  비주얼 빌더 → .yiso 저장 (StreamingAssets/)
  → 플레이모드 실행 → 즉시 반영 → 수정 반복

[릴리즈 빌드]
  StreamingAssets/Scripts/ → Addressables 번들로 패킹
  → 기기에서 파일 직접 접근 불가
```

---

## 폴더 구조

```
Assets/Scripts/World/Scripting/
├── Core/
│    ├── YisoScriptAsset.cs
│    ├── YisoScriptLexer.cs
│    ├── YisoScriptParser.cs
│    ├── YisoScriptRunner.cs
│    └── YisoScriptContext.cs
├── AST/
│    ├── ScriptBlockNode.cs
│    ├── DialogueLineNode.cs
│    ├── BranchNode.cs
│    ├── CommandNode.cs
│    ├── ObjectiveNode.cs
│    └── WaveNode.cs
├── API/
│    ├── IScriptAPI.cs
│    ├── DialogueScriptAPI.cs
│    ├── CameraScriptAPI.cs
│    ├── QuestScriptAPI.cs
│    ├── EventScriptAPI.cs
│    ├── SoundScriptAPI.cs
│    ├── SpawnScriptAPI.cs
│    └── InputScriptAPI.cs
└── Editor/
     └── YisoScriptEditorWindow.cs

Assets/StreamingAssets/Scripts/    // .yiso 파일 저장 위치
├── Dialogue/
├── Quest/
├── Trigger/
├── Wave/
└── Cutscene/
```
