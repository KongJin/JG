# Garage UI/UX 장기 개선 계획

> 생성일: 2026-04-13
> 상태: 초안
> 근거: 2026-04-13 플레이모드 스크린샷 기반 평가

## 현재 상태

### 스크린샷 캡처 결과 (2026-04-13)

**로그인 화면:**
- "CODEX LOBBY" 타이틀 + "Photon room flow for room creation, ready checks, and scene handoff." 설명
- Lobby/Garage 탭 버튼 (Lobby 활성화)
- Garage 탭 클릭 전: GaragePageRoot inactive

**Garage 화면 (Garage 탭 클릭 후):**
- 3패널 레이아웃: 슬롯 목록(좌) → 에디터(중) → 프리뷰/저장(우)
- Slot 1: "Striker | Scatter Cannon" (활성, 파란색)
- Slot 2: "Empty"
- Slot 3: "Relay | Pulse Rifle"
- Slot 4~6: "Empty"
- 에디터: Striker(파랑) / Scatter Cannon(주황) / Burst Thrusters(초록) 선택
- 우측: "Roster incomplete: 2/6 saved units" + 통계 텍스트
- Google 버튼 (우측 패널 상단)
- "anonymous" / "Google sign-in ready" (우측 상단)

### 종합 평점: 7.2 / 10

| 항목 | 평점 | 비고 |
|---|---|---|
| 정보 구조 | 7/10 | 3패널 흐름은 좋으나 우측 패널 정보 중복 |
| 시각 계층 | 6/10 | 텍스트 대비도 부족, 저장 버튼 불명확 |
| 상호작용 | 7/10 | 부품 선택 직관적, 저장 흐름 불명확 |
| 일관성 | 8/10 | 부품 카드 패턴 일관, 버튼 스타일 불통일 |
| 피드백 | 6/10 | 저장 성공/실패 피드백 약함 |
| 레이아웃/여백 | 7/10 | 우측 패널 텍스트 조밀, 계정 정보 고립 |

---

## 개선 원칙

1. **1인 개발 우선순위** — 가장 영향도 높은 것부터, 난이도 낮은 것부터
2. **WebGL 성능 유지** — UI 개선이 빌드 사이즈/성능에 영향 주지 않도록
3. **MVP 검증 차단 요소 우선** — 플레이테스트 참여자가 혼란을 느끼는 부분 먼저
4. **디자인 시스템 구축** — 일회성 수정이 아니라 재사용 가능한 컴포넌트로

---

## Phase 1: 치명적 문제 수정 (즉시)

**목표:** 플레이테스트 참여자가 "왜 안 되지?"라고 묻는 문제 제거

### 1.1 우측 패널 텍스트 겹침 수정

**문제:** 스크린샷에서 우측 패널 텍스트가 여러 레이어에 걸쳐 겹쳐 보임. "Saved. Slot com..."와 통계 텍스트가 서로 가림.

**원인 추정:** `GarageResultPanelView`에서 여러 `TMP_Text`가 같은 RectTransform 영역에 배치되었거나, VerticalLayoutGroup이 제대로 작동하지 않음.

**작업:**
- [ ] `GarageResultPanelView`의 hierarchy 확인
- [ ] 각 텍스트 요소가 독립된 RectTransform을 갖도록 분리
- [ ] VerticalLayoutGroup + ContentSizeFitter로 자동 정렬
- [ ] 텍스트 간 간격(spacing) 8~12px 확보

**예상 효과:** 가독성 급상승, 정보 전달 명확화

---

### 1.2 저장 버튼 명확히 배치

**문제:** 스크린에서 저장 버튼이 확인되지 않음. 사용자가 "어디에 저장하지?"라고 혼란.

**작업:**
- [ ] "Save Roster" 버튼을 우측 패널 하단에 명확히 배치
- [ ] 버튼 색상: 파란색 계열 (primary action)
- [ ] 버튼 텍스트: "Save Roster" 또는 "저장"
- [ ] 버튼 크기: Clear Slot 버튼과 동일 또는 약간 더 크게
- [ ] 저장 버튼 위에 저장 상태 텍스트 배치 ("Roster incomplete: 2/6" 아래)

**레이아웃 제안:**
```
┌─────────────────────────┐
│      [Google] 버튼       │  ← 계정 액션
├─────────────────────────┤
│   Roster: 2/6 완료       │  ← 진행 상태
│   3개 이상 저장 시 Ready │  ← 안내
├─────────────────────────┤
│   [ 3D 프리뷰 영역 ]     │  ← 시각 확인
├─────────────────────────┤
│  HP 640 | DMG 32 | ...  │  ← 통계
├─────────────────────────┤
│  [  Save Roster  ]      │  ← 주요 액션 (하단 고정)
└─────────────────────────┘
```

**예상 효과:** 저장 플로우 명확화, 플레이테스트 혼란 감소

---

### 1.3 설명 텍스트 대비도 향상

**문제:** "Saved loadout. Adjust selectors to overwrite this slot automatically." 같은 설명 텍스트가 배경과 구분 어려움.

**작업:**
- [ ] 설명 텍스트 색상: 현재 `#8B95A8` 정도 → `#A8B4C8` 정도로 밝게
- [ ] 또는 배경 패널 색상을 약간 어둡게 조정
- [ ] 폰트 크기 유지, 자간(word-spacing) 약간 증가

**예상 효과:** 읽기 쉬움, 정보 전달력 향상

---

## Phase 2: 상호작용 개선 (1~2주)

**목표:** 사용자가 "어떻게 쓰는지" 직관적으로 알 수 있게

### 2.1 탭 활성 상태 시각 개선

**문제:** Lobby/Garage 탭에서 Garage가 선택되었는지 명확하지 않음.

**작업:**
- [ ] 활성 탭: 배경색 + 하단 보더(2px) 또는 왼쪽 보더(3px) 추가
- [ ] 비활성 탭: 배경색만, 보더 없음
- [ ] 활성 탭 텍스트: 흰색(#FFFFFF), 비활성: 회색(#8B95A8)
- [ ] 호버 상태 추가 (마우스 오버 시 배경 약간 밝게)

**예시:**
```
┌──────────┐  ┌──────────┐
│  Lobby   │  │▸ Garage  │  ← 활성 탭: 왼쪽 보더 + 밝은 배경
└──────────┘  └──────────
```

---

### 2.2 저장 피드백 강화

**문제:** 저장 성공/실패 시 피드백이 약함.

**작업:**
- [ ] 저장 성공: 초록색 토스트 메시지 ("Roster saved!") — 2초 후 자동 사라짐
- [ ] 저장 실패: 빨간색 배너 ("저장 실패: 다시 시도하세요") — 수동 dismiss
- [ ] 로딩 중: 저장 버튼에 스피너 또는 "Saving..." 텍스트
- [ ] `GarageResultPanelView.ShowToast(string, bool isError)` 메서드 추가

---

### 2.3 빈 슬롯 클릭 피드백

**문제:** 빈 슬롯(Slot 2, 4, 5, 6) 클릭 시 어떤 액션이 일어나는지 불명확.

**작업:**
- [ ] 빈 슬롯 클릭 → 에디터 패널이 해당 슬롯 번호로 전환
- [ ] 에디터 패널 상단에 "Editing: Slot 2 (Empty)" 표시
- [ ] "Click < > to add parts" 안내 텍스트 추가
- [ ] 빈 슬롯 호버 시 배경색 약간 밝게 (클릭 가능함을 시각적으로)

---

### 2.4 부품 선택 즉시 저장 명확화

**문제:** "Adjust selectors to overwrite this slot automatically" — 자동 저장이 되는지 수동 저장이 필요한지 불명확.

**작업:**
- [ ] 부품 선택 시 토스트: "Slot 1 updated: Striker → Relay"
- [ ] 또는 "Auto-saved to Slot 1" 토스트
- [ ] 저장 버튼의 역할 명확화: "Save Roster" = 전체 슬롯 저장 (네트워크 동기화)
- [ ] 도움말 툴팁 추가: "부품 변경은 즉시 에디터에 반영됩니다. Roster 저장을 눌러 네트워크에 저장하세요."

---

## Phase 3: 디자인 시스템 구축 (2~4주)

**목표:** 일회성 수정이 아니라 재사용 가능한 컴포넌트 체계로

### 3.1 버튼 컴포넌트 통일

**문제:** 탭 버튼, 부품 선택 버튼, 저장 버튼, Clear 버튼 등 스타일이 제각각.

**작업:**
- [ ] `ButtonStyles.cs` 생성 — 위치: `Features/Garage/Presentation/ButtonStyles.cs`
- [ ] 프리셋 정의:
  - `Primary` — 저장, 주요 액션 (파란색)
  - `Secondary` — 부품 선택 < > (컬러별: 파랑/주황/초록)
  - `Danger` — Clear Slot (빨간색)
  - `Tab` — 탭 버튼 (활성/비활성 상태)
  - `Ghost` — Google 로그인 (투명 배경 + 보더)
- [ ] 각 프리셋에 hover/pressed/disabled 상태 정의
- [ ] 기존 버튼들을 새 프리셋으로 교체

---

### 3.2 카드 컴포넌트 표준화

**문제:** 슬롯 카드, 부품 카드, 에디터 패널이 각자 다른 스타일.

**작업:**
- [ ] `CardPanel` 프리팹 생성 — 위치: `Assets/Prefabs/UI/Garage/CardPanel.prefab`
  - 배경: `#1A1E2E` (또는 현재 테마색)
  - 보더: `#2A3048`, radius 8px
  - 패딩: 16px
  - 제목 영역: `TMP_Text` + 구분선
- [ ] 슬롯 카드, 부품 카드, 에디터 패널을 CardPanel 기반으로 재구성
- [ ] 변형: `CardPanel.Compact` (슬롯용, 패딩 12px), `CardPanel.Default` (에디터용)

---

### 3.3 색상 토큰 체계

**작업:**
- [ ] `ThemeColors.cs` 생성 — 위치: `Features/Garage/Presentation/ThemeColors.cs`
  - Garage 피처 전용이므로 Shared에 두지 않음 (architecture.md 규칙)
- [ ] 토큰 정의:
  ```
  Background.Primary   = #0F1220
  Background.Secondary = #1A1E2E
  Background.Card      = #1E2436

  Text.Primary         = #FFFFFF
  Text.Secondary       = #A8B4C8
  Text.Muted           = #6B7A94

  Accent.Blue          = #3E7AE5
  Accent.Orange        = #E5573E
  Accent.Green         = #3EAF57
  Accent.Red           = #AF2E2E

  State.Selected       = #3E7AE5
  State.Hover          = #2A3048
  State.Disabled       = #3D4560
  ```
- [ ] 모든 UI 요소가 토큰 참조하도록 변경
- [ ] 테마 변경 시 토큰만 수정하면 전체 적용

---

### 3.4 계정 정보 영역 재구성

**문제:** "anonymous" / "Google sign-in ready" / "Google" 버튼이 우측 상단에 고립됨.

**작업:**
- [ ] 계정 정보 전용 카드 생성 (우측 패널 상단 또는 별도 영역)
- [ ] 구성:
  ```
  ┌─────────────────────────┐
  │ 👤 anonymous            │
  │    Google sign-in ready │
  │    [  Connect Google ]  │
  └─────────────────────────┘
  ```
- [ ] 로그인 후: "👤 user@gmail.com" + "[ Disconnect ]" 버튼
- [ ] 계정 카드와 에디터 영역을 시각적으로 분리 (보더 또는 간격)

---

## Phase 4: 고급 개선 (4주+)

**목표:** MVP 검증 이후, 본격적인 UX 고도화

### 4.1 3D 프리뷰 통합

**현재:** `PreviewRawImage`가 있지만 스크린샷에서 프리뷰가 제대로 렌더링되지 않음.

**작업:**
- [ ] `GarageUnitPreviewView`의 `RenderCamera`가 올바르게 대상 유닛을 향하도록
- [ ] 프리뷰 영역에 회전 제어 추가 (마우스 드래그로 360도 회전)
- [ ] 프리뷰 배경에 그리드 또는 파티클 효과 추가
- [ ] 부품 변경 시 프리뷰가 실시간으로 업데이트 (애니메이션 포함)

---

### 4.2 부품 비교 툴팁

**작업:**
- [ ] 부품 < > 버튼 호버 시 현재 부품 vs 다음 부품 비교 툴팁
- [ ] 비교 항목: HP, DMG, ASPD, Range, Move, Cost
- [ ] 상승/하강을 화살표(↑↓)와 색상(초록/빨강)으로 표시
- [ ] 툴팁 위치: 버튼 위 또는 옆에 고정

---

### 4.3 키보드 단축키

**작업:**
- [ ] `< >` 버튼에 키보드 단축키 매핑 (예: A/D 또는 ←/→)
- [ ] 슬롯 전환: 1~6 키
- [ ] 저장: Ctrl+S
- [ ] 단축키 안내: 하단에 "?" 버튼 또는 "Press ? for shortcuts"

---

### 4.4 모바일 대응

**목표:** WebGL 모바일 브라우저에서도 사용 가능하게

**작업:**
- [ ] 3패널 → 1패널 스택 레이아웃 (슬롯 탭 → 에디터 탭 → 프리뷰 탭)
- [ ] 부품 < > 버튼 크기 확대 (터치 타겟 44px 이상)
- [ ] 텍스트 크기 확대 (최소 14px)
- [ ] 스크롤 영역 확대

---

## 진행 상황

| Phase | 상태 | 시작일 | 완료일 | 비고 |
|---|---|---|---|---|
| Phase 1: 치명적 문제 | 🟡 진행 중 | 2026-04-13 | - | 1.1~1.3 |
| Phase 2: 상호작용 | ⬜ 대기 | - | - | |
| Phase 3: 디자인 시스템 | ⬜ 대기 | - | - | |
| Phase 4: 고급 개선 | ⬜ 대기 | - | - | |

---

## 검증 방법

### 플레이테스트 체크리스트

Phase 1 완료 후 다음을 확인:

- [ ] 우측 패널 텍스트가 겹치지 않고 읽기 쉬운가?
- [ ] 저장 버튼이 어디인지 3초 안에 찾을 수 있는가?
- [ ] 설명 텍스트가 배경과 구분되는가?
- [ ] 새 사용자가 "어디에 저장하지?"라고 묻지 않는가?

Phase 2 완료 후 추가:

- [ ] Garage 탭이 선택되었는지 명확한가?
- [ ] 저장 성공/실패를 즉시 알 수 있는가?
- [ ] 빈 슬롯 클릭 시 다음 행동을 알 수 있는가?
- [ ] 부품 변경이 즉시 저장되는지 명시적인가?

### 정량적 지표 (Firebase 연동)

- [ ] Garage 페이지 체류 시간 (목표: 3분 이상)
- [ ] 슬롯 저장 버튼 클릭률 (목표: 60% 이상)
- [ ] 부품 변경 횟수 (목표: 세션당 5회 이상)
- [ ] 저장 실패 후 재시도율 (목표: 20% 이하 — 낮을수록 좋음)

---

## 가정 및 제약

1. **1인 개발** — Phase 1만 완료해도 플레이테스트 진행 가능
2. **WebGL 빌드** — UI 변경이 빌드 사이즈에 영향 주지 않도록 (새 프리팹 최소화)
3. **기존 코드 구조** — Clean Architecture 레이어 위반 없이 Presentation 레이어만 수정
4. **Photon 네트워크** — 저장 로직은 `GarageNetworkAdapter`와 연동되도록 유지
5. **디자인 자산** — 색상/아이콘 변경은 기존 에셋 범위 내에서
6. **파일 배치 규칙** — Garage 전용 UI 컴포넌트(`ButtonStyles.cs`, `ThemeColors.cs`, `CardPanel.prefab`)는 Garage 피처 내에 배치. Shared에 올리지 않음 (architecture.md 규칙)

---

## 관련 문서

- MCP 개선 계획: `/docs/plans/mcp_improvement_plan.md`
- 게임 디자인 SSOT: `/docs/design/game_design.md`
- 아키텍처 규칙: `/agent/architecture.md`
- Unity 규칙: `/agent/unity.md` (섹션 14, 15)
