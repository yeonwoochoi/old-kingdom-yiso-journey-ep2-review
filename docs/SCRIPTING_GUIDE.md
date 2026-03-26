# .yiso 스크립트 작성 가이드

> 대상: 기획자
> 이 문서는 `.yiso` 스크립트 파일을 직접 작성하거나 비주얼 빌더 결과물을 검토할 때 사용합니다.

---

## 목차

1. [기본 규칙](#1-기본-규칙)
2. [블록 구조](#2-블록-구조)
3. [@dialogue — 대화 스크립트](#3-dialogue--대화-스크립트)
4. [@quest — 퀘스트 스크립트](#4-quest--퀘스트-스크립트)
5. [@trigger — 트리거 스크립트](#5-trigger--트리거-스크립트)
6. [@wave — 웨이브 스크립트](#6-wave--웨이브-스크립트)
7. [@cutscene — 컷씬 스크립트](#7-cutscene--컷씬-스크립트)
8. [커맨드 전체 목록](#8-커맨드-전체-목록)
9. [분기 조건 전체 목록](#9-분기-조건-전체-목록)
10. [자주 하는 실수](#10-자주-하는-실수)

---

## 1. 기본 규칙

### 파일 위치

```
Assets/StreamingAssets/Scripts/
├── Dialogue/    ← @dialogue 파일
├── Quest/       ← @quest 파일
├── Trigger/     ← @trigger 파일
├── Wave/        ← @wave 파일
└── Cutscene/    ← @cutscene 파일
```

파일 확장자는 반드시 `.yiso`여야 합니다.
파일 인코딩은 **UTF-8**을 사용합니다.

---

### 들여쓰기

스페이스와 탭 모두 사용할 수 있습니다.
단, **한 파일 안에서 스페이스와 탭을 혼용할 수 없습니다.**
들여쓰기 깊이는 앞에 붙은 문자 수로 계산하므로, 같은 레벨은 항상 같은 수의 문자여야 합니다.

```
@dialogue my_dialogue       ← 블록 선언 (들여쓰기 없음)
  [화자]: 텍스트             ← 블록 내용 (스페이스 2칸 또는 탭 1개)
  ? 조건                    ← 분기 (같은 레벨)
    [화자]: 텍스트           ← 분기 내용 (한 단계 더 들여쓰기)
    END
```

들여쓰기가 맞지 않으면 파싱 에러가 발생합니다.

---

### 주석

`//` 뒤의 내용은 무시됩니다. 줄 중간에도 사용 가능합니다.

```
// 이 대화는 1장 마을에서 사용됩니다
@dialogue village_elder_intro

  [마을장로]: 어서오게.   // 첫 인사
```

---

### ID 작성 규칙

블록 ID와 퀘스트 ID 등은 영문 소문자와 언더스코어(`_`)만 사용합니다.

```
✅ village_elder_intro
✅ chapter1_main_quest
❌ 마을장로인트로
❌ VillageElderIntro
❌ village-elder-intro
```

---

### 하나의 파일에 여러 블록

한 파일에 여러 블록을 작성할 수 있습니다. 타입이 달라도 됩니다.

```
@dialogue npc_guard_hello
  [경비병]: 멈춰라!

@dialogue npc_guard_bye
  [경비병]: 통과해도 좋다.

@trigger gate_enter
  EVENT("OpenGate")
```

---

## 2. 블록 구조

모든 블록은 동일한 기본 형태를 따릅니다.

```
@<타입> <ID>
  <내용>
  <내용>
  ...
```

| 요소 | 설명 |
|---|---|
| `@<타입>` | `@dialogue` `@quest` `@trigger` `@wave` `@cutscene` 중 하나 |
| `<ID>` | 블록 고유 식별자. 게임 시스템이 이 ID로 블록을 찾아 실행합니다 |
| `<내용>` | 한 단계 들여쓰기 후 작성 (스페이스 또는 탭, 파일 내 통일) |

---

## 3. @dialogue — 대화 스크립트

NPC와의 대화를 정의합니다.
InteractionSystem이 NPC 상호작용 시 이 블록을 실행합니다.

### 사용 가능한 요소

| 요소 | 설명 |
|---|---|
| `[화자]: 텍스트` | 대화 한 줄 |
| `? 조건` | 조건 분기 시작 |
| `? default` | 조건이 모두 해당 없을 때의 기본 분기 |
| `END` | 현재 분기 케이스 종료 |
| `SET flag.키 = 값` | 로컬 플래그 설정 |
| `GIVE_QUEST("퀘스트ID")` | 퀘스트 수주 |
| `UNLOCK_QUEST("퀘스트ID")` | 퀘스트 해금 |

### 대화 라인

```
[화자이름]: 대화 내용을 여기에 씁니다.
```

- 화자 이름은 대괄호 `[ ]`로 감쌉니다
- `:` 콜론 뒤에 한 칸 띄고 텍스트를 작성합니다
- 텍스트는 한 줄에 하나씩 작성합니다 (줄바꿈 없음)

### 분기 (`?`)

조건에 따라 다른 대화를 보여줄 때 사용합니다.
`?` 블록이 여러 개 연속되면 위에서부터 순서대로 조건을 확인하고, **처음 일치하는 케이스만 실행**합니다.
각 케이스는 반드시 `END`로 닫아야 합니다.

```
  ? 조건1
    [화자]: 조건1이 맞을 때 대화
    END

  ? 조건2
    [화자]: 조건2가 맞을 때 대화
    END

  ? default
    [화자]: 아무 조건도 해당 없을 때 대화
    END
```

> **주의:** `? default`는 반드시 가장 마지막에 작성해야 합니다.

### 플래그 (`SET`)

대화 중에 플래그 값을 저장합니다. 이 값은 **현재 스크립트 실행 범위 안에서만** 유효합니다.

```
SET flag.elder_met = true
SET flag.quest_count = 3
SET flag.player_name = "용사"
```

플래그 값 종류: `true` / `false` / 숫자 / `"문자열"`

### 전체 예시

```
// 마을 장로 첫 만남 대화
@dialogue village_elder_intro

  [마을장로]: 어서오게, 젊은이.

  // 이미 퀘스트를 받은 경우
  ? HasQuest("village_defense")
    [마을장로]: 아직 고블린들을 처치하지 못했나?
    [마을장로]: 서두르게. 마을이 위험하네.
    END

  // 장로를 이미 만난 적 있는 경우 (로컬 플래그)
  ? flag.elder_met
    [마을장로]: 또 왔군. 무슨 일인가?
    END

  // 처음 만나는 경우 (기본)
  ? default
    [마을장로]: 처음 보는 얼굴이군. 우리 마을에 위기가 닥쳤다네.
    [마을장로]: 고블린들이 마을 외곽을 습격하고 있어. 부탁할 수 있겠나?
    SET flag.elder_met = true
    GIVE_QUEST("village_defense")
    END
```

---

## 4. @quest — 퀘스트 스크립트

퀘스트 데이터를 정의합니다.
QuestSystem이 이 블록을 읽어 퀘스트를 등록합니다.

### 사용 가능한 요소

| 요소 | 설명 |
|---|---|
| `TITLE "텍스트"` | 퀘스트 표시 이름 |
| `DESC "텍스트"` | 퀘스트 설명 |
| `OBJECTIVE kill("ID") count(n) label("텍스트")` | 처치 목표 |
| `OBJECTIVE talk("NPC_ID") label("텍스트")` | 대화 목표 |
| `OBJECTIVE reach("구역_ID") label("텍스트")` | 도달 목표 |
| `ON_START` | 퀘스트 시작 시 실행할 커맨드 블록 |
| `ON_COMPLETE` | 퀘스트 완료 시 실행할 커맨드 블록 |
| `REWARD exp(n) gold(n)` | 완료 보상 |
| `UNLOCK_QUEST("ID")` | 완료 시 해금할 다음 퀘스트 |

### OBJECTIVE 상세

```
OBJECTIVE kill("goblin")   count(10) label("고블린 처치")
           ↑ 목표 타입      ↑ 목표수  ↑ UI에 표시되는 이름
```

- `kill`: 특정 적 처치. 괄호 안에 적 ID
- `talk`: 특정 NPC와 대화. 괄호 안에 NPC ID
- `reach`: 특정 구역 도달. 괄호 안에 구역 ID
- `count`는 `kill`에서만 유효합니다. `talk`와 `reach`는 count 생략 (기본값 1)
- `label`은 퀘스트 추적 UI에 표시됩니다

### ON_START / ON_COMPLETE

퀘스트 시작/완료 시점에 자동 실행되는 커맨드를 정의합니다.
들여쓰기를 한 단계 더 들어가 작성합니다.

```
ON_START
  EVENT("SpawnGoblins")
  SOUND.PlayBgm("BgmBattle1")
```

사용 가능한 커맨드는 [8. 커맨드 전체 목록](#8-커맨드-전체-목록)을 참고하세요.

### 전체 예시

```
// 마을 방어 퀘스트
@quest village_defense

  TITLE "마을을 지켜라"
  DESC  "고블린들로부터 마을 주민을 보호하라."

  OBJECTIVE kill("goblin")        count(10) label("고블린 처치")
  OBJECTIVE talk("village_elder")           label("장로에게 보고")

  ON_START
    EVENT("SpawnGoblins")
    SOUND.PlayBgm("BgmBattle1")

  ON_COMPLETE
    REWARD exp(500) gold(200)
    UNLOCK_QUEST("chapter1_main")
    SOUND.StopBgm()
```

---

## 5. @trigger — 트리거 스크립트

특정 구역에 진입하거나 조건이 충족될 때 자동 실행되는 커맨드 시퀀스입니다.
TriggerSystem이 감지 후 이 블록을 실행합니다.

### 특징

- 대화 라인(`[화자]:`)을 직접 작성할 수 없습니다. 대화가 필요하면 `CUTSCENE`을 통해 연결합니다.
- `?` 분기를 사용할 수 없습니다.
- 순서대로 커맨드가 실행됩니다.

### 사용 가능한 커맨드

커맨드 전체 목록에서 `CAMERA`, `SOUND`, `INPUT`, `WAIT`, `EVENT`, `CUTSCENE`, `GIVE_QUEST`, `UNLOCK_QUEST`, `SET` 사용 가능합니다.

### 전체 예시

```
// 보스방 진입 트리거
@trigger boss_room_enter

  CAMERA.ZoomTo(4.0, 0.5)
  SOUND.StopBgm()
  WAIT(0.5)
  CUTSCENE("boss_intro")    // INPUT.Disable/Enable은 컷씬 안에서 처리
  EVENT("LockDoor")
  SOUND.PlayBgm("BgmBoss1")
```

---

## 6. @wave — 웨이브 스크립트

보스방 또는 특정 구역의 몬스터 소환 패턴을 정의합니다.
SpawnSystem이 이 블록을 읽어 웨이브를 실행합니다.

### 구조

```
@wave <ID>

  WAVE(1)
    SPAWN "몬스터ID" count(수량) interval(간격)

  WAVE(2)
    SPAWN "몬스터ID" count(수량)
    SPAWN "몬스터ID" count(수량)
```

### WAVE(n)

- `n`은 웨이브 번호 (1부터 시작)
- 이전 웨이브의 몬스터를 모두 처치해야 다음 웨이브가 시작됩니다

### SPAWN 상세

```
SPAWN "goblin" count(5) interval(1.0)
       ↑ 몬스터ID ↑ 마리수  ↑ 스폰 간격(초)
```

- `"몬스터ID"`: 소환할 몬스터 ID (큰따옴표 필수)
- `count(n)`: 소환 마리수. 생략하면 1마리
- `interval(초)`: 한 마리씩 스폰하는 간격(초). 생략하면 동시 스폰

### 전체 예시

```
// 보스방 1 웨이브 패턴
@wave boss_room_1

  // 1웨이브: 고블린 5마리를 1초 간격으로 순차 소환
  WAVE(1)
    SPAWN "goblin" count(5) interval(1.0)

  // 2웨이브: 오크와 아처 동시 소환
  WAVE(2)
    SPAWN "orc"    count(3)
    SPAWN "archer" count(2)

  // 3웨이브: 보스 등장
  WAVE(3)
    SPAWN "goblin_chief" count(1)
```

---

## 7. @cutscene — 컷씬 스크립트

카메라, 사운드, 대화, 이벤트를 순서대로 연출하는 시퀀스입니다.
모든 명령은 **위에서 아래로 순서대로** 실행되며, 각 단계가 완료된 후 다음 단계로 넘어갑니다.

### 특징

- `[화자]:` 대화 라인과 커맨드를 자유롭게 섞어 쓸 수 있습니다
- 플레이어는 대화 라인에서 버튼을 눌러야 다음으로 넘어갑니다
- `CAMERA.MoveTo`, `CAMERA.Shake`는 해당 동작이 완료될 때까지 대기합니다
- `CUTSCENE` 커맨드로 다른 컷씬을 **재귀 호출할 수 없습니다** (무한 루프 방지)

### 사용 가능한 요소

| 요소 | 설명 |
|---|---|
| `[화자]: 텍스트` | 대화 라인 (플레이어 입력 대기) |
| `CAMERA.*` | 카메라 연출 |
| `SOUND.*` | 사운드 재생/정지 |
| `INPUT.Enable/Disable()` | 플레이어 입력 차단/허용 |
| `WAIT(초)` | 지정 시간 대기 |
| `EVENT("ID")` | 이벤트 발행 |
| `SET flag.키 = 값` | 플래그 설정 |

### 전체 예시

```
// 보스 등장 컷씬
@cutscene boss_intro

  INPUT.Disable()
  CAMERA.MoveTo(12.5, -3.0, 1.5)    // x, y, 이동시간(초)
  WAIT(0.5)
  [보스]: 드디어 왔군... 기다리고 있었다.
  CAMERA.Shake(0.4, 1.2, 0.5)       // 진폭, 주파수, 지속시간(초)
  SOUND.PlayBgm("BgmBoss1")
  WAIT(0.3)
  [주인공]: ...!
  [보스]: 네 목숨은 오늘 여기서 끝이다.
  CAMERA.Release()
  INPUT.Enable()
```

---

## 8. 커맨드 전체 목록

### WAIT

지정한 시간만큼 대기합니다.

```
WAIT(초)

WAIT(1.0)    // 1초 대기
WAIT(0.5)    // 0.5초 대기
```

---

### CAMERA 커맨드

```
CAMERA.MoveTo(x, y, 이동시간)
```
카메라를 지정 좌표로 이동합니다. 이동이 완료될 때까지 스크립트가 대기합니다.

```
CAMERA.MoveTo(12.5, -3.0, 1.5)   // x=12.5, y=-3.0으로 1.5초에 걸쳐 이동
```

---

```
CAMERA.Shake(진폭, 주파수, 지속시간)
```
카메라를 흔듭니다. 지속시간이 끝날 때까지 스크립트가 대기합니다.

```
CAMERA.Shake(0.4, 1.2, 0.5)   // 진폭 0.4, 주파수 1.2, 0.5초 동안
```

---

```
CAMERA.ZoomTo(크기, 속도)
```
카메라 줌을 변경합니다. 실행 후 즉시 다음으로 넘어갑니다.

```
CAMERA.ZoomTo(4.0, 0.5)   // orthographic size 4.0으로, 속도 0.5
```

---

```
CAMERA.Release()
```
카메라를 플레이어 추적 상태로 되돌립니다.

---

### SOUND 커맨드

```
SOUND.PlayBgm("사운드ID")    // BGM 재생
SOUND.StopBgm()              // BGM 정지
SOUND.PlaySfx("사운드ID")    // 효과음 재생
SOUND.StopAll()              // 모든 사운드 정지
```

사운드 ID는 개발팀에게 문의하거나 사운드 ID 목록 문서를 참고하세요.

```
SOUND.PlayBgm("BgmBattle1")
SOUND.PlaySfx("SfxExplosion")
```

---

### INPUT 커맨드

```
INPUT.Disable()   // 플레이어 입력 차단 (컷씬, 연출 중 사용)
INPUT.Enable()    // 플레이어 입력 허용 (연출 종료 후 반드시 호출)
```

> **주의:** `INPUT.Disable()` 후 반드시 `INPUT.Enable()`을 호출해야 합니다. 빠뜨리면 플레이어가 아무것도 할 수 없는 상태가 됩니다.

---

### EVENT 커맨드

```
EVENT("이벤트ID")
```

게임 내 다른 시스템에 신호를 보냅니다. 사용 가능한 이벤트 ID는 개발팀에게 문의하세요.

```
EVENT("SpawnGoblins")
EVENT("LockDoor")
EVENT("UnlockChest")
```

---

### GIVE_QUEST / UNLOCK_QUEST

```
GIVE_QUEST("퀘스트ID")      // 플레이어에게 퀘스트 수주
UNLOCK_QUEST("퀘스트ID")    // 퀘스트를 해금 (수주 가능 상태로 변경)
```

---

### SET (플래그)

현재 스크립트 실행 범위 안에서 값을 저장합니다.

```
SET flag.키이름 = 값

SET flag.elder_met = true
SET flag.elder_met = false
SET flag.visit_count = 3
```

---

### REWARD

퀘스트 보상을 지급합니다. `ON_COMPLETE` 블록 안에서 사용합니다.

```
REWARD exp(경험치) gold(골드)

REWARD exp(500) gold(200)
REWARD exp(1000)              // gold 생략 시 0
REWARD gold(500)              // exp 생략 시 0
```

---

### CUTSCENE

다른 컷씬 블록을 현재 위치에서 실행합니다. `@trigger` 블록에서만 사용 가능합니다.

```
CUTSCENE("컷씬블록ID")

CUTSCENE("boss_intro")
```

---

## 9. 분기 조건 전체 목록

`@dialogue` 블록의 `?` 뒤에 사용합니다.

| 조건 | 설명 | 예시 |
|---|---|---|
| `HasQuest("ID")` | 플레이어가 해당 퀘스트를 보유 중인지 | `? HasQuest("village_defense")` |
| `HasFlag("키")` | 영속 저장된 플래그 값이 true인지 | `? HasFlag("elder_met")` |
| `HasItem("ID")` | 플레이어 인벤토리에 아이템이 있는지 | `? HasItem("letter_item")` |
| `flag.키` | 현재 스크립트에서 SET한 로컬 플래그 | `? flag.elder_met` |
| `default` | 위 조건이 모두 false일 때 (기본 분기) | `? default` |

### HasFlag vs flag.키 차이

```
HasFlag("elder_met")  → 게임 저장 데이터에서 읽음 (세이브 파일, 영속)
flag.elder_met        → 현재 스크립트 실행 중 SET한 임시 값 (실행 종료 시 소멸)
```

---

## 10. 자주 하는 실수

### ❌ `END` 누락

분기(`?`) 케이스 안에서 `END`를 빠뜨리면 다음 케이스로 넘어가지 않고 에러가 발생합니다.

```
// 잘못된 예
? HasQuest("village_defense")
  [마을장로]: 아직 처치하지 못했나?
  // END 없음 ← 에러!

? default
  [마을장로]: 처음이군.
  END
```

```
// 올바른 예
? HasQuest("village_defense")
  [마을장로]: 아직 처치하지 못했나?
  END            ← 반드시 필요

? default
  [마을장로]: 처음이군.
  END
```

---

### ❌ `? default`를 중간에 작성

`? default`는 반드시 분기 목록의 **맨 마지막**에 작성해야 합니다. 중간에 있으면 그 아래 조건들이 절대 실행되지 않습니다.

```
// 잘못된 예
? default             ← default가 먼저 오면
  [장로]: 처음이군.
  END

? HasQuest("quest1")  ← 이 조건은 절대 도달하지 않음
  [장로]: 퀘스트 중이군.
  END
```

---

### ❌ SOUND.Play 사용

`SOUND.Play` 는 존재하지 않습니다. 반드시 `SOUND.PlayBgm` 또는 `SOUND.PlaySfx`를 사용해야 합니다.

```
// 잘못된 예
SOUND.Play("boss_bgm")       ← 에러

// 올바른 예
SOUND.PlayBgm("BgmBoss1")    ← BGM
SOUND.PlaySfx("SfxExplosion") ← 효과음
```

---

### ❌ CAMERA.MoveTo 인자 2개

`CAMERA.MoveTo`는 좌표(x, y)와 이동 시간까지 **3개의 인자**가 필요합니다.

```
// 잘못된 예
CAMERA.MoveTo(boss_room, 1.5)         ← 에러 (위치 이름 사용 불가, 인자 2개)

// 올바른 예
CAMERA.MoveTo(12.5, -3.0, 1.5)        ← x, y, 이동시간
```

좌표값은 개발팀에게 문의하거나 Unity 씬에서 직접 확인하세요.

---

### ❌ INPUT.Disable() 후 Enable() 빠뜨림

연출 중 입력을 차단했다면 연출 종료 후 반드시 되돌려야 합니다.

```
// 잘못된 예
@cutscene my_scene
  INPUT.Disable()
  [보스]: 드디어 왔군.
  // INPUT.Enable() 없음 ← 플레이어 조작 불가 상태 지속!

// 올바른 예
@cutscene my_scene
  INPUT.Disable()
  [보스]: 드디어 왔군.
  INPUT.Enable()    ← 반드시 마지막에 호출
```

---

### ❌ @cutscene 안에서 CUTSCENE 호출

컷씬 안에서 다른 컷씬을 호출하는 것은 허용되지 않습니다.
`CUTSCENE("ID")` 는 `@trigger` 블록에서만 사용하세요.

```
// 잘못된 예
@cutscene scene_a
  [화자]: 안녕.
  CUTSCENE("scene_b")   ← 에러

// 올바른 예
@trigger some_trigger
  CUTSCENE("scene_a")   ← 트리거에서 컷씬 호출
```

---

### ❌ 들여쓰기 혼용

스페이스와 탭을 한 파일 안에서 섞어 쓰면 에러가 발생합니다.
스페이스로 통일하거나 탭으로 통일해서 사용하세요.

```
// 잘못된 예 (스페이스와 탭 혼용)
@dialogue foo
  [화자]: 안녕       ← 스페이스 2칸
	END              ← 탭 1개 (문자 수 불일치 → 에러)

// 올바른 예 (스페이스로 통일)
@dialogue foo
  [화자]: 안녕
    END

// 올바른 예 (탭으로 통일)
@dialogue foo
	[화자]: 안녕
		END
```

---

## 빠른 참고표

### 블록 타입별 허용 요소

| | `@dialogue` | `@quest` | `@trigger` | `@wave` | `@cutscene` |
|---|:---:|:---:|:---:|:---:|:---:|
| `[화자]: 텍스트` | ✅ | ❌ | ❌ | ❌ | ✅ |
| `? 분기` | ✅ | ❌ | ❌ | ❌ | ❌ |
| `CAMERA.*` | ❌ | ❌ | ✅ | ❌ | ✅ |
| `SOUND.*` | ❌ | ❌ | ✅ | ❌ | ✅ |
| `INPUT.*` | ❌ | ❌ | ✅ | ❌ | ✅ |
| `WAIT` | ❌ | ❌ | ✅ | ❌ | ✅ |
| `EVENT` | ❌ | ✅(훅 안) | ✅ | ❌ | ✅ |
| `SET flag` | ✅ | ❌ | ✅ | ❌ | ✅ |
| `GIVE_QUEST` | ✅ | ❌ | ✅ | ❌ | ❌ |
| `UNLOCK_QUEST` | ✅ | ✅(훅 안) | ✅ | ❌ | ❌ |
| `CUTSCENE` | ❌ | ❌ | ✅ | ❌ | ❌ |
| `REWARD` | ❌ | ✅(ON_COMPLETE 안) | ❌ | ❌ | ❌ |
| `WAVE / SPAWN` | ❌ | ❌ | ❌ | ✅ | ❌ |
