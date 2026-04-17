# UI Foundations

> 마지막 업데이트: 2026-04-17
> 상태: Garage-first SSOT

이 문서는 **Garage UI 레이아웃과 Figma handoff 규칙의 단일 기준**이다.
1차 범위는 `Garage`만 포함한다.
`Lobby`, `Game HUD`는 Garage 토큰/컴포넌트 체계가 안정화된 뒤 같은 규칙으로 확장한다.

## Source Of Truth

- 디자인 SSOT: Garage 전용 Figma 파일 1개
- 문서 SSOT: 이 문서 + [`figma_ui_system_plan.md`](../plans/figma_ui_system_plan.md)
- 색상 구현 SSOT: [`ThemeColors.cs`](../../Assets/Scripts/Features/Garage/Presentation/Theme/ThemeColors.cs)

Figma 파일 페이지 구조는 아래 네 개로 고정한다.

1. `Foundations`
2. `Components`
3. `Garage`
4. `Handoff`

문서 구조는 Figma와 1:1로 대응한다.

- `Foundations` ↔ 이 문서
- `Garage`, `Handoff` ↔ [`figma_ui_system_plan.md`](../plans/figma_ui_system_plan.md)

## Validation Frames

공식 레이아웃 검증 해상도는 아래 두 개다.

- 모바일 우선: `390x844`
- 데스크톱 WebGL: `1440x900`

이 두 해상도에서 아래 조건을 만족해야 한다.

- Garage 주요 정보가 겹치거나 잘리지 않는다
- 저장 버튼이 첫 화면 또는 명확한 스크롤 맥락 안에 있다
- 선택 슬롯, 빈 슬롯, 저장 상태 차이가 즉시 구분된다

## Foundations

### Token Naming

토큰 이름은 Figma와 Unity에서 같은 의미 단위를 사용한다.

- `bg/*`: 화면/패널 배경
- `text/*`: 본문/보조/힌트 텍스트
- `accent/*`: 주요 액션과 강조
- `state/*`: hover, selected, disabled
- `slot/*`: 슬롯 상태 전용
- `toast/*`: 저장 피드백 전용

예:

- `bg/primary`
- `bg/card`
- `text/primary`
- `text/secondary`
- `accent/blue`
- `state/hover`

### Color Tokens

1차에서는 새 색 체계를 만들지 않는다.
현재 Garage 구현 색을 이름과 쓰임새 기준으로만 정리한다.

| Figma token | Unity source | 용도 |
|---|---|---|
| `bg/primary` | `BackgroundPrimary` | 페이지 전체 배경 |
| `bg/secondary` | `BackgroundSecondary` | 보조 배경, 하위 구역 |
| `bg/card` | `BackgroundCard` | 카드/패널 배경 |
| `text/primary` | `TextPrimary` | 주요 제목/숫자 |
| `text/secondary` | `TextSecondary` | 설명/보조 본문 |
| `text/muted` | `TextMuted` | 빈 상태/힌트 |
| `accent/blue` | `AccentBlue` | 주요 CTA, 활성 강조 |
| `accent/orange` | `AccentOrange` | 화력 모듈 강조 |
| `accent/green` | `AccentGreen` | 기동 모듈 강조 |
| `accent/red` | `AccentRed` | 위험/삭제 액션 |
| `state/selected` | `StateSelected` | 선택 상태 |
| `state/hover` | `StateHover` | hover 상태 |
| `state/disabled` | `StateDisabled` | 비활성 상태 |
| `slot/selected` | `SlotSelected` | 선택 슬롯 |
| `slot/filled` | `SlotFilled` | 저장된 슬롯 |
| `slot/empty` | `SlotEmpty` | 빈 슬롯 |
| `slot/empty-hover` | `SlotEmptyHover` | 빈 슬롯 hover |
| `toast/success-bg` | `ToastSuccessBg` | 성공 토스트 |
| `toast/error-bg` | `ToastErrorBg` | 실패 토스트 |

### Spacing / Radius / Sizing

1차 공용 토큰:

- Spacing: `4 / 8 / 12 / 16 / 24 / 32`
- Radius: `8 / 12 / 16`
- Button height: `40 / 48`

규칙:

- 카드 기본 패딩은 `16`
- 카드 간 기본 간격은 `16`
- 같은 카드 내부 섹션 간 간격은 `12`
- 캡션과 값의 간격은 `4` 또는 `8`
- 주요 저장 버튼 최소 높이는 `48`

### Typography

텍스트 체계는 아래 4개만 1차 표준으로 둔다.

- `Title`
- `Section`
- `Body`
- `Caption`

규칙:

- `Title`: 페이지/패널 주요 제목
- `Section`: 카드/섹션 제목
- `Body`: 값, 설명, 상태 메시지
- `Caption`: 슬롯 보조 설명, 힌트, 메타 정보

## Garage Layout Contract

### Desktop

데스크톱 Garage는 3패널 구조를 유지한다.

- 좌: `Slot Strip / Slot Summary`
- 중: `Unit Editor`
- 우: `Account Card / Preview / Stats / Primary Action`

고정 규칙:

- 저장 버튼은 우측 패널의 주요 액션으로 항상 노출한다
- 계정 정보는 우측 패널 내부에서도 별도 `AccountCard`로 분리한다
- 슬롯 상태와 저장 상태는 같은 카드 안에 섞지 않는다
- 3D 프리뷰, 통계, 저장 액션은 위계가 분명해야 한다

### Mobile

모바일 Garage는 1패널 스택 구조로 정의한다.

- 상단: `Slot Strip`
- 본문 탭: `Edit / Preview / Summary`
- 하단 고정: `Save Roster`

고정 규칙:

- 저장 버튼은 sticky/fixed 성격의 주요 액션으로 유지한다
- 스크롤 중에도 현재 편집 대상과 저장 액션을 잃지 않게 한다
- 터치 타깃은 최소 `44px` 이상으로 잡는다

### Core User Flow

1차에서 최적화하는 흐름은 아래 세 개뿐이다.

1. 슬롯 선택
2. 부품 변경
3. 저장 상태 확인 및 저장

그 외 고급 상호작용은 1차 우선순위가 아니다.

## Component Contract

1차 Figma/Unity 공통 컴포넌트 표준:

1. `TabButton`
2. `SlotCard`
3. `PartSelector`
4. `SectionCard`
5. `StatsBlock`
6. `PrimaryButton`
7. `AccountCard`
8. `Toast`

상태 규칙:

- 필요한 상태만 만든다: `default / hover / selected / disabled / loading`
- 같은 상태 의미는 Figma와 Unity에서 동일해야 한다
- 임시 장식용 variation을 늘리지 않는다

## Unity Translation Rules

Figma에서 Unity로 옮길 때 아래 규칙을 따른다.

1. Figma 컴포넌트 1개 = Unity 프리팹 또는 View 1개
2. Figma Auto Layout = Unity `HorizontalLayoutGroup` / `VerticalLayoutGroup` 우선
3. 최소/우선 크기 보장은 `LayoutElement`
4. 화면별 배치 차이는 앵커와 별도 컨테이너로 처리한다
5. 개별 View 내부 `NormalizeLayout()`류 런타임 보정은 점진적으로 줄인다

추가 규칙:

- Garage 전용 토큰은 계속 Garage 피처 내부에 둔다
- Shared로 승격하지 않는다
- 새 UI를 만들 때 먼저 Figma 컴포넌트 대응표를 작성한 뒤 구현한다

## Current Mapping Targets

1차 컴포넌트 매핑 대상은 아래 현재 구현 기준으로 잡는다.

- `GaragePageController`
- `GarageSlotItemView`
- `GaragePartSelectorView`
- `GarageResultPanelView`
- `GarageUnitEditorView`

## Figma / usfigma Workflow

### Access Order

1. Figma MCP 또는 다른 직접 연동이 없으면 먼저 설치/등록 가능성을 확인한다
2. 직접 연동이 가능해지면 그것을 우선 사용한다
3. 설치/등록이 현재 환경에서 막히거나 외부 제약으로 불가능할 때만 Figma Inspect / 수동 측정으로 진행한다
4. 자동 생성 산출물은 SSOT가 아니다

### usfigma Role

`usfigma`는 1차에서 아래 두 가지 용도로만 쓴다.

- 토큰/컴포넌트 인벤토리 추출
- Figma 변경점 대비 Unity 반영 체크리스트 생성

자동 코드 생성, Unity 자동 구현, 씬 구조 자동 패치 용도는 1차 범위 밖이다.

### Current Environment Note

현재 레포의 `.mcp.json`에는 `unity` MCP만 명시되어 있다.
다만 2026-04-17 기준 로컬 Codex 글로벌 MCP 설정에는 원격 `figma` 서버가 설치되고 OAuth 인증까지 완료된 상태다.

- Figma remote MCP: `https://mcp.figma.com/mcp`
- 설치 방식: `codex mcp add figma --url https://mcp.figma.com/mcp`
- 인증 방식: `codex mcp login figma`

즉 이 레포에서 Figma 작업을 시작할 때 기본값은 `Figma MCP 사용`이다.
수동 handoff는 설치/등록/인증이 실제로 막혔을 때만 fallback으로 사용한다.
