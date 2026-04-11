# CodexLobbyScene Garage UI 2차 리팩터링 계획

> 마지막 정리: 2026-04-11

이 문서는 `CodexLobbyScene` Garage UI 리팩터링의 2차 마무리 계획이다.
1차 목표였던 `GaragePanelView` 해체와 `controller + subview` 구조 분리는 워킹트리 기준으로 대부분 반영됐고, 이제 남은 일은 새 구조를 compile/runtime 기준으로 안정화하고 scene contract를 닫는 것이다.
공식 진행 상태는 [`progress.md`](./progress.md)에서 관리한다.

## Summary

- 1차 분리 결과로 `GaragePageController`, `GarageRosterListView`, `GarageUnitEditorView`, `GarageResultPanelView`, `GaragePagePresenter`, `GaragePageState`, `Garage*ViewModel`이 도입됐다.
- `GarageSetup`, `LobbyView`, `CodexLobbyScene`, `CodexLobbyGarageAugmenter`, `Garage/ Lobby README`도 새 3분할 Garage 페이지 계약에 맞춰 갱신 중이다.
- 2차 리팩터링은 새 구조를 유지한 채 compile blocker 제거, 책임 경계 고정, Inspector wiring 검증, 탭 왕복/Ready interlock smoke test를 완료하는 데 집중한다.
- 도메인 규칙인 `6슬롯`, `유효 조합만 저장`, `최소 3기 Ready`, `Clear 시 Ready 영향`은 이번 단계에서도 바꾸지 않는다.

## Current Snapshot

### 이미 반영된 것

- Garage 화면은 `좌측 슬롯 리스트 / 중앙 편집기 / 우측 결과 패널` 3분할 레이아웃으로 재구성됐다.
- `GarageSetup`의 scene 참조는 `GaragePanelView` 대신 `GaragePageController`를 받는다.
- `LobbyView`는 `Lobby/Garage` 탭과 `GaragePageRoot`를 같은 씬 안에서 전환한다.
- Presentation은 `committed roster`와 `editing draft`를 분리해 다루도록 재구성됐다.

### 아직 닫히지 않은 것

- `GaragePageController`, `GarageDraftEvaluation`가 `Result<Unit>`를 nullable처럼 다뤄 compile error가 남아 있다.
- scene contract는 코드/README/scene YAML까지 반영됐지만, 실제 Inspector wiring과 탭 왕복 동작은 아직 smoke test로 닫히지 않았다.
- progress 문서는 한동안 "장기 후속 계획" 기준으로 남아 있었기 때문에, 2차부터는 "진행 중인 구조 안정화" 기준으로 읽어야 한다.

## Phase 2 Goals

### 1. Compile/State 안정화

- `GarageDraftEvaluation`은 nullable `Result<Unit>` 전제를 버리고, `draft 미완성 / compose 실패 / compose 성공` 상태를 명시적으로 표현한다.
- `GaragePageController`는 `Initialize / Select / Cycle / Clear / TryCommit / Render` orchestration만 담당하고, 상태 판별 로직은 `GaragePageState`와 `GarageDraftEvaluation`으로 더 밀어낸다.
- presenter는 순수 표시 모델 변환만 담당하고, 저장/검증/compose 호출은 controller 밖으로 새지 않게 유지한다.

### 2. Scene Contract 마감

- `CodexLobbyScene`에서 `GarageSetup -> GaragePageController -> subview` 참조가 모두 연결된 상태를 기준 계약으로 확정한다.
- `CodexLobbyGarageAugmenter`가 새 3분할 구조와 각 selector/view/controller 참조를 완전하게 생성/연결하는지 재검증한다.
- `GaragePanelView` 제거 이후 레거시 브리지 문구가 정말 필요한지 재판단하고, 불필요하면 문서/코드에서 걷어낸다.

### 3. Runtime 동작 검증

- Garage 첫 진입 시 `InitializeGarage.Execute()` 결과가 좌측 committed roster와 중앙 선택 슬롯에 올바르게 반영되는지 확인한다.
- `Lobby -> Garage -> Lobby -> Garage` 왕복 시 controller 재초기화 없이 선택 슬롯과 committed roster가 유지되는지 확인한다.
- invalid draft는 우측 결과에만 오류를 보이고, 좌측 슬롯 리스트와 roster count는 바뀌지 않는지 확인한다.
- valid draft는 즉시 저장되고 `RosterSavedEvent`를 통해 `RoomDetailView`의 Ready 가능 상태가 갱신되는지 확인한다.
- `Clear`가 슬롯 삭제, roster count 감소, 필요 시 Ready auto-cancel까지 이어지는지 확인한다.

### 4. 문서/운영 정리

- `progress.md`는 "장기 후속" 표현 대신 "1차 구조 분리 완료, 2차 안정화 진행" 상태로 유지한다.
- `Garage` README와 `Lobby` README는 실제 scene contract와 코드가 완전히 일치하도록 final wording을 맞춘다.
- 2차 리팩터링 종료 시 이 문서는 decision-complete에서 done/archived 상태로 넘길 수 있게 잔여 TODO를 없앤다.

## Work Breakdown

1. compile error를 먼저 제거해 새 Presentation 구조가 최소한 C# 기준으로 닫히게 만든다.
2. `GarageDraftEvaluation`, `GaragePageState`, `GaragePageController`의 책임 경계를 정리해 draft/committed 흐름을 읽기 쉽게 만든다.
3. `CodexLobbyScene`과 `CodexLobbyGarageAugmenter`의 Inspector contract를 검증한다.
4. Garage 탭 왕복, 자동 저장, invalid draft 유지, Clear, Ready interlock 순서로 smoke test를 수행한다.
5. README와 `progress.md`를 최종 상태로 정리한다.

## Test Plan

- compile
  - Garage Presentation 관련 C# compile error가 없어야 한다.
  - 단, 현재 repo 전체 `dotnet build ProjectSD.slnx`는 Photon demo csproj 누락 파일 때문에 별도 실패할 수 있으므로 Garage 관련 compile gate와 repo baseline noise를 분리해서 판단한다.
- scene wiring
  - `CodexLobbyScene`에서 `GarageSetup`, `GaragePageController`, `GarageRosterListView`, `GarageUnitEditorView`, `GarageResultPanelView`, `GarageNetworkAdapter`, `UnitSetup` 참조가 모두 연결돼야 한다.
  - augmenter 재실행 후에도 동일 계약이 재현돼야 한다.
- runtime
  - Garage 탭 첫 진입, 탭 왕복, slot selection, part cycling, invalid draft, valid save, `Clear`, Ready auto-cancel이 모두 의도대로 동작해야 한다.
  - console error는 Garage 관련 신규 오류 기준 `0`이어야 한다.

## Exit Criteria

- `GaragePageController` 기반 Presentation이 compile/runtime 기준으로 안정화된다.
- 탭 전환과 Ready interlock까지 포함한 Garage UI 흐름이 smoke test로 재현된다.
- scene contract가 코드, README, `progress.md`, scene/augmenter 기준으로 서로 모순 없이 맞는다.

## Assumptions

- 이번 2차 리팩터링은 Garage Presentation 구조 안정화가 목표이며, 도메인 규칙이나 Photon 동기화 모델 자체는 바꾸지 않는다.
- `Setup` 네이밍이 현재 씬 조립 SSOT이며, `Bootstrap` 명칭으로 되돌리지 않는다.
- 한글 TMP 폰트 경고 문제와 Garage 외 다른 feature refactor는 별도 작업으로 분리한다.
