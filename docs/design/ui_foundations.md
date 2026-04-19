# UI Foundations

> 마지막 업데이트: 2026-04-19
> 상태: Lobby + Garage Shell SSOT

이 문서는 **Lobby/Garage UI 레이아웃과 Figma handoff 규칙의 단일 기준**이다.
현재 범위는 `CodexLobbyScene`의 `LobbyPageRoot`와 `GaragePageRoot`까지 포함한다.
`Game HUD`는 현재 범위 밖이다.

## Source Of Truth

- 디자인 SSOT: Garage 전용 Figma 파일 1개
- 문서 SSOT: 이 문서 + [`figma_ui_system_plan.md`](../plans/figma_ui_system_plan.md)
- 색상 구현 SSOT: [`ThemeColors.cs`](../../Assets/Scripts/Features/Garage/Presentation/Theme/ThemeColors.cs)
- 외부 레퍼런스 참고 루틴: [`ui_reference_workflow.md`](./ui_reference_workflow.md)

Figma 파일의 논리 구조는 아래 네 개로 고정한다.

1. `Foundations`
2. `Components`
3. `Garage`
4. `Handoff`

Starter 플랜 제약이 있는 현재 1차 운영에서는 물리 페이지를 최대 3개만 사용한다.

1. `Foundations`
2. `Components`
3. `Garage`

이때 `Handoff`는 별도 페이지 대신 아래 두 위치에 분산해 유지한다.

- 레포 문서: [`figma_ui_system_plan.md`](../plans/figma_ui_system_plan.md)
- Figma `Garage` 페이지 내부의 handoff 전용 섹션 또는 프레임

문서 구조는 Figma의 논리 구조와 1:1로 대응한다.

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
- Lobby에서 room list가 create room과 garage summary보다 먼저 읽힌다
- Lobby에서 garage summary는 CTA와 상태만 보여 주고, deep editor 역할을 하지 않는다

## Foundations

### Scene Ownership

- `CodexLobbyScene.unity` is the final layout SSOT for Lobby/Garage shell work.
- `LobbyView`와 `GaragePageController`는 layout author가 아니다.
- scene-owned geometry는 MCP scene/prefab edits로 수정하고, runtime code는 상태 렌더와 page focus만 담당한다.
- decorative hierarchy naming은 contract 대상이 아니지만, section root와 serialized refs는 contract에 남긴다.
- `GaragePageController`는 smoke host가 아니다.
- WebGL/dev smoke 전용 엔트리포인트가 필요할 때도 production controller에 계속 누적하지 말고, 별도 bridge/driver로 분리하는 것을 원칙으로 한다.

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

### Visual Direction

Garage는 "포스터 같은 정적 아트"가 아니라 "상호작용 대시보드"다.
다만 시각 언어는 일반적인 게임 툴 패널보다 더 정제되고 밀도 있게 가져간다.

`canvas-design`류의 철학에서 Garage UI로 번역해 유지할 원칙은 아래 다섯 가지다.

1. **감축 우선**
   - 새 장식이나 새 패턴을 더하기보다 기존 블록의 간격, 정렬, 대비를 먼저 다듬는다.
   - 텍스트는 설명문이 아니라 상태와 행동을 짧게 고정하는 용도로만 쓴다.
2. **어두운 캔버스 + 제한된 강조색**
   - 전체 배경은 깊고 조용하게 유지하고, 시선 유도는 소수의 accent 색으로만 만든다.
   - 같은 화면에서 강한 색은 많아도 2~3개만 동시에 주도권을 가져야 한다.
3. **카드가 구조를 말해야 한다**
   - 정보는 문장보다 카드의 크기, 위치, 여백, 순서로 먼저 읽혀야 한다.
   - 특히 `AccountCard`, `Preview`, `Stats`, `Primary Action`은 서로 다른 밀도를 가져야 한다.
4. **비대칭은 허용하지만 정렬은 엄격하게**
   - 레이아웃은 좌우가 완전히 대칭일 필요는 없지만, 각 카드 내부 기준선은 엄격히 맞춘다.
   - "의도된 긴장감"과 "정돈되지 않음"은 구분되어야 한다.
5. **빈 상태도 완성형처럼 보여야 한다**
   - 프리뷰 미노출, 계정 미연결, 빈 슬롯 같은 상태도 임시 공백처럼 보이면 안 된다.
   - 비어 있을 때는 placeholder, 안내 문구, 배경 처리까지 포함해 하나의 완결된 카드처럼 보여야 한다

이 다섯 원칙은 Figma handoff가 재개되더라도 Garage 시각 판단의 기본 축으로 유지한다.

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
- 우측 패널은 `AccountCard -> Preview -> Stats/Primary Action`의 상하 스택으로 읽혀야 한다
- `AccountCard`는 상단 탭 또는 페이지 타이틀 영역과 겹치지 않는다
- 프리뷰가 비어 있어도 큰 검은 공백처럼 보이지 않게, 명확한 카드 경계와 주변 여백을 유지한다
- 가운데 `Unit Editor`는 보조 카드가 아니라 주 작업면처럼 보여야 하며, 우측 레일보다 더 넓은 편집 폭을 확보한다
- 우측 레일 내부 카드 간 간격은 최소 `16`을 유지해 한 덩어리로 뭉개져 보이지 않게 한다

## Lobby Layout Contract

### Lobby Main

Lobby main은 아래 읽기 순서를 유지한다.

1. `LobbyHeaderCard`
2. `RoomsSectionCard`
3. `CreateRoomCard`
4. `GarageSummaryCard`

고정 규칙:

- `Open Rooms`가 첫 시선과 첫 행동을 가져야 한다
- `Create Room`은 2차 카드이며 hero panel처럼 보이면 안 된다
- `GarageSummaryCard`는 저장 상태, ready/draft 상태, compact summary copy, `Open Garage` CTA만 보여 준다
- Lobby main에는 3D preview나 deep Garage editor controls를 두지 않는다
- `RoomDetailPanel`은 in-room flow 전용으로 유지하고, lobby home shell과는 별도 상태로 본다

### Visual Direction

- Lobby는 Garage와 같은 어두운 hangar 톤을 쓰되, 첫인상은 `matchmaking surface`로 읽혀야 한다
- strong accent는 room join/create와 garage CTA 두 군데를 넘지 않게 제한한다
- room list surface는 비어 있어도 공백처럼 보이면 안 되고, 항상 명확한 frame과 empty-state copy를 가진다
- garage summary는 tertiary 정보 카드로 남아야 하며, room list보다 시선을 먼저 가져가면 안 된다

### Mobile

모바일 Garage는 1패널 스택 구조로 정의한다.

- 상단: `Slot Selector`
- 본문 탭: `Edit / Preview / Summary`
- 하단 고정: `Save Roster`

고정 규칙:

- 상단 슬롯 선택부는 `6개 슬롯 전체를 한 번에 읽을 수 있는 compact grid/strip`로 유지한다
- 저장 버튼은 sticky/fixed 성격의 주요 액션으로 유지한다
- 스크롤 중에도 현재 편집 대상과 저장 액션을 잃지 않게 한다
- 터치 타깃은 최소 `44px` 이상으로 잡는다
- `Edit` 탭은 `UnitEditor + compact AccountCard`를 같은 본문 흐름 안에서 보여 준다
- `Preview`와 `Summary`는 동시에 펼치지 않고 본문에서 하나씩만 노출한다
- scene hierarchy 기준 mobile contract roots는 `GarageMobileStackRoot -> GarageMobileTabBar / MobileBodyHost`와 `RosterListPane/MobileSlotGrid`, `MobileSaveButton`이다

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
5. 개별 View 내부 `NormalizeLayout()`류 런타임 보정은 도입하지 않는다

금지 규칙:

- scene-owned UI의 `anchor / offset / size`를 runtime view가 재설정하지 않는다
- 반응형 대응을 위해 presentation layer에서 `RectTransform` geometry를 덮어쓰지 않는다
- layout drift를 해결하기 위한 런타임 `NormalizeLayout()`류 보정을 추가하지 않는다

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

## Figma Workflow

### Access Order

1. Figma MCP 또는 다른 직접 연동이 없으면 먼저 설치/등록 가능성을 확인한다
2. 직접 연동이 가능해지면 그것을 우선 사용한다
3. 설치/등록이 현재 환경에서 막히거나 외부 제약으로 불가능할 때만 Figma Inspect / 수동 측정으로 진행한다
4. 자동 생성 산출물은 SSOT가 아니다

### Current Tooling Role

현재 1차에서는 로컬 전용 Figma 스킬 없이 아래 두 축으로만 진행한다.

- Figma MCP 직접 호출
- 문서 SSOT 기반 handoff 체크리스트 유지

자동 코드 생성, Unity 자동 구현, 씬 구조 자동 패치 용도는 1차 범위 밖이다.

### Current Environment Note

현재 레포의 `.mcp.json`에는 `unity` MCP만 명시되어 있다.
다만 2026-04-17 기준 로컬 Codex 글로벌 MCP 설정에는 원격 `figma` 서버가 설치되고 OAuth 인증까지 완료된 상태다.

- Figma remote MCP: `https://mcp.figma.com/mcp`
- 설치 방식: `codex mcp add figma --url https://mcp.figma.com/mcp`
- 인증 방식: `codex mcp login figma`

또한 2026-04-17 기준 로컬 `usfigma` 스킬은 제거됐다.

즉 이 레포에서 Figma 작업을 시작할 때 기본값은 `Figma MCP 사용`이다.
수동 handoff는 설치/등록/인증이 실제로 막혔을 때만 fallback으로 사용한다.
