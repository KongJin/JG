# Garage Feature

## Responsibility

`Garage`는 로컬 차고 편성, Photon `garageRoster`/`garageReady` 동기화, 그리고 로비 씬의 편성 UI를 소유한다.

## Entrypoints

* `GarageSetup.cs`
  scene-level wiring, persistence/network/presentation 조립
* `Presentation/GaragePageController.cs`
  Garage 페이지 상태/저장/검증 orchestration
* `Presentation/GarageRosterListView.cs`, `GarageUnitEditorView.cs`, `GarageResultPanelView.cs`
  좌측 슬롯 리스트 / 중앙 편집기 / 우측 결과 패널 렌더링과 입력 이벤트 발행
* `Infrastructure/GarageNetworkAdapter.cs`
  Photon CustomProperties 기반 편성 동기화

## Local Contract

* `GarageSetup`는 `GarageNetworkAdapter`를 inspector로 받아야 한다.
* `GarageSetup`의 page 참조는 `GaragePageController`를 받는다. 레거시 씬은 `GaragePanelView : GaragePageController` 브리지로 유지할 수 있다.
* `GaragePageController`는 `GarageRosterListView`, `GarageUnitEditorView`, `GarageResultPanelView`를 받아야 한다.
* `Garage` Presentation은 `committed roster`와 `editing draft`를 분리한다. 좌측 리스트는 committed만, 중앙/우측은 draft를 기준으로 표시한다.
* `GarageRoster`는 직렬화 시 항상 6슬롯으로 정규화되며, 빈 슬롯은 비어 있는 `UnitLoadout`으로 표현한다.
* `SaveRosterUseCase`는 0~6 저장 슬롯을 모두 저장하지만 `garageReady`는 `3~6기` 유효 편성일 때만 true로 동기화한다.

## Scene-owned Notes

* `CodexLobbyScene`의 Garage UI는 `GaragePageRoot` 아래 `RosterListPane / UnitEditorPane / ResultPane` 3영역으로 존재하며, 씬 전환이 아니라 탭 전환으로 표시된다.
* `GaragePageController.Initialize(...)`는 씬 수명 동안 한 번만 실행되고, Garage 탭 재진입 시 controller를 재초기화하지 않는다.
* 유효 조합은 즉시 저장되고, 무효 조합은 draft/결과 패널에만 남고 committed 슬롯 리스트에는 확정 반영되지 않는다.
* `Clear`로 슬롯을 비워 총 편성 수가 3기 미만이 되면 로비 `Ready` 버튼이 자동으로 해제된다.
