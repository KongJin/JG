# Figma UI System Plan

> 마지막 업데이트: 2026-04-17
> 상태: 진행 시작

이 문서는 Garage-first Figma / usfigma 도입의 실행 계획이다.
레이아웃 SSOT는 [`ui_foundations.md`](../design/ui_foundations.md)를 따른다.

## Summary

- 1차 범위는 `Garage`만 포함한다
- 목표는 Garage UI 레이아웃을 Figma 기준으로 재정의하고 Unity handoff를 안정화하는 것이다
- 시각 톤은 현재 메카-차고 방향과 `ThemeColors`를 유지한다
- `usfigma`는 디자인 SSOT 보조와 handoff 체크리스트 생성까지만 맡는다

## Deliverables

### Repo Deliverables

1. [`ui_foundations.md`](../design/ui_foundations.md)
2. 이 문서
3. `progress.md` 진행 상황 반영
4. 기존 Garage 계획 문서의 SSOT 링크 정리

### Figma Deliverables

Garage 전용 Figma 파일 1개에 아래 페이지를 만든다.

1. `Foundations`
2. `Components`
3. `Garage`
4. `Handoff`

### Local Tooling Deliverables

- 로컬 `usfigma` skill
- Figma MCP 등록 또는 등록 불가 사유 기록
- Figma MCP가 실제로 막힐 때만 쓰는 수동 handoff 절차

## Current Inputs

1차 Garage 매핑 기준 구현:

- `GaragePageController`
- `GarageSlotItemView`
- `GaragePartSelectorView`
- `GarageResultPanelView`
- `GarageUnitEditorView`

현재 구조상 먼저 계약화해야 하는 이슈:

- 계정 패널 분리
- 우측 결과 패널 위계
- 저장 버튼 위치
- 빈 슬롯/선택 슬롯 상태 차이
- 개별 View 내부 런타임 레이아웃 보정 의존

## Implementation Checklist

### Phase A. Foundations

- [x] Garage-first UI foundations 문서화
- [x] 공식 검증 해상도 고정: `390x844`, `1440x900`
- [x] 색상/간격/반지름/버튼 높이/타이포 토큰 이름 고정
- [ ] Figma 스타일과 변수로 동일 토큰 구성
- [ ] Title / Section / Body / Caption 텍스트 스타일 생성

### Phase B. Component Library

- [ ] `TabButton`
- [ ] `SlotCard`
- [ ] `PartSelector`
- [ ] `SectionCard`
- [ ] `StatsBlock`
- [ ] `PrimaryButton`
- [ ] `AccountCard`
- [ ] `Toast`

각 컴포넌트 공통 체크:

- [ ] default 상태 정의
- [ ] 필요한 hover/selected/disabled/loading 상태 정의
- [ ] Auto Layout 구조가 Unity 컨테이너로 무리 없이 대응되는지 점검
- [ ] 토큰 직접 참조로 스타일이 바뀌도록 구성

### Phase C. Garage Screen Redesign

#### Desktop

- [ ] 좌 패널: Slot Strip + Slot Summary
- [ ] 중 패널: Unit Editor
- [ ] 우 패널: Account Card + Preview + Stats + Save Roster
- [ ] 저장 버튼을 우 패널 주요 CTA로 고정
- [ ] Preview / Stats / Save 위계 정리

#### Mobile

- [ ] 상단 Slot Strip 정의
- [ ] 본문 `Edit / Preview / Summary` 탭 정의
- [ ] 하단 `Save Roster` 고정 액션 정의
- [ ] 터치 타깃 최소 `44px` 준수

#### Core Flow Validation

- [ ] 슬롯 선택이 한눈에 읽힌다
- [ ] 부품 변경 후 현재 편집 상태가 즉시 보인다
- [ ] 저장 상태와 저장 액션이 같은 시야 안에서 이해된다

### Phase D. Unity Handoff

- [ ] Figma 컴포넌트 ↔ Unity 뷰 대응표 작성
- [ ] Auto Layout ↔ LayoutGroup 대응 규칙 점검
- [ ] `LayoutElement`가 필요한 구역 표시
- [ ] 앵커 기반 분기와 별도 컨테이너 경계를 표시
- [ ] 런타임 보정이 필요한 구역과 제거 목표를 분리 기록

권장 대응표:

| Figma component | Unity target | 메모 |
|---|---|---|
| `SlotCard` | `GarageSlotItemView` | empty / filled / selected 구분 |
| `PartSelector` | `GaragePartSelectorView` | frame / firepower / mobility 공통 구조 |
| `SectionCard` | `GarageResultPanelView`, `GarageUnitEditorView` | 카드 패턴 통일 |
| `PrimaryButton` | `GarageResultPanelView` 저장 버튼 | loading 포함 |
| `AccountCard` | `AccountSettingsView` 컨테이너 | Garage 본문과 분리 |

### Phase E. usfigma Workflow

- [x] `usfigma`는 자동 구현 도구가 아니라 handoff 보조로 범위 고정
- [x] Figma MCP 사용 가능 여부 확인
- [x] 미등록 상태면 설치/등록 시도
- [x] Codex 글로벌 MCP에 원격 `figma` 서버 등록 + OAuth 인증 완료
- [ ] 연결 성공 상태에서 토큰/컴포넌트 인벤토리 추출
- [ ] 설치/등록이 실제로 막히면 Figma Inspect + 수동 체크리스트로 대체

## Acceptance Criteria

### Design Acceptance

- `390x844`, `1440x900`에서 주요 정보가 겹치지 않는다
- 저장 버튼이 명확한 주요 액션으로 보인다
- 선택 슬롯, 빈 슬롯, 저장 성공/실패가 즉시 구분된다
- 계정 영역이 Garage 본문 정보와 겹치지 않는다

### Handoff Acceptance

- Figma 없는 구현자가 문서만 읽고 레이아웃 계약을 이해할 수 있다
- Figma 있는 구현자는 컴포넌트와 토큰을 그대로 Unity 대응표에 옮길 수 있다
- Figma MCP 또는 `usfigma`가 없더라도, 설치/등록 시도 결과와 fallback 절차가 함께 기록된다

### Regression Acceptance

- 기존 자동 저장/수동 저장 흐름은 깨지지 않는다
- Ready 연동과 `garageRoster` 저장 규칙은 유지된다
- GameScene의 Unit 스펙 계산 흐름과 충돌하지 않는다

## Out Of Scope

1차에서 하지 않는 것:

- 전 화면 리브랜딩
- Lobby / HUD 동시 재설계
- Unity 씬/프리팹 자동 생성
- Figma 산출물 기반 자동 코드 생성
- Figma MCP 설치 자체가 외부 계정/권한 문제로 막힌 상황의 조직적 해결
