# Garage Feature

## Responsibility

`Garage`는 로컬 차고 편성, Photon `garageRoster`/`garageReady` 동기화, 그리고 로비 씬의 편성 UI를 소유한다.

## Entrypoints

* `GarageBootstrap.cs`
  scene-level wiring, persistence/network/presentation 조립
* `Presentation/GaragePanelView.cs`
  6슬롯 차고 페이지 UI, 자동 저장, 선택 슬롯 편집
* `Infrastructure/GarageNetworkAdapter.cs`
  Photon CustomProperties 기반 편성 동기화

## Local Contract

* `GarageBootstrap`는 `GarageNetworkAdapter`를 inspector로 받아야 한다.
* `GaragePanelView`는 선택 참조다. 로비 씬에 있으면 Garage UI를 초기화하고, 없으면 데이터/네트워크 경로만 유지한다.
* `GaragePanelView`는 6개 `GarageSlotItemView`, 3개 선택기(prev/next/value/hint), 상태 텍스트, `Clear` 버튼 참조가 필요하다.
* `GarageRoster`는 직렬화 시 항상 6슬롯으로 정규화되며, 빈 슬롯은 비어 있는 `UnitLoadout`으로 표현한다.
* `SaveRosterUseCase`는 0~6 저장 슬롯을 모두 저장하지만 `garageReady`는 `3~6기` 유효 편성일 때만 true로 동기화한다.

## Scene-owned Notes

* `CodexLobbyScene`의 Garage UI는 `GaragePageRoot` 단일 패널로 존재하며, 씬 전환이 아니라 탭 전환으로 표시된다.
* 상단 슬롯 스트립은 저장된 슬롯 상태를 보여주고, 하단 편집기는 현재 선택 슬롯만 수정한다.
* 유효 조합은 즉시 저장되고, 무효 조합은 슬롯에 확정 반영되지 않는다.
* `Clear`로 슬롯을 비워 총 편성 수가 3기 미만이 되면 로비 `Ready` 버튼이 자동으로 해제된다.
