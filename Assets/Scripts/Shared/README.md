# Shared

## Responsibility

`Shared`는 여러 feature가 함께 쓰는 공통 계약, 값 객체, 런타임 서비스, 유틸리티만 둔다.

## Source Of Truth

Shared 계약은 실제 선언 파일이 유일한 기준이다. 선언되지 않은 공용 계약 이름을 추측해서 쓰지 않는다.

예:

* `EventBus`
* `IEventPublisher`
* `IEventSubscriber`

`IEventBus` 같은 phantom contract는 Shared 표준이 아니다.

## Allowed

* feature 독립적인 공통 인터페이스와 구현
* 공통 kernel/value types
* 공통 lifecycle / analytics / sound / time / ui infrastructure
* 실제로 둘 이상의 feature가 함께 쓰는 코드

## Not Allowed

* 특정 feature에만 의미가 있는 도메인 규칙
* feature-specific Application port
* feature-specific scene contract
* 존재하지 않는 공용 계약 이름을 추측해서 추가하는 것

## Current Shared Contracts

주요 루트:

* `EventBus`
* `Kernel`
* `Lifecycle`
* `Time`
* `Analytics`
* `Ui`
* `Attributes`

EventBus 계약은 실제 선언 파일 기준으로 본다.

## Validation Notes

* Shared 변경은 전 feature에 파급될 수 있으므로 `static-clean`만으로 끝내지 않는다.
* `clean` 판정은 `/agent/validation_gates.md`를 따른다.
