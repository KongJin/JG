# UI Foundations

> 마지막 업데이트: 2026-04-27
> 상태: active
> doc_id: design.ui-foundations
> role: ssot
> owner_scope: Lobby와 Garage UI 레이아웃, 토큰, Unity 변환 규칙
> upstream: design.game-design, ops.unity-ui-authoring-workflow, design.ui-reference-workflow
> artifacts: `.stitch/contracts/`, `Assets/UI/`, `Assets/Scenes/`, `artifacts/unity/`

이 문서는 **Lobby/Garage UI 레이아웃과 Unity 변환 규칙의 단일 기준**이다.
현재 범위는 폐기된 legacy scene이 아니라, 새로 다시 세울 Lobby/Garage baseline surface contract를 포함한다.
`Game HUD`는 현재 범위 밖이다.
응집도와 결합도의 상위 정의, 예외, hard-fail/review 경계는 `ops.cohesion-coupling-policy`를 따른다.
이 문서는 Lobby/Garage UI 설계 lane의 레이아웃, 토큰, Unity 변환 파생 규칙만 소유한다.

## Source Of Truth

- 문서 SSOT: 이 문서
- import 진행 중 layout SSOT: accepted Stitch structured contract + UI Toolkit candidate surface + capture/report evidence
- 색상 구현 SSOT: UI Toolkit USS/tokens under `Assets/UI/`
- 외부 레퍼런스 참고 원칙 owner: `design.ui-reference-workflow` (`docs/index.md`에서 현재 경로 확인)

## Validation Frames

공식 레이아웃 검증 해상도는 `390x844` 하나로 고정한다.

새 UI Toolkit candidate surface와 preview scene도 같은 `390x844`를 기준으로 맞춘다.
다만 import 중에는 과거 canonical `page-switch smoke` 산출물을 acceptance proof로 재사용하지 않는다.

이 해상도에서 아래 조건을 만족해야 한다.

- Garage 주요 정보가 겹치거나 잘리지 않는다
- 저장 버튼이 첫 화면 또는 명확한 스크롤 맥락 안에 있다
- 선택 슬롯, 빈 슬롯, 저장 상태 차이가 즉시 구분된다
- Lobby에서 room list가 create room과 garage summary보다 먼저 읽힌다
- Lobby에서 garage summary는 CTA와 상태만 보여 주고, deep editor 역할을 하지 않는다
- Garage auxiliary flow는 `Settings overlay open -> close` smoke로 별도 검증한다

## Foundations

### Scene Ownership

- Stitch import 중에는 legacy scene이 아니라 UI Toolkit candidate surface와 capture evidence가 Layout SSOT 역할을 먼저 맡는다.
- `LobbyView`와 `GaragePageController`는 layout author가 아니다.
- geometry는 MCP prefab/scene authoring으로 수정하고, runtime code는 상태 렌더와 page focus만 담당한다.
- decorative hierarchy naming은 contract 대상이 아니지만, section root와 serialized refs는 contract에 남긴다.
- `GaragePageController`는 smoke host가 아니다.
- WebGL/dev smoke 전용 엔트리포인트가 필요할 때도 production controller에 계속 누적하지 말고, 별도 bridge/driver로 분리하는 것을 원칙으로 한다.
- production `*PageController`는 thin orchestration만 맡는다. page chrome styling, header/save dock skinning, button preset 선택 같은 시각 책임은 dedicated view/controller로 분리한다.
- 이 lane에서는 값 하드코딩과 fallback 보정을 정답으로 문서화하지 않는다. 숫자, 토큰, 상태 기본값의 owner는 token SSOT, serialized contract, 또는 scene/prefab contract다.

### Serialized Reference Policy

- scene/prefab에서 항상 있어야 하는 UI 참조는 `Required`로 선언하고 null-check로 감싸지 않는다.
- 필수 참조는 Inspector + `RequiredFieldValidator` + project audit를 통과한 뒤 코드에서 계약으로 신뢰한다.
- truly optional인 경우만 nullable로 두고, 멤버별 null-check 대신 `has feature/capability` 단위로 한 번 분기한다.
- `내부 serialized member가 null일 수도 있다`는 가정으로 렌더 코드를 작성하지 않는다.
- `Initialize()`나 composition root에서 보장되는 외부 주입도 매 프레임/매 렌더에서 반복 확인하지 않는다.
- legacy UI fallback 습관 때문에 남아 있던 `if (_member != null)` 패턴은 Required/audit 계약으로 대체하고, truly optional section만 capability helper로 남긴다.

### Token Naming

토큰 이름은 디자인 문서와 Unity에서 같은 의미 단위를 사용한다.

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

### Shared Lobby/Garage Navigation

- `Lobby`와 `Garage` 사이 페이지 전환은 scene shell의 단일 shared nav bar가 맡는다.
- shared nav contract root는 `/Canvas/LobbyGarageNavBar`다.
- shared nav는 `LobbyTabButton`, `GarageTabButton` 두 버튼만 가진다.
- Lobby/Garage surface root는 page-routing chrome을 직접 소유하지 않는다.
- `GarageMobileTabBar`는 page nav가 아니라 `Frame / Weapon / Mobility` 포커스 바로 유지한다.
- Stitch shared shell 후보에서 `로비 / 차고 / 기록` navigation을 검토할 때도 selected state는 command blue를 쓰고, signal orange는 page-owned primary CTA에만 남긴다.
- 현재 runtime `LobbyGarageNavBar`의 두 버튼 계약을 바꾸는 작업은 별도 runtime replacement pass로 분리한다.

Garage는 mobile-first 단일 구조로 정의한다.

- 상단: `Title / current slot summary / Settings`
- 스크롤 본문 상단: `Slot Selector`
- 스크롤 본문 본체: `Part Focus Bar -> Focused Editor -> Final Preview -> Summary`
- 하단 고정: `Save Dock`

고정 규칙:

- 슬롯 선택부는 첫 화면에서 먼저 보이되, 본문과 같은 scroll content 안에 둬서 아래로 스크롤하면 자연스럽게 사라지게 한다
- 저장 버튼은 sticky/fixed 성격의 주요 액션으로 유지한다
- 본문은 `slot first -> single scroll body -> fixed save dock` 흐름을 유지한다
- 터치 타깃은 최소 `44px` 이상으로 잡는다
- `Frame / Weapon / Mobility` 포커스 바는 현재 편집 부위를 바꾸는 컨트롤이다
- 포커스 바를 바꾸면 본문 에디터는 해당 selector 하나만 크게 노출한다
- Preview와 Summary는 같은 세로 흐름 안에 함께 노출할 수 있다
- `Settings overlay`는 Garage main flow 밖의 auxiliary panel로 둔다
- 저장 완료 후 본문 스크롤은 상단으로 복귀해 슬롯 상태를 다시 확인할 수 있어야 한다
- scene hierarchy 기준 contract roots는 `LobbyGarageNavBar`, `GarageMobileStackRoot -> MobileBodyHost -> MobileBodyScrollContent`, `GarageMobileTabBar`, `GarageMobileStackRoot/MobileBodyHost/MobileBodyScrollContent/RosterListPane/SlotStripRow`, `MobileSaveDock`, `GarageSettingsOverlay`다

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
- `GarageSummaryCard`는 저장 상태, ready/draft 상태, compact summary copy만 보여 준다
- Lobby main에는 3D preview나 deep Garage editor controls를 두지 않는다
- `RoomDetailPanel`은 in-room flow 전용으로 유지하고, lobby home shell과는 별도 상태로 본다

### Visual Direction

- Lobby는 Garage와 같은 어두운 hangar 톤을 쓰되, 첫인상은 `matchmaking surface`로 읽혀야 한다
- strong accent는 room join/create와 shared nav 두 군데를 넘지 않게 제한한다
- room list surface는 비어 있어도 공백처럼 보이면 안 되고, 항상 명확한 frame과 empty-state copy를 가진다
- garage summary는 tertiary 정보 카드로 남아야 하며, room list보다 시선을 먼저 가져가면 안 된다

### Core User Flow

1차에서 최적화하는 흐름은 아래 세 개뿐이다.

1. 슬롯 선택
2. 부품 변경
3. 저장 상태 확인 및 저장

그 외 고급 상호작용은 1차 우선순위가 아니다.

## Component Contract

1차 공통 vocabulary는 loose list가 아니라 shared component catalog로 고정한다.

- contract path: `.stitch/contracts/components/shared-ui.component-catalog.json`
- schema path: `.stitch/contracts/components/schema/stitch-component-catalog.schema.json`
- 이 catalog는 `supporting design/reference lane`이다.
- active generator input은 source freeze에서 다시 준비된 execution contracts다.

owner boundary:

- token SSOT는 계속 이 문서와 `ThemeColors.cs`가 가진다.
- shared component catalog는 재사용 가능한 `atom / molecule` vocabulary만 가진다.
- `screen-manifest.blocks[]`는 surface 의미와 읽기 순서를 계속 소유한다.
- `unity-map`은 host path binding만 계속 소유한다.

v1 atom vocabulary:

1. `primary-button`
2. `tab-button`
3. `icon-button`
4. `status-text`
5. `stat-chip`

v1 molecule vocabulary:

1. `slot-card`
2. `section-card`
3. `part-selector`
4. `stats-block`
5. `account-card`
6. `toast`

상태 규칙:

- 필요한 상태만 만든다: `default / hover / selected / disabled / loading / empty / error / success`
- 같은 상태 의미는 Stitch와 Unity에서 동일해야 한다
- 임시 장식용 variation을 늘리지 않는다
- v1 catalog에는 `organism / block / surface` 레벨 entry를 넣지 않는다

Garage worked example:

- `slot-selector -> slot-card`
- `focus-bar -> tab-button`
- `editor-panel -> section-card + part-selector + stats-block`
- `preview-card -> section-card + status-text + stat-chip`
- `summary-card -> section-card + stats-block + toast`
- `save-dock -> primary-button + status-text`
- `header-chrome -> status-text + icon-button`

## Unity Translation Rules

Figma에서 Unity로 옮길 때 아래 규칙을 따른다.

1. Figma 컴포넌트 1개 = Unity 프리팹 또는 View 1개
2. Figma Auto Layout = Unity `HorizontalLayoutGroup` / `VerticalLayoutGroup` 우선
3. 최소/우선 크기 보장은 `LayoutElement`
4. 화면 배치 차이를 위한 desktop/mobile 이중 구조를 만들지 않는다
5. 개별 View 내부 `NormalizeLayout()`류 런타임 보정은 도입하지 않는다

금지 규칙:

- scene-owned UI의 `anchor / offset / size`를 runtime view가 재설정하지 않는다
- 반응형 대응을 위해 legacy runtime UI script에서 `RectTransform` geometry를 덮어쓰지 않는다
- layout drift를 해결하기 위한 런타임 `NormalizeLayout()`류 보정을 추가하지 않는다

추가 규칙:

- Garage 전용 토큰은 계속 Garage 피처 내부에 둔다
- Shared로 승격하지 않는다
- 새 UI를 만들 때 먼저 `block -> shared component` 대응표를 작성한 뒤 구현한다
- shared component catalog는 vocabulary reference일 뿐이고, UI Toolkit candidate authoring 입력은 source freeze에서 다시 준비된 execution contracts다.

## Current Mapping Targets

1차 컴포넌트 매핑 대상은 아래 현재 구현 기준으로 잡는다.

- `GaragePageController`
- `GarageSlotItemView`
- `GaragePartSelectorView`
- `GarageResultPanelView`
- `GarageUnitEditorView`

## Design Tool Note

- 외부 디자인 툴은 선택 사항이다.
- 어떤 툴을 쓰더라도 이 문서와 scene contract가 레이아웃 SSOT다.
- 자동 생성 산출물이나 개인 로컬 MCP 설정은 레포 기준으로 가정하지 않는다.
- 구현자는 필요하면 수동 handoff, Inspect, 스크린샷 비교만으로도 작업을 이어갈 수 있어야 한다.
