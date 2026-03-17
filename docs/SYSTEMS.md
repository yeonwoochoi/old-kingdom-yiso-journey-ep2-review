# 시스템 상세 설계

각 시스템의 역할, 책임범위, 연동 관계.

---

## Layer 2: Core 시스템

DontDestroyOnLoad. BootStrapper가 순서대로 초기화.

### LogSystem
- **역할:** 디버깅 및 유저 행동 데이터 로깅
- **책임:** 콘솔 로그, 치명적 에러 트래킹, 챕터 포기율 등 애널리틱스

### ConfigSystem
- **역할:** 게임 환경 설정 관리
- **책임:** 볼륨(BGM/SFX), 해상도, 언어, 단축키 로컬 저장 및 불러오기

### PoolingSystem
- **역할:** 오브젝트 풀 관리 (GC 병목 방지)
- **책임:** 몬스터, 투사체, 데미지 폰트, 드랍 아이템, 이펙트 풀 관리

### EventSystem
- **역할:** 글로벌 이벤트 버스 (Pub/Sub)
- **책임:** 몬스터 처치, 퀘스트 갱신, 아이템 획득, 씬 전환 등 이벤트 발행/구독
- **설계:** 시스템 간 직접 참조 없이 이벤트로만 통신

### TimeSystem
- **역할:** 게임 내 시간 제어 및 타이머
- **책임:** DeltaTime, 일시정지 시 TimeScale 제어, 무한 도장 제한 시간 타이머, 스킬 쿨타임

### InputSystem
- **역할:** 유저 입력 → 게임 명령 변환
- **책임:** 2D 탑다운 이동, 스킬 단축키, UI 클릭/터치, 귀환 주문서 숏컷

### SoundSystem
- **역할:** 오디오 재생 및 볼륨 관리
- **책임:** 씬별 BGM 전환(마을/필드/보스), SFX(타격/스킬), UI 클릭음, 오디오 풀링

### UISystem
- **역할:** 전체 UI 프레임워크
- **책임:** 팝업 스택(Z-Order 관리), UIManager / HUDManager 생성, 경고 팝업, 배경 블러
- **하위:**
  - **UIManager:** 인게임 팝업/창 열고 닫기. Dialogue UI도 여기서 처리 (별도 DialogueSystem 없음 — InteractionSystem/QuestSystem이 대화 데이터 제공, UIManager가 렌더링)
  - **HUDManager:** 체력/마나 바, 스킬 쿨타임, 미니맵, 퀘스트 목록 상시 표시

### SceneSystem
- **역할:** 씬 전환 및 로딩 관리
- **책임:** 씬 비동기 로딩, 로딩 스크린, 이전 씬 메모리 해제
- **이벤트 발행:** 씬 전환 시 `SceneTransitionEvent` 발행 → CameraSystem, SoundSystem 등이 구독

### LocalizationSystem
- **역할:** 다국어 텍스트 관리
- **책임:** 언어별 텍스트 테이블 로드, 텍스트 키 → 현재 언어 문자열 변환
- **초기화 위치:** SceneSystem 이후, CameraSystem 이전
- **StringTable 위치:** `Resources/StringTable/{locale}` (확장자 없이 `Resources.Load<TextAsset>`)
  - `StreamingAssets` 미사용 이유: Android에서 APK 내부 경로는 `File.ReadAllLines` 불가, `UnityWebRequest` 비동기 필요 → 구조 복잡도 증가
  - `Resources.Load<TextAsset>` 동기 로드로 플랫폼 무관하게 동작
- **언어 변경:** `SetLocale(LocaleType)` 호출 → 기존 테이블 클리어 후 재로드, `YisoLocaleChangeEvent` 발행

### CameraSystem
- **역할:** 씬 타입에 따른 카메라 동작 관리
- **초기화 위치:** Layer 2 마지막 (LocalizationSystem 이후)
- **책임:**
  - 플레이어 Transform 추적 (데드존, 스무딩)
  - EventSystem으로 SceneTransitionEvent 수신 → 씬 타입에 맞게 동작 전환
  - 컷씬 플로우에 따른 카메라 이동 (CutsceneSystem이 API 호출)
  - 카메라 흔들림 (Camera Shake)
  - Area Trigger 기반 Boundary 제한
  - Orthographic Size 조절 (Area Trigger → Zoom In/Out)
- **Public API:**

```csharp
void SetTarget(Transform target);               // PlayerSystem 호출
void MoveToPosition(Vector3 pos, float dur);   // CutsceneSystem 호출
void ReleaseControl();                          // 컷씬 종료 후 추적 복귀
void Shake(float intensity, float duration);    // DamageSystem/EffectSystem 호출
void ZoomTo(float orthographicSize, float dur); // TriggerSystem/Area 호출
void SetBoundary(Bounds boundary);              // MapSystem/TriggerSystem 호출
void ClearBoundary();
```

| SceneType | 기본 동작 |
|-----------|-----------|
| BaseCamp | 플레이어 추적, Boundary 없음 |
| Chapter | 플레이어 추적, 맵 경계 Boundary |
| InfiniteDojo | 플레이어 추적, 인스턴스 맵 Boundary |
| Login | 고정 위치 |

---

## Layer 3: Infra 시스템

Login Scene에서 초기화. 인증 및 데이터 기반.

### NetworkSystem
- **역할:** 서버 통신 규약 및 패킷 처리
- **책임:** 클라우드 데이터 동기화, 리더보드(무한 도장 타임) 통신, 공지 수신

### AuthSystem
- **역할:** 유저 계정 인증
- **책임:** 게스트/구글/애플/스팀 로그인, UID 발급
- **흐름:** NetworkSystem 이후 초기화. 로그인 성공 시 SaveSystem에 신호.

### SaveSystem
- **역할:** 유저 플레이 데이터 저장 및 복구. **이원화 저장 로직 핵심.**
- **책임:**
  - **일반 저장:** 위치 + 퀘스트 진행도 포함 전체 저장
  - **후퇴 저장:** 레벨/장비/골드만 저장, 챕터 퀘스트/위치 파기 후 저장
- **흐름:** AuthSystem 로그인 성공 후 유저 데이터 로드.

### ResourceSystem
- **역할:** 에셋 로드 및 메모리 관리
- **책임:** Addressables(챕터별 대형 에셋 동적 로드) + Built-in(Login/Loading Scene 에셋 번들 포함)
- **설계:** DontDestroyOnLoad. Layer 4 시스템이 전역 접근. ResourceSystem은 Layer 4 시스템을 알 필요 없음.
- **하위:**
  - **AddressableLoader:** 챕터 진입 시 맵 데이터, 보스 리소스 동적 다운로드 및 적재
  - **Built-in Loader:** 빌드에 포함된 에셋 동기/비동기 로드

---

## Layer 4: World 시스템

Game Scene에서 로드.

### MapSystem
- **역할:** 전체 맵 구조 및 네비게이션 관리
- **책임:**
  - 중심 마을과 방사형 필드 연결 노드 관리
  - 미니맵/월드맵 UI에 지형/위치 데이터 제공
  - 안전지대(마을) 판별
  - CameraSystem에 맵 Boundary 설정
- **하위 시스템 (MapSystem 초기화 후 로드):**

#### EnvironmentSystem
- 챕터 컨셉 조명 설정, 날씨(비/눈) 파티클, 환경음(새소리/바람소리)

#### SpawnSystem
- 필드 몬스터 리스폰, NPC 스폰, 보스 처치 후 포탈 생성, 무한 도장 특수 목표 스폰

#### TriggerSystem
- 보스방 진입 감지(문 닫힘), 함정, 특정 지역 도달 시 퀘스트 업데이트
- Area 기반 CameraSystem Zoom/Boundary 변경 요청

#### InteractionSystem
- NPC 대화 트리거 (대화 데이터를 UIManager에 전달하여 Dialogue UI 출력)
- 포탈 입장, 드랍 아이템 루팅

### PlayerSystem
- **역할:** 플레이어 Entity 생성 및 제어
- **책임:** Player/Enemy/NPC Entity 인스턴스 생성, 초기화, 생명주기 관리
- **CameraSystem 연동:** Player 생성 후 `CameraSystem.SetTarget(playerTransform)` 호출
- **하위:**
  - **EntityBase:** Entity 공통 베이스 (ID, Transform, HP, 상태)
  - **Component:** AnimationModule / MovementModule / PhysicModule / FSMModule / NavigationModule

#### NavigationModule
- Enemy FSM의 이동 명령에 A* 경로 탐색 제공
- TriggerSystem의 장애물 정보를 기반으로 경로 갱신

---

## Player State 시스템 (독립 시스템, 의미적 그룹)

QuestSystem, InventorySystem, AchievementSystem은 PlayerSystem의 하위가 아니라 **Layer 4의 독립 시스템**이다. 의미상 "플레이어 상태"를 관리하므로 Player State 프레임으로 묶어 표기.

### QuestSystem ⭐
- **역할:** 메인/서브/특수(무한 도장) 퀘스트 진행도 관리
- **책임:**
  - 퀘스트 수주/달성/보상 처리
  - **중간 맵 후퇴 시 해당 챕터 퀘스트 '수주 전'으로 롤백**
  - 무한 도장 세션 퀘스트 발동/종료
  - NPC 대화 시 대화 데이터(텍스트, 조건) InteractionSystem에 제공 → UIManager가 렌더링

### InventorySystem
- **역할:** 유저 소지품 관리
- **책임:** 장비 장착/해제, 귀환 주문서 등 아이템 사용, 골드 관리

### AchievementSystem
- **역할:** 계정 단위 누적 업적 관리
- **책임:** 몬스터 누적 처치, 보스 클리어 횟수 영구 기록, 업적 달성 보상

---

## Character State 시스템 (독립 시스템, 의미적 그룹)

### SkillSystem ⭐
- **역할:** 스킬 획득, 장착, 실행 관리
- **책임:** 보스 처치 시 고유 스킬 해금, 장착 스킬 Ability 실행, 쿨타임/자원 관리

### BuffSystem
- **역할:** 일시적 상태 이상 및 버프 관리
- **책임:** 출혈, 기절, 슬로우 디버프, 무한 도장 일시 능력치 버프

### CombatSystem
- **역할:** 전투 흐름 및 타겟팅 제어
- **책임:** 공격자/피격자 유효 판정, 적 어그로 목록 관리

### StatSystem ⭐
- **역할:** 캐릭터/몬스터 능력치 관리
- **책임:** 경험치 누적, 레벨업, 레벨업 테이블 기반 스탯 자동 상승, 장비 강화 포함 최종 수치 합산

### EffectSystem
- **역할:** 전투 시각 효과
- **책임:** 타격/스킬 이펙트, 보스 스킬 전조(장판) 마커 렌더링

### DamageSystem
- **역할:** 최종 데미지 산출 및 적용
- **책임:** 공격력/방어력/크리티컬 확률 → 데미지 계산, HP 차감, 데미지 텍스트 팝업 요청, 사망 처리

---

## Economy 시스템 (독립 시스템, 의미적 그룹)

### ItemSystem
- **역할:** 게임 내 아이템 정적 데이터 관리
- **책임:** 아이템 ID/종류/능력치/아이콘/설명 정적 테이블 제공

### DropSystem
- **역할:** 전리품 생성 규칙
- **책임:** 확률 테이블 기반 드랍. **무한 도장 진행 시 드랍률/골드량 배율(Multiplier) 적용**

### ShopSystem
- **역할:** NPC 상점 거래
- **책임:** 목록 출력, 구매/판매 시 골드 처리, InventorySystem 연동

### EnhancementSystem ⭐
- **역할:** 대장장이 장비 강화
- **책임:** 골드 기반 강화 비용 산출(복잡한 재료 없음), 성공률 계산, 성공 시 스탯 상승

### CashShopSystem
- **역할:** 인앱 결제(IAP) 관리
- **책임:** 영구 버프, 코스튬, 편의 아이템 결제 검증 및 지급

---

## ScriptingSystem

### ScriptingSystem
- **역할:** `.yiso` 스크립트 파일을 런타임에 파싱·실행하는 콘텐츠 스크립팅 엔진
- **초기화 위치:** Layer 4, MapSystem 이후 (Phase 5에서 Core 구축)
- **책임:**
  - `.yiso` 파일 Lexing → Parsing → AST 생성
  - Coroutine 기반 블록 실행 (순서 보장, `WAIT` 지원)
  - 런타임 변수·플래그 상태 유지 (`ScriptContext`)
  - 각 게임 시스템 호출 브릿지 (`IScriptAPI`) 제공
- **스크립팅 대상:**

| 블록 타입 | 연동 시스템 | 도입 시점 |
|-----------|------------|---------|
| `@trigger` | TriggerSystem | Phase 5 |
| `@wave` | SpawnSystem | Phase 5 |
| `@dialogue` | InteractionSystem → UIManager | Phase 6 |
| `@quest` | QuestSystem | Phase 6 |
| `@cutscene` | CutsceneSystem | Phase 9 |

- **스크립팅 비대상:** StatSystem, DamageSystem, SaveSystem 등 — 수치 정밀도·신뢰성이 필요한 엔진 레벨 시스템
- **EditorWindow:** Unity 에디터 전용 툴. 문법 하이라이팅, 실시간 파싱 검증, 플레이모드 블록 테스트, ID 자동완성 제공.
- **참고:** → [SCRIPTING.md](SCRIPTING.md)

---

## Cutscene 시스템

### CutsceneSystem
- **역할:** 연출 및 시네마틱 재생
- **책임:** 보스 조우 인트로, 챕터 엔딩 연출, 컷씬 재생 중 InputSystem 차단
- **카메라 제어:** CameraSystem Public API(`MoveToPosition`, `ZoomTo`, `ReleaseControl`) 직접 호출

---

## ⭐ 기획 핵심 시스템

| 시스템 | 핵심 이유 |
|--------|-----------|
| StatSystem | 레벨업 시 스탯 자동 상승 테이블 — 성장 밸런스 근간 |
| SkillSystem | 보스 처치 → 스킬 해금 — 챕터 클리어 주요 보상 |
| QuestSystem | 챕터 진행 + 후퇴 시 롤백 — 저장 이원화 실행자 |
| EnhancementSystem | 골드만으로 강화 — 경제 시스템 핵심 골드 소비처 |
| SaveSystem | 이원화 저장 — 게임 루프 핵심 규칙 |
| CameraSystem | 모든 시각 연출의 기반 — 컷씬/전투/이동 연동 |
