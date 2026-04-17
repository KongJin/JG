# Figma UI System Plan

> 마지막 업데이트: 2026-04-17
> 상태: 보류

이 문서는 Garage-first Figma 도입의 보류 기록이다.
레이아웃 SSOT는 [`ui_foundations.md`](../design/ui_foundations.md)를 따른다.

현재 대상 Figma 파일:

- `Untitled` — `https://www.figma.com/design/axm5HOzgC9reiuJPY6VBtj/Untitled?node-id=0-1&p=f&t=IRzuWexlmsbtsNSu-0`

## Summary

- 1차 범위는 `Garage`만 포함한다
- 목표는 Garage UI 레이아웃을 Figma 기준으로 재정의하고 Unity handoff를 안정화하는 것이다
- 시각 톤은 현재 메카-차고 방향과 `ThemeColors`를 유지한다
- 현재 상태에서는 이 계획을 실행 계획으로 사용하지 않는다
- 이유는 Figma MCP 연결, 권한, Starter 호출 한도 때문에 실제 작업 진입이 반복적으로 막혔기 때문이다

## Current Status

- 이 문서는 현재 `보류된 계획 문서`다
- Figma 기반 실제 작업은 현재 중단 상태다
- 실행 기준 문서로 쓰지 않고, 왜 중단됐는지 남기는 기록으로만 유지한다
- Garage UI 작업의 현재 실질 SSOT는 [`ui_foundations.md`](../design/ui_foundations.md)와 코드 구현이다

## Deliverables

### Repo Deliverables

1. [`ui_foundations.md`](../design/ui_foundations.md)
2. 이 문서
3. `progress.md` 진행 상황 반영
4. 기존 Garage 계획 문서의 SSOT 링크 정리

### Figma Deliverables

Garage 전용 Figma 파일 1개에 아래 논리 구조를 만든다.

1. `Foundations`
2. `Components`
3. `Garage`
4. `Handoff`

현재 1차 운영 방식:

- 사용 가능한 Figma 환경은 Starter만 전제한다
- 따라서 물리 페이지는 `3개`까지만 사용한다
- `Handoff`는 별도 페이지가 아니라 `Garage` 페이지 내부 섹션과 레포 문서로 운영한다
- 즉 Starter 환경의 실제 스캐폴드는 `Foundations / Components / Garage` 3페이지다

### Local Tooling Deliverables

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

현재 Figma 조사 결과:

- 접근 권한 확인 완료
- 루트 페이지 `Page 1 (0:1)` 접근 확인
- 첫 조회 시 파일은 사실상 비어 있는 상태로 확인
- 기존 `4페이지` 스캐폴드 생성 시도는 Starter 제약과 MCP 호출 한도에 막힘
- 2026-04-17 기준 운영 방식을 `Starter 3페이지 staged`로 전환

## Implementation Checklist

### Phase A. Foundations

- [x] Garage-first UI foundations 문서화
- [x] 공식 검증 해상도 고정: `390x844`, `1440x900`
- [x] 색상/간격/반지름/버튼 높이/타이포 토큰 이름 고정
- [ ] Figma `Foundations` 페이지 생성 또는 기존 `Page 1`을 `Foundations`로 정리
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

### Phase E. Figma Workflow

- [x] Figma MCP 사용 가능 여부 확인
- [x] 미등록 상태면 설치/등록 시도
- [x] Codex 글로벌 MCP에 원격 `figma` 서버 등록 + OAuth 인증 완료
- [x] 대상 파일 접근 권한 확인 (`node 0:1`, `Page 1`)
- [x] Starter 제약을 고려해 `3페이지 staged` 운영으로 전환
- [x] 로컬 `usfigma` skill 제거 후 직접 Figma MCP 프롬프트 기반으로 전환
- [ ] 연결 성공 상태에서 토큰/컴포넌트 인벤토리 추출
- [ ] 설치/등록이 실제로 막히면 Figma Inspect + 수동 체크리스트로 대체

### Current Blockers

1. Figma MCP Starter 호출 한도 때문에 읽기/쓰기 모두 안정적으로 진행할 수 없음
2. 계정 전환 시 파일 권한과 seat 상태가 계속 달라져 재현 가능한 작업 기준을 유지할 수 없음
3. Starter 환경에서는 문서가 전제한 운영 구조를 안정적으로 검증하기 어려움

### Required Remediation

이 문서를 다시 실행 계획으로 올리려면 아래가 먼저 해결되어야 한다.

1. 동일 계정으로 지속 접근 가능한 Figma 파일 확보
2. 읽기와 쓰기 모두 가능한 seat/권한 확보
3. Starter MCP 호출 한도에 막히지 않는 운영 환경 확보

이 세 가지 전에는 이 문서를 다시 활성 계획으로 올리지 않는다.

## Acceptance Criteria

### Design Acceptance

- `390x844`, `1440x900`에서 주요 정보가 겹치지 않는다
- 저장 버튼이 명확한 주요 액션으로 보인다
- 선택 슬롯, 빈 슬롯, 저장 성공/실패가 즉시 구분된다
- 계정 영역이 Garage 본문 정보와 겹치지 않는다

### Handoff Acceptance

- Figma 없는 구현자가 문서만 읽고 레이아웃 계약을 이해할 수 있다
- Figma 있는 구현자는 컴포넌트와 토큰을 그대로 Unity 대응표에 옮길 수 있다
- Figma MCP가 없더라도, 설치/등록 시도 결과와 fallback 절차가 함께 기록된다
- Starter 제약이 있어도 `3페이지 staged` 운영 규칙으로 같은 설계 계약을 유지할 수 있다

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
