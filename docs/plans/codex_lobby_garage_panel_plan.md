# CodexLobbyScene Garage 패널 구현 계획

> 마지막 업데이트: 2026-04-11
> 상태: historical
> doc_id: plans.codex-lobby-garage-panel
> role: historical
> owner_scope: 초기 Garage 패널 구상 기록
> upstream: design.ui-foundations, plans.progress
> artifacts: none

이 문서는 초기 Garage 패널 구상 기록이다. 현재 구현 기준으로 직접 사용하지 않는다.
현재 계약은 [`../design/ui_foundations.md`](../design/ui_foundations.md),
[`progress.md`](./progress.md)를 우선한다.

이 문서는 `CodexLobbyScene` 안에 `Garage`를 탭 페이지로 붙이는 작업의 구현 계획이다.
공식 진행 상태는 [`progress.md`](./progress.md)에서 관리한다.
레이아웃/컴포넌트 계약은 [`ui_foundations.md`](../design/ui_foundations.md)를 우선한다.

레퍼런스 방향:
- 상단 슬롯 스트립 + 하단 넓은 편집기 구조를 기본 레이아웃으로 사용한다.
- 시각 톤은 사용자가 공유한 메카 차고 UI 레퍼런스의 "빠르게 읽히는 슬롯 열 + 넓은 상세 편집" 감각을 따른다.

## Summary

- `Garage`는 별도 씬으로 분리하지 않고 `CodexLobbyScene` 내부의 상단 탭 페이지로 구현한다.
- 이번 작업 범위는 `Garage UI + 자동 저장 + 로비 Ready 연동`까지다.
- `전투 3슬롯 순환 덱형`은 이번 구현에 넣지 않고 장기 플랜으로만 기록한다.

확정된 기준:
- `Garage`는 로비 접속 직후부터 항상 접근 가능
- 상단 탭으로 `Lobby / Garage` 전환
- UI는 `상단 슬롯 스트립 + 하단 넓은 편집기`
- 슬롯은 총 6개
- Ready 가능 최소 편성은 3기
- `Clear`로 슬롯을 비워 최소 편성 수가 깨질 때만 Ready 해제
- 무효 조합은 슬롯에 확정 저장되지 않음
- 타인 Garage 정보는 이번 범위에서 노출하지 않고 기존 Ready 중심 유지

## Implementation Changes

### 1. 씬 구조와 페이지 전환

- `CodexLobbyScene`에 상단 네비게이션 바를 추가하고 `Lobby`, `Garage` 탭 버튼을 둔다.
- 기존 로비 UI는 `LobbyPageRoot` 아래로 묶고, 새 `GaragePageRoot`를 같은 레벨에 둔다.
- 페이지 전환은 씬 이동이 아니라 활성 패널 토글로 처리한다.
- 기본 진입 페이지는 `Lobby`로 유지한다.
- `GaragePageRoot`는 다음 구조로 고정한다:
  - 상단: 6개 편성 슬롯 스트립
  - 하단 좌/중앙: 선택 슬롯의 프레임/화력 모듈/기동 모듈 편집기
  - 하단 우측 또는 요약 영역: 현재 슬롯/전체 편성 상태 메시지, 비용/역할/핵심 스탯 요약
  - 선택 슬롯 비우기용 `Clear` 버튼

### 2. Garage UI 동작 규칙

- 상단 6개 슬롯 중 하나를 선택하면 하단 편집기가 해당 슬롯 상태를 편집한다.
- 슬롯이 비어 있으면 "새 유닛 추가" 상태로 편집기를 연다.
- 슬롯 편집 흐름은 `프레임 -> 화력 모듈 -> 기동 모듈` 순서로 고정한다.
- 조합이 완성되면 즉시 `ComposeUnitUseCase`와 `ValidateRosterUseCase`로 검증한다.
- 조합이 유효하면 자동 저장한다.
- 조합이 무효하면 저장하지 않고 해당 슬롯은 확정 상태로 반영하지 않는다.
- `Clear` 버튼은 현재 선택 슬롯만 비운다.
- 빈 슬롯이 있어도 총 편성 유닛 수가 3기 이상이면 전체 편성은 Ready 가능 상태다.

### 3. 코드 연결과 로비 연동

- `Garage` UI 컴포넌트는 `Features/Garage/Presentation`에 추가한다.
- `Lobby` 쪽에는 페이지 전환과 로비 버튼 상태 동기화를 담당하는 얇은 scene controller 또는 presentation 컴포넌트를 둔다.
- `LobbyBootstrap`은 `UnitBootstrap`과 `GarageBootstrap`를 다시 실제 조립 대상으로 포함한다.
- `CodexLobbyScene`에는 최소 다음 씬 계약을 추가한다:
  - `UnitBootstrap`
  - `GarageBootstrap`
  - `GarageNetworkAdapter`
  - Garage 페이지 루트와 편집 UI 오브젝트
- 자동 저장은 `SaveRosterUseCase`를 통해 로컬 저장 + Photon `garageRoster` 동기화로 처리한다.
- Ready 상태 연동 규칙:
  - 유효한 조합 저장만으로는 Ready를 해제하지 않는다
  - 사용자가 `Clear`로 슬롯을 비워 전체 편성 수가 3기 미만이 되면 Ready를 해제한다
  - 무효 조합은 저장 자체가 되지 않으므로 Ready 상태는 기존 확정 편성 기준으로 유지한다
- 타인 플레이어에는 기존 Ready 상태만 노출하고 Garage 세부 정보는 추가하지 않는다.

### 4. 데이터 규칙과 후속 메모

- 이번 구현 기준의 실제 편성 규칙은 `3~6기`로 통일한다.
- `GarageRoster.IsValid`와 `ValidateRosterUseCase`의 편성 수 규칙을 `3~6기` 기준으로 맞춘다.
- `3슬롯 순환 덱형`은 이번 구현에 넣지 않는다.
- 장기 플랜 메모:
  - 목표 모델은 Clash 스타일 `3슬롯 표시 + 6편성 대기열 순환`
  - 재소환 규칙은 덱 순환과 별개로 유지
  - 실제 구현은 Garage 패널 MVP 이후 별도 작업으로 분리

### 5. 자산과 UI 밀도

- 이번 1차는 기능 MVP이므로 3D 프리뷰나 고급 카드 연출은 제외한다.
- 다만 사용자가 빠르게 슬롯을 읽고 수정할 수 있도록 편집기 가로 폭과 상태 가독성을 우선한다.
- `ModuleCatalog` 또는 Garage 편집에 필요한 최소 자산이 비어 있으면, 이번 구현 범위 안에서 최소 동작 가능한 카탈로그/기본 데이터도 같이 준비한다.

## Public Interfaces / Contract Changes

- `CodexLobbyScene`의 씬 계약이 확장된다:
  - `GaragePageRoot` 및 Garage 관련 UI 참조 추가
  - `UnitBootstrap`, `GarageBootstrap`, `GarageNetworkAdapter` 필수화
- `Garage` validation/storage 계약은 `3~6기` 기준으로 정렬된다.
- `LobbyBootstrap`은 `Garage`를 실제 조립 대상으로 다시 포함한다.
- 로컬 계약 문서에는 다음을 명시한다:
  - Garage는 항상 접근 가능
  - 자동 저장형
  - `Clear`로 최소 편성 수를 깨는 경우만 Ready 해제
  - 타인 편성 비노출
  - 전투 3슬롯 순환은 후속 과제

## Test Plan

- `CodexLobbyScene`에 `Lobby`, `Garage` 탭과 Garage 루트가 모두 존재해야 한다.
- Photon 연결 직후 방 생성/참가 전에도 Garage 탭 진입과 편집이 가능해야 한다.
- 6개 슬롯 중 임의 슬롯 선택 시 하단 편집기가 해당 슬롯 상태를 정확히 반영해야 한다.
- 유효 조합 선택 시 자동 저장과 `garageRoster` 동기화가 즉시 일어나야 한다.
- 무효 조합 선택 시 저장되지 않고 기존 확정 슬롯 상태가 유지되어야 한다.
- 총 편성 3기 이상이면 Ready 가능해야 한다.
- `Clear`로 슬롯을 비워 총 편성이 3기 미만이 되면 Ready가 해제되어야 한다.
- Ready 상태 유지 중 유효한 조합 변경은 Ready를 해제하지 않아야 한다.
- 타인 플레이어 로비 UI에는 기존 Ready 정보만 보이고 Garage 세부 정보는 노출되지 않아야 한다.
- GameScene 진입 시 최신 `garageRoster`를 읽어 기존 유닛 스펙 계산 흐름이 깨지지 않아야 한다.

## Assumptions

- `Garage`는 `CodexLobbyScene` 내부 페이지로 구현한다.
- 이번 작업은 Garage UI/저장/로비 연동까지만 포함한다.
- `전투 3슬롯 순환 덱형`은 이번 구현 대상이 아니며 장기 계획으로만 기록한다.
- 준비 상태는 "유효 저장"으로는 유지되며, "슬롯 비우기로 최소 편성 수를 깨는 경우"에만 해제된다.
- 타인 편성 가시화는 이번 범위에 포함하지 않는다.
