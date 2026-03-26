# 개발 로드맵

게임 루프를 최대한 빨리 플레이 가능한 상태로 만들고, 레이어별 의존성 순서를 지키는 것을 기준으로 한다.

---

## 개발 순서 요약

```
Phase 1   Core 프레임워크       → 씬 전환, 기본 UI
Phase 2   플레이어 이동          → 첫 플레이어블
Phase 3   Infra & SaveSystem    → 저장 구조 확정 (이후 변경 최소화)
Phase 4   전투 루프              → 공격/피해/사망
Phase 5   월드 구성              → 맵 위에서 전투
Phase 6   플레이어 진행          → 퀘스트/드랍 루프
Phase 7   스킬 & 보스            → 챕터 클리어 루프
Phase 8   경제 & 거점            → 전체 게임 루프 완성
Phase 9   연출 & 서버            → 퀄리티업
Phase 10  콘텐츠 & 밸런싱        → 출시 준비
```

---

## Phase 1 — Core 프레임워크

**목표:** 씬 전환이 되고 화면에 뭔가 보이는 상태

| 시스템 | 비고 |
|--------|------|
| BootStrapper | 초기화 순서 오케스트레이터 |
| LogSystem | 첫 줄부터 필요 |
| EventSystem | 전 시스템 통신 기반 — 최우선 |
| SceneSystem | 씬 전환 없으면 아무것도 진행 불가 |
| TimeSystem | DeltaTime, 타이머 |
| InputSystem | 플레이어 조작 기반 |
| UISystem + UIManager + HUDManager | 팝업 스택, HUD 틀 |
| CameraSystem | 기본 추적만 |
| PoolingSystem | 이후 전투에서 바로 필요 |
| ConfigSystem / SoundSystem | 간단한 기본 틀만 |

**마일스톤:** 씬 전환 → 카메라 움직임 → 기본 UI 출력

---

## Phase 2 — 플레이어 이동 (첫 플레이어블)

**목표:** 캐릭터가 화면에서 조작 가능한 상태

| 시스템 | 비고 |
|--------|------|
| EntityBase + Player | |
| PlayerSystem | Entity 생성/관리 |
| MovementModule + AnimationModule + PhysicModule | 기존 YisoCharacter 코드 통합 |

**마일스톤:** 캐릭터 조작 가능, 카메라 추적

---

## Phase 3 — Infra & 저장 구조 확정

**목표:** 로그인 → 데이터 로드/저장 흐름 완성

> **Phase 3을 앞에 두는 이유:**
> 플레이어블 루프가 커지기 전에 SaveData 스키마를 확정해야 한다.
> QuestSystem, InventorySystem 구현 전에 저장 구조가 없으면
> Phase 6~8에서 전부 뜯어야 할 가능성이 높다.

| 시스템 | 비고 |
|--------|------|
| AuthSystem | 초기엔 게스트 로그인 스텁으로 |
| SaveSystem | **이원화 저장 로직 핵심 — 여기서 SaveData 구조 확정** |
| ResourceSystem | 기본 에셋 로딩 (Addressables 고도화는 Phase 9) |

**마일스톤:** 로그인 → 게임 씬 진입 → 저장/로드 동작

---

## Phase 4 — 전투 루프

**목표:** 플레이어가 몬스터를 공격하고 몬스터가 반격하는 상태

| 시스템 | 비고 |
|--------|------|
| StatSystem | 스탯 테이블 — DamageSystem이 의존 |
| DamageSystem | EntityHealth 포함 |
| Enemy Entity | FSMModule + NavigationModule |
| CombatSystem | 어그로, 타겟팅 |
| EffectSystem | 타격 이펙트 (PoolingSystem 연동) |

**마일스톤:** 기본 전투 루프 동작 (공격 → 피해 → 사망)

---

## Phase 5 — 월드 구성

**목표:** 실제 맵 위에서 전투 가능한 상태

| 시스템 | 비고 |
|--------|------|
| MapSystem | 맵 노드 구조, Boundary → CameraSystem 연동 |
| SpawnSystem | 몬스터/NPC 스폰 |
| TriggerSystem | 보스방 진입, 구역 트리거 |
| EnvironmentSystem | 조명, 날씨 (아트 작업에 맞춰서) |
| **ScriptingSystem Core** | **Lexer / Parser / Runner / Context + EditorWindow 기본 틀** |
| **@trigger / @wave ScriptAPI** | **TriggerSystem·SpawnSystem 연동** |

**마일스톤:** 챕터 맵에서 전투 가능

---

## Phase 6 — 플레이어 진행 시스템

**목표:** 퀘스트 받고 → 사냥 → 보상 → 완료 루프 동작

| 시스템 | 비고 |
|--------|------|
| ItemSystem | 아이템 정적 데이터 테이블 |
| InventorySystem + DropSystem | 드랍 → 인벤토리 |
| InteractionSystem | NPC 대화 트리거, 루팅 |
| QuestSystem | **후퇴 시 챕터 퀘스트 롤백 로직 포함** |
| **@dialogue / @quest ScriptAPI** | **InteractionSystem·QuestSystem 연동 — 기획자 대화/퀘스트 스크립팅** |

**마일스톤:** 퀘스트 수주 → 처치 → 보상 → 저장

---

## Phase 7 — 스킬 & 보스

**목표:** 보스 처치 → 스킬 해금 루프 동작

| 시스템 | 비고 |
|--------|------|
| SkillSystem | 기존 Ability 구조 위에 구현 |
| BuffSystem | 상태 이상 |
| 보스 Enemy | 페이즈, 고유 패턴 |

**마일스톤:** 챕터 1 보스 클리어 → 스킬 획득 → 포탈 생성

---

## Phase 8 — 경제 & 거점 콘텐츠

**목표:** 중간 맵 콘텐츠 완성 (강화, 상점, 무한 도장)

| 시스템 | 비고 |
|--------|------|
| ShopSystem | NPC 상점 |
| EnhancementSystem | 골드 기반 강화 |
| AchievementSystem | 계정 단위 기록 |
| 외전 세션 | QuestSystem + SpawnSystem + ScriptingSystem(@quest) 조합 |

**마일스톤:** 중간 맵 → 강화/상점 → 챕터 재도전 루프 완성

---

## Phase 9 — 연출 & 서버

**목표:** 컷씬, 서버 연동, 리소스 최적화

| 시스템 | 비고 |
|--------|------|
| CutsceneSystem | 보스 인트로, 챕터 엔딩 |
| **@cutscene ScriptAPI** | **CutsceneSystem 연동 — 기획자 컷씬 스크립팅** |
| SoundSystem 고도화 | BGM 씬별 전환, SFX 전체 |
| NetworkSystem | 리더보드, 클라우드 동기화 |
| LocalizationSystem | 다국어 필요 시 |
| ResourceSystem 고도화 | Addressables 챕터별 분리 |
| CashShopSystem | IAP |

---

## Phase 10 — 콘텐츠 & 밸런싱

- 챕터 2~N 제작
- 스탯 성장 테이블 조정
- 드랍률 / 강화 비용 밸런싱
- 무한 도장 미션 유형 추가

---

## 핵심 게임 루프

```
[중간 맵]
 ├── 외전 → 파밍(골드/경험치) + 곁줄기 스토리 → 귀환
 ├── 대장장이 → 장비 강화 (골드 소비)
 └── 챕터 선택
          │ 스펙 체크
          ▼
     [챕터 입장]
          │
          ├── 마을 ↔ 필드 (퀘스트 수행, 사냥)
          │
          ├── [클리어 불가 → 중간 맵 후퇴]
          │        레벨/장비/골드 유지
          │        퀘스트/위치 리셋
          │
          └── [보스 처치 + 메인 퀘스트 완료]
                   스킬 해금 + 포탈 생성
                   다음 챕터 or 중간 맵
```

---

## 구현 현황

→ [IMPLEMENTED.md](IMPLEMENTED.md) 참조
