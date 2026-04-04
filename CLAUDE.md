# CLAUDE.md

This project follows **Feature-first Clean Architecture**.

- **Folders, layers, dependencies, feature/EventBus rules, naming, cross-feature ports:** `/agent/architecture.md` (single source of truth).
- **Game design:** `/agent/game_design.md`
- **Author intent, distribution/validation, confidentiality:** `/agent/developer_context.md`
- **Work-level cohesion, SSOT for docs/tools:** `/agent/work_principles.md`

---

## Mandatory workflow

* Before modifying **any** file under `Features/<Name>/` or `Shared/`, read that area’s **`README.md`**. Do not skip.
* When modifying code under `Features/<Name>/` or `Shared/`, update the corresponding **`README.md`**. Do not skip.
* When making design decisions: (1) colocate what changes for the same reason, (2) prefer interfaces over leaking implementation. Check the code first; if a change might violate rules, ask before proceeding.
* When writing or modifying code, follow **`/agent/anti_patterns.md`**. Do not skip.
* Scene-owned features: keep the **README scene contract** current (references, runtime objects, allowed lookups, init order, late-join/reconnect).

---

## Network Feature Standard

네트워크 동기화가 필요한 피처는 아래 구조를 따른다.

### Application Layer

| 클래스 | 역할 |
|---|---|
| `<Feature>UseCases` | 로컬 액션 → 검증 → 네트워크 명령 전송 |
| `<Feature>NetworkEventHandler` | 네트워크 콜백 수신 → 도메인 상태 갱신 → 이벤트 발행 |

### Port Interfaces (Application/Ports)

| 포트 | 역할 |
|---|---|
| `I<Feature>NetworkCommandPort` | 네트워크 명령 전송 (RPC 등) |
| `I<Feature>NetworkCallbackPort` | 네트워크 콜백 수신 |

### Infrastructure Layer

| 클래스 | 역할 |
|---|---|
| `<Feature>NetworkAdapter` | CommandPort + CallbackPort 구현 |

### 동기화 방식

모든 네트워크 데이터는 반드시 아래 세 경로 중 하나를 통해야 한다.

- **연속 데이터** → `OnPhotonSerializeView` (위치, 회전 등 매 프레임 변경되는 값)
- **이산 이벤트** → `RPC` (점프, 스킬 캐스팅, 데미지 등 특정 시점에 발생하는 이벤트)
- **상태 동기화** → `CustomProperties` (팀, 레디, 닉네임 등 현재 상태를 나타내는 값. 늦게 입장한 유저에게 자동 동기화됨)

다른 동기화 방식(PhotonTransformView 등)은 사용하지 않는다.

### Port 정리 원칙

- 네트워크 포트는 **방향별**로 나눈다: `CommandPort` (보내기), `CallbackPort` (받기)
- 포트의 반환 타입/데이터 클래스는 해당 포트와 **같은 파일**에 둔다

### Bootstrap 조립 순서

1. `NetworkAdapter` 참조 획득 (Photon 런타임 Instantiate 프리팹은 Inspector 연결 불가 → GetComponent 허용)
2. `NetworkEventHandler` 생성 (콜백 포트 + EventBus 주입; EventHandler가 생성자에서 EventBus를 직접 구독한다)
3. `UseCases` 생성 (커맨드 포트 + 기타 의존성 주입)
4. Presentation 초기화

---

## Unity MCP Bridge

Unity 에디터 작업이 필요할 때 로컬 HTTP 브리지를 사용한다.

- **MANDATORY**: 브리지를 사용하기 전에 `Assets/Editor/UnityMcp/README.md`를 먼저 읽어라. 엔드포인트 목록, 요청/응답 형식, 주의사항이 기록되어 있다.
- **MANDATORY**: Unity가 플레이 중일 때는 C# 스크립트(특히 `Assets/Editor/**` 브리지 코드)를 수정한 뒤 즉시 반영될 것이라 가정하지 마라. 스크립트/브리지 수정이 필요하면 먼저 플레이를 멈추고, 컴파일 완료를 확인한 다음 다시 테스트를 시작한다.
- 코드: `Assets/Editor/UnityMcp/UnityMcpBridge.cs`
- 주소: `http://127.0.0.1:{port}/` (포트는 `ProjectSettings/UnityMcpPort.txt`에서 읽거나 `/health`로 확인)
- 호출: `powershell -Command "Invoke-RestMethod ..."`
- 필요한 엔드포인트가 없으면 `UnityMcpBridge.cs`에 직접 추가/수정 가능

용도: 씬 조회, 인스펙터 확인, 게임오브젝트 생성/삭제, 컴포넌트 수정, 플레이 시작/정지, **프리팹 조회/수정/컴포넌트 추가** 등 에디터 작업 전반.

---

## Bulk Codemod Checklist

Before applying a bulk replacement or mechanical edit across many files:

1. Define the exact target pattern.
2. Define explicit exclusions.
3. Confirm validator/editor semantics actually match the codemod.
4. Run compile or editor validation immediately after the change.
5. Update README/rule docs if the meaning of the codebase changed.

---

## Rule Priority (on conflict)

1. `architecture.md` (folders, features, dependencies, layers, naming, ports)
2. `anti_patterns.md` (established patterns from refactoring)
3. `event_rules.md` (이벤트 체인 방향, 깊이 제한, 이벤트 vs 직접 호출 판단)
4. `initialization_order.md` (피처 간 초기화 순서, 의존 관계 맵)
5. `state_ownership.md` (CustomProperties 키별 소유권, 동기화 채널 선택)

---

## Agent Collaboration

- 자동화된 교차 에이전트 리뷰/중계 체인은 사용하지 않는다.
- 외부 에이전트 호출을 전제로 한 스크립트, 프롬프트 릴레이, 우회성 오케스트레이션은 프로젝트 규칙에서 제외한다.
- 추가 검토가 필요하면 현재 작업 맥락에서 직접 검토하거나 사람이 명시적으로 범위를 정해 수동으로 진행한다.
