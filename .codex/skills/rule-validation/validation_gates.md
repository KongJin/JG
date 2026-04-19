# /agent/validation_gates.md

> 마지막 검토: 2026-04-11

이 문서는 이 레포에서 `clean`을 선언할 때 통과해야 하는 검증 게이트의 SSOT다.
엔트리포인트는 `../AGENTS.md`이고, 전역 구조 규칙은 `architecture.md`, 금지 패턴은 `anti_patterns.md`가 소유한다. 이 문서는 결과 판정과 검증 순서만 정의한다.

---

## Clean Levels

이 레포의 `clean`은 한 단계가 아니라 세 단계로 나뉜다.

> **세부 규칙**: 각 레벨의 포함은 [`/agent/validation/validation_gates.md`](#clean-levels-세부-규칙)를 참조하세요.

> **상위 우선순위**: [`/docs/design/game_design.md`](../../docs/design/game_design.md) - 게임 컨셉 SSOT가 최우선

### 1. static-clean

정적 스캔 기준 규칙 위반이 없다.

포함:

* 레이어 위반
* 금지된 Unity API / Photon API 사용
* feature 경계 / 포트 방향 위반
* 문서 owner/reference 규칙 위반
* **문서·코드 상태 불일치** (완료된 Phase가 `progress.md`에 반영되지 않음)
* 하네스가 구현한 기타 정적 규칙

제외:

* Unity 컴파일 성공 여부
* 씬/프리팹 직렬화 참조 유효성
* 플레이모드 동작

### 2. compile-clean

Unity Editor 기준 컴파일 에러가 0이다.

포함:

* `error CSxxxx`
* 누락 using / namespace drift
* 타입명 충돌
* 삭제되거나 이동된 심볼 참조
* asmdef / package 해석 실패로 인한 스크립트 컴파일 실패

### 3. runtime-smoke-clean (선택 보고)

지정된 최소 씬/플로우 런타임 검증이 통과한다. 이 단계는 **가능한 경우에만 추가 보고**한다.

현재 자동 하네스 범위에는 포함되지 않는다.
기본 보고 경로는 `docs/playtest/runtime_validation_checklist.md` 같은 수동 체크리스트 또는 세션 검증 기록이다.

예:

* 대상 씬 로드 가능
* 필수 Bootstrap/Setup가 정상 초기화
* 필수 직렬화 참조 누락 없음
* 핵심 플레이 플로우 진입/복귀 가능
* 필요 시 MCP의 compile/status/hierarchy/console 진단으로 보조 확인 가능

### 4. webgl-build-clean (선택 보고)

WebGL 빌드 성공 및 성능·최적화 기준이 통과한다.
이 단계는 **WebGL 빌드 배포 직전 또는 정기 검증 시** 추가 보고한다.

**컴파일·빌드 검증:**
* WebGL Development 빌드 에러 0
* IL2CPP 코드 스트리핑으로 인한 런타임 MissingMethodException 없음
* `webgl_optimization.md`는 `validation_gates.md`로 통합됨 — 별도 문서 없음

**성능 게이트 (목표 지표):**
* SetPass Calls < 50 (셰이더 교체 횟수)
* Batches < 100 (실제 드로우콜)
* Triangles < 100K (프레임당 폴리곤)
* 프레임 타임 < 33ms (30fps 유지)

**최적화 필수 설정:**
* SRP Batcher: ON (가장 중요 — 셰이더 단위 배칭)
* Static Batching: ON (움직이지 않는 배경 오브젝트용)
* Dynamic Batching: OFF (WebGL에서 CPU 오버헤드 > 이득)
* GPU Instancing: 반복 오브젝트(투사체, 이펙트)에 설정
* 렌더 스케일: 0.8x (GPU 부하 절감)

**금지 사항:**
* WebGL에서 Dynamic Batching 활성화
* Mobile_RPAsset에 과도한 포스트 프로세싱 추가
* 2048px 초과 텍스처 (권장: 1024px 이하)
* 실시간 그림자 남용 (1 cascade + soft shadow OFF 기본값 유지)

**모니터링 방법:**
* Unity Profiler: WebGL Development Build + Autoconnect Profiler
* Frame Debugger: Window > Analysis > Frame Debugger
* 브라우저 DevTools: Performance 탭 프레임 타임 측정
* Game 뷰 Stats 패널: Batches/SetPass calls 실시간 확인

**적용 우선순위:**
```
[즉시]  Static Batching ON → GPU Instancing 설정
[중기]  머티리얼 통합 → 텍스처 아틀라스 → 오클루전 컬링
[장기]  LOD → 셰이더 배리언트 → 라이트맵 튜닝
```

각 Phase 적용 후 Frame Debugger로 드로우콜 변화를 측정하고 기록한다.

---

## Clean 판정 규칙

* `static-clean`만 통과한 상태를 그냥 `clean`이라고 부르지 않는다.
* 사람 또는 하네스가 `clean`이라고 보고하려면 최소 `static-clean + compile-clean`을 만족해야 한다.
* `runtime-smoke-clean`은 선택적 추가 보고다. 부재 자체가 `compile-clean` 판정을 무효로 만들지는 않는다.
* 현재 자동 하네스는 `runtime-smoke-clean`을 기본으로 생성하지 않는다. 수동 체크리스트/세션 검증으로 추가 보고할 수 있다.
* Unity가 꺼져 있거나 compile 상태를 확인하지 못한 경우, 결과는 반드시 `static-clean only`로 표시한다.
* `static-clean only`는 임시 상태다. 운영 보고, 인계, 정기 요약에서 이를 `clean`이라고 부르지 않는다.
* `resolved`는 코드 수정 후 컴파일 확인까지 끝났을 때만 사용할 수 있다.

권장 출력 예:

* `static-clean only`
* `compile-clean`
* `compile-clean + runtime-smoke-clean`

---

## 필수 순서

기본 순서는 아래를 따른다.

1. 전역/로컬 SSOT 확인
2. 정적 규칙 점검
3. 코드 수정
4. Unity 컴파일 확인
5. 필요 시 런타임 스모크 확인
6. **문서·진행 상태 동기화** (`progress.md`, 관련 SSOT, 필요 시 `AGENTS.md`)
7. 최종 상태 기록

코드 수정 후 컴파일 확인 없이 `resolved` 또는 `clean`을 선언하지 않는다.
코드 완료 후 문서 업데이트 없이 `resolved` 또는 `clean`을 선언하지 않는다.

---

## 이 게이트에 필요한 규칙 추가

`compile-clean`을 위해 아래 정적 안전 규칙을 유지한다.

* feature 이름과 같은 short type name 충돌 금지
* 존재하지 않는 Shared 계약명 사용 금지
* 자주 이동하는 심볼의 namespace drift 탐지
* concrete vs interface drift 탐지
* 이벤트 계약 drift 탐지
* 레이어 안전 타입 배치 drift 탐지

대표 구조 문제:

* `short-type shadowing`
  `Features.Unit` namespace 안에서 `Unit`을 bare identifier 타입처럼 사용
* `phantom shared contract`
  실제 선언되지 않은 `IEventBus`를 공용 계약처럼 가정
* `missing import after symbol move`
  `Func<>`, `GarageRoster`, `StatusNetworkAdapter`, `SceneLoaderAdapter` 같은 심볼의 using drift
* `concrete vs interface drift`
  한쪽은 `EventBus`, 다른 쪽은 `IEventPublisher`만 가정해 wiring이 깨지는 상태
* `event contract drift`
  consumer가 더 이상 존재하지 않는 `GameEndEvent` 필드를 계속 참조하는 상태
* `layer-safe type placement drift`
  `PlacementArea`처럼 Unity 의존 scene helper가 Domain에 남아 있는 상태

이 규칙의 의미는 `architecture.md`와 `anti_patterns.md`가 소유하고, 이 문서는 왜 필요한지만 정의한다.

---

## 소유권

* 전역 게이트 규칙: `agent/*.md`
* feature 로컬 계약: `Assets/Scripts/Features/<Name>/README.md`
* Shared 로컬 계약: `Assets/Scripts/Shared/README.md`
* 자동 점검 구현체: `Assets/Editor/LayerDependencyValidator.cs`

## 정적 검증 실행 방법

`LayerDependencyValidator.cs`가 static-clean의 핵심 검증 도구다.

**Unity Editor에서 실행:**
* 메뉴: `Tools > Layer Dependency Validation` (또는 유사한 Editor 메뉴)
* 출력: `Temp/LayerDependencyValidator/feature-dependencies.json` (DAG 의존성 그래프)
* 콘솔에 레이어 위반, cycle 존재 시 즉시 보고

**검증 항목:**
* 레이어 위반 (Domain→Unity, Application→Photon, Shared→Features 등)
* Feature 간 cycle (`A→B→A`, `A→B→C→A`)
* Consumer-owned port 참조는 DAG edge에서 제외
* Analytics/reporting observer 코드는 gameplay DAG에서 제외

`static-clean`을 선언하려면 validator 실행 결과 위반 0, cycle 없음이어야 한다.
