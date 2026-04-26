# GameScene UI/UX 개선 계획

> 마지막 업데이트: 2026-04-26
> 상태: draft
> doc_id: plans.game-scene-ui-ux-improvement
> role: plan
> owner_scope: GameScene 전투 UI/UX 재설계 방향과 실행 순서
> upstream: plans.progress, design.game-design, ops.unity-ui-authoring-workflow
> artifacts: `Assets/Scenes/GameScene.unity`, `Assets/Prefabs/`
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 `GameScene` 전투 화면의 UI/UX 재설계 방향과 작업 순서를 유지하는 draft 계획 문서다.
제품 판단 SSOT는 [`../design/game_design.md`](../design/game_design.md), 실제 진행 상태 SSOT는 [`progress.md`](./progress.md)를 따른다.

현재 기준으로 `GameScene` 전투 UI는 hand-authored scene/prefab 기반으로 복구된 상태이며, 다음 단계는 "동작은 한다"에서 "읽기 쉽고 지휘 판타지가 보이는 화면"으로 올리는 것이다.

## Draft Triage

- 판정: draft 유지.
- 이유: 현재 직접 실행은 Agent B HUD/input validation plan이 맡고, 이 문서는 UX 방향과 다음 redesign 기준을 보존한다.
- active 전환 조건: Agent B closeout 뒤 GameScene HUD/소환 UX redesign을 직접 실행할 때 active로 올린다.
- reference 전환 조건: UX 방향이 Agent B plan, design owner, 또는 구현 결과로 흡수되어 더 이상 실행 순서를 소유하지 않으면 reference로 내린다.

---

## 핵심 결정

| 항목 | 현재 기준 |
|---|---|
| 플랫폼 기준 | 모바일 세로 우선 |
| 시각 방향 | 전술 SF |
| 최우선 개선 영역 | 소환/배치 UX |
| 입력 모델 | 하이브리드 (`탭 기본 + 드래그 고급 입력`) |
| 정보 노출 | 내 플레이 + 공용 코어 중심, 팀원 정보 최소 |
| UI 카피 | 아이콘/숫자 우선, 한국어 보조 |
| 목표 인상 | 아바타 액션 HUD가 아니라 현장 지휘 화면 |

---

## 현재 진단

- 현재 `GameScene` HUD는 기능 연결 관점에서는 복구됐지만, 시각 계층과 상호작용 언어는 아직 프로토타입 인상이 강하다.
- 상단 `Wave / Countdown / Status`, 하단 `UnitSummonUi`, 중앙 `PlacementErrorView`, 종료 `WaveEndOverlay`가 각각 따로 존재할 뿐, 하나의 전술 HUD 문법으로 읽히지 않는다.
- 모바일 우선 게임 방향 대비 현재 소환 슬롯과 배치 피드백은 "무엇을 언제 어디에 내는가"를 즉시 이해시키는 수준까지 올라오지 않았다.
- 드래그 기반 배치는 구현 경로가 있지만, 현재 smoke 기준 자동화 안정성이 낮아 탭 기반 기본 경로를 먼저 읽히게 만드는 편이 안전하다.
- 결과 화면은 기능상 `Victory!/Defeat!`까지 닫히지만, 통계 카드 구조와 CTA 우선순위는 아직 전투 종료 경험으로 다듬을 여지가 크다.

---

## 재설계 방향

### 1. 전장 HUD를 "목표 + 자원 + 명령" 구조로 재정렬

- 상단 좌측: `Wave`, `Countdown`, 짧은 상태 배너만 유지
- 상단 우측: `Core HP`를 팀 목표 카드처럼 크게 고정
- 하단 중앙: `소환 덱 바`를 메인 command 영역으로 재구성
- 팀원 정보는 큰 패널로 띄우지 않고, 필요 시 작은 상태 칩 정도로만 유지

### 2. 소환/배치를 GameScene의 얼굴로 끌어올리기

- 슬롯 탭 시 `선택 상태`에 들어가고, 배치 가능 구역이 전장에 명확히 강조되어야 한다
- 선택 상태에서 배치 구역 탭으로 소환하는 흐름을 기본 경로로 둔다
- 드래그는 유지하되 고급 입력으로 취급하고, 고스트/유효/무효 피드백을 분명히 한다
- 에너지 부족, 배치 실패, 선택 취소는 모두 같은 command 영역 문법 안에서 읽히게 정리한다

### 3. 텍스트를 줄이고 숫자/아이콘 중심으로 압축

- 전투 중에는 긴 설명보다 `아이콘 + 큰 숫자 + 짧은 보조 문구`를 우선한다
- 첫 진입과 첫 wave에서만 상황별 힌트를 짧게 보여준다
- `Wave Cleared`, `Need Energy`, `배치 영역 밖` 같은 상태 메시지는 각각 고유 색/위계로 구분한다

### 4. 종료 화면을 모바일 카드형 결과 경험으로 정리

- 결과 화면은 `결과 -> 핵심 수치 3~4개 -> Return To Lobby` 순으로 단순화한다
- 뒤 전장은 어둡게 유지하되 완전히 숨기지 않아 "전투가 방금 끝났다"는 감각을 살린다
- 장문 텍스트보다 큰 숫자 카드와 명확한 CTA를 우선한다

---

## 작업 묶음

### Workstream A. 전장 HUD 재배치

목표:
모바일 세로 기준에서 `Wave / Core / Energy / Summon`의 읽기 순서를 즉시 이해 가능하게 만든다.

변경 방향:
- 상단 HUD를 더 얇고 선명한 전술 바 형태로 재배치
- `Core HP`를 별도 목표 카드로 승격
- 하단 command 영역과 상단 상태 영역의 역할을 명확히 분리

완료 기준:
- 첫 5초 안에 `현재 wave`, `코어 상태`, `내가 누를 슬롯`이 한눈에 들어온다
- HUD끼리 겹치거나 시선 경합이 생기지 않는다

### Workstream B. 소환/배치 상호작용 재설계

목표:
플레이어가 "캐릭터를 조작하는 것"이 아니라 "유닛을 지휘하는 것"을 즉시 이해하게 만든다.

변경 방향:
- 슬롯 선택 상태 추가
- 탭 기반 배치 기본 경로 추가 또는 강화
- 드래그는 고급 입력으로 유지하되 피드백 정밀도 향상
- 에너지 부족/배치 실패를 command 영역 피드백으로 통합

완료 기준:
- 탭만으로도 자연스럽게 소환 가능하다
- 드래그 중 유효/무효 상태를 색과 위치로 즉시 구분할 수 있다
- 실패 메시지가 화면에 뜨기만 하는 것이 아니라 "왜 실패했는지" 입력 문맥 안에서 이해된다

### Workstream C. 상황별 힌트와 전투 피드백 정리

목표:
초반 이탈 없이 규칙을 익히게 하되, 과한 튜토리얼 감은 피한다.

변경 방향:
- 첫 wave, 에너지 부족, 배치 실패, wave clear에만 짧은 상황별 힌트 노출
- 설명 문장을 줄이고 아이콘/짧은 카피로 대체
- 상태 메시지 색과 위치를 목적별로 분리

완료 기준:
- 신규 플레이어가 1분 안에 `슬롯 선택 -> 배치 -> 소환` 루프를 이해한다
- 힌트가 과하게 남아 전투 화면을 가리지 않는다

### Workstream D. 결과 오버레이 재설계

목표:
전투 종료가 로그 확인이 아니라 감정적으로도 "한 판 끝"처럼 느껴지게 만든다.

변경 방향:
- `Victory!/Defeat!` 결과 카드를 더 단순하고 강한 계층으로 재배치
- 통계는 핵심 수치 카드 중심으로 정리
- `Return To Lobby` 버튼을 가장 분명한 후속 액션으로 둔다

완료 기준:
- 종료 순간 결과와 다음 행동이 즉시 읽힌다
- 장문 텍스트 없이도 성과를 이해할 수 있다

### Workstream E. scene / presentation seam 정리

목표:
UI 재설계가 임시 scene patch가 아니라 재생성 가능한 구조로 남게 한다.

변경 방향:
- 레이아웃은 `Assets/Scenes/GameScene.unity`와 관련 prefab을 MCP로 직접 정리
- `GameSceneRoot`는 orchestration만 유지
- 선택/배치 모드 정책은 presentation seam으로 분리
- `UnitSlotView`, `WaveHudView`, `WaveEndView`는 표시 책임 위주로 유지
- code-driven builder/rebuild 경로는 UI authoring 기본값으로 다시 도입하지 않는다

완료 기준:
- UI 구조 변경이 scene drift를 만들지 않는다
- 정책과 렌더링 책임이 섞이지 않는다

---

## 구현 원칙

- 실제 runtime/layout 계약은 `Assets/Scenes/GameScene.unity`와 관련 prefab의 직렬화 상태를 기준으로 본다.
- authoring 기본 경로는 Unity MCP scene/prefab repair이며, 코드 기반 scene rebuild는 사용하지 않는다.
- `GameSceneRoot`는 wiring-only entry point로 유지하고, durable UI workflow state를 먹지 않는다.
- `UnitSlotView`는 슬롯 렌더와 affordance 표시 위주로 두고, 선택/취소/배치 정책은 별도 presentation seam으로 내린다.
- 드래그/탭이 서로 다른 규칙을 가지지 않도록 같은 summon contract를 쓰게 한다.
- 이번 패스에서는 게임 규칙, cost, wave 수치, 코어 규칙은 바꾸지 않는다.

---

## 검증 기준

### 기본 검증

- `compile-clean`
- active entry scene -> GameScene summon smoke 유지
- `placement/wave/outcome smoke`를 새 HUD 기준으로 갱신

### UI/UX 검증

- 모바일 세로 기준에서 HUD가 서로 겹치지 않는다
- 탭 기반 소환이 기본 경로로 안정적으로 읽힌다
- 드래그는 유지되지만 기본 경로를 가리지 않는다
- `Wave -> Core -> Victory/Defeat` 흐름에서 새 HUD와 결과 카드가 끝까지 유지된다

### 수동 체크 포인트

- 첫 1분 안에 "나는 캐릭터를 움직이는 게 아니라 유닛을 소환하는 플레이어"라는 점이 읽히는가
- 에너지 부족과 배치 실패를 텍스트를 다 읽지 않아도 이해할 수 있는가
- 종료 순간 결과와 복귀 CTA를 2초 안에 찾을 수 있는가

---

## 다음 세션 시작점

1. Unity MCP로 `GameScene` HUD/card hierarchy를 직접 재배치
2. `소환 슬롯 선택 상태 + 탭 배치 기본 경로` 추가
3. `드래그 고급 입력` 피드백을 새 command 문법에 맞게 재정렬
4. `WaveEndOverlay`를 결과 카드형으로 재구성
5. smoke / screenshot 기준을 새 레이아웃에 맞게 갱신

---

## 관련 문서

- 게임 방향 SSOT: [`../design/game_design.md`](../design/game_design.md)
- 진행 상황 SSOT: [`progress.md`](./progress.md)
- GameScene 상위 흐름: [`game_scene_entry_plan.md`](./game_scene_entry_plan.md)
- Garage UI 계획 참고: [`garage_ui_ux_improvement_plan.md`](./garage_ui_ux_improvement_plan.md)
- Unity MCP 검증 루틴: [`../../tools/unity-mcp/README.md`](../../tools/unity-mcp/README.md)
