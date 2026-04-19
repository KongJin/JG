# 기술부채 감축 실행 계획

> 생성일: 2026-04-17
> 최종 업데이트: 2026-04-18
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 현재 프로젝트의 기술부채를 "무엇이 불편한가" 수준이 아니라, **무엇을 어떤 순서로 줄여야 배포 신뢰도가 올라가는가** 기준으로 정리한 실행 계획이다.

핵심 방향은 아래 세 가지로 고정한다.

- 지금 가장 위험한 부채는 코드 스타일보다 **실제 런타임 검증 공백**이다.
- 우선순위는 새 기능 추가가 아니라 **이미 연결한 경로를 Editor/WebGL에서 재현 가능하게 검증**하는 것이다.
- `완료` 표시는 코드 존재가 아니라 **contract, 테스트, smoke 중 적절한 층에서 재현된 상태**를 기준으로 올린다.

---

## 1. 현재 심각도 스냅샷

점수 기준은 `0 = 건강`, `10 = 심각`이다.

| 영역 | 점수 | 현재 해석 |
|---|---:|---|
| 런타임 검증 / 배포 신뢰도 | 7.9 | WebGL, GameScene loop, 멀티플레이 핵심 경로의 실기 확인이 아직 부족하다 |
| 테스트 안전망 | 7.5 | 최근 EditMode/reflection 테스트는 늘었지만 핵심 흐름 대비 더 보강이 필요하다 |
| 씬 / Inspector wiring | 6.2 | `CodexLobbySceneContract`와 required-field audit로 안정화가 진행됐다 |
| Account / Firebase / WebGL 통합 | 6.6 | Garage save/load, account delete는 진척됐지만 Google linking 실기는 남아 있다 |
| 도구 / 자동화 안정성 | 5.8 | Unity MCP core workflow는 정리됐지만 Play Mode 전환은 간헐적 흔들림이 남는다 |
| 비동기 UI 수명주기 | 6.2 | `async void` 기반 액션이 예외 추적과 취소를 어렵게 만든다 |
| 전역 상태 / 싱글톤 | 5.8 | `SoundPlayer` 같은 전역 상태가 씬 경계를 흐릴 수 있다 |
| 구조 복잡도 / lifecycle seam | 6.1 | 대형 controller/adapter에 임시 복구 seam, smoke 훅, mapping, policy가 함께 쌓여 있다 |
| 문서 최신성 | 3.8 | 핵심 SSOT는 많이 맞춰졌지만 일부 계획 문서는 계속 동기화가 필요하다 |
| 아키텍처 기반 | 3.9 | feature 분리와 SSOT 문화 자체는 비교적 건강하다 |

요약하면, 지금은 "설계 붕괴"보다 **Gameplay/WebGL 실기 검증과 테스트 층 확장이 뒤처진 상태**로 보는 게 맞다.

---

## 2. 용어 정리

이전 메모의 `Garage save/load/delete 실기 smoke`는 표현이 부정확했다. 이 문서에서는 아래처럼 분리해서 쓴다.

- `Garage save/load WebGL smoke`
  - Garage 변경 후 저장되고, 새로고침 또는 재진입 후 다시 복원되는지 확인하는 시나리오
- `Account delete WebGL smoke`
  - 계정 삭제가 실제 Auth/Firestore 정리까지 이어지는지 확인하는 시나리오
- `Google linking WebGL smoke`
  - 익명 계정을 Google 계정으로 연결할 때 UID 유지와 `authType` 반영을 확인하는 시나리오

이 세 가지를 분리하는 이유는, Unity Editor/MCP에서 통과해도 WebGL 브라우저 환경에서는 다음이 달라질 수 있기 때문이다.

- Firebase Auth 토큰 처리
- Firestore 권한 규칙과 네트워크 실패 양상
- WebGL JS 브리지 호출
- 브라우저 새로고침 후 세션 복원

---

## 3. 실행 원칙

- 먼저 사용자 여정이 끊기는 경로를 막는다.
- 런타임 fallback보다 Inspector wiring과 scene contract를 우선한다.
- 검증은 `contract -> EditMode/unit tests -> 얇은 smoke` 순서로 내린다.
- Play Mode smoke와 WebGL smoke를 분리해서 기록한다.
- 도구 성공을 기능 성공으로 간주하지 않는다.
- 설계 판단이 바뀌면 `progress.md`를 같은 턴에 갱신한다.

---

## 4. 우선순위 로드맵

### Track A. 씬 계약 안정화

목표:
Editor 기준으로 Lobby/Garage 관련 씬 계약을 다시 흔들리지 않는 상태로 만든다.

작업:
- `progress.md`에 남아 있는 Inspector wiring TODO를 전수 점검한다.
- 최근 제거한 runtime UI grabbing 이후 누락된 직렬화 참조가 없는지 확인한다.
- `PlacementAreaView`, `DragGhostPrefab`, `AccountConfig`, `LoginLoadingView` 등 TODO 항목을 실제 씬 기준으로 재확인한다.
- 관련 scene contract 문구와 운영 문서를 현재 씬 상태와 맞춘다.

완료 기준:
- Lobby/Garage 핵심 씬에서 missing reference, runtime fallback, name-based lookup이 남지 않는다.
- Play Mode 진입 시 즉시 터지는 wiring 오류가 없다.
- `progress.md`의 관련 TODO가 구체적인 남은 항목만 남도록 줄어든다.
- `CodexLobbySceneContract`와 required-field audit가 canonical structure 검증을 맡고, smoke는 decorative hierarchy에 의존하지 않는다.

예상 순서:
1. `CodexLobbyScene` wiring 재검증
2. 필수 직렬화 참조 누락 수정
3. contract 문서 / `progress.md` 동기화

### Track B. WebGL 핵심 계정/차고 smoke

목표:
Phase 10/11의 가장 큰 리스크인 "브라우저에서 진짜 되는가"를 얕고 빠른 절차로 고정한다.

작업:
- `Garage save/load WebGL smoke`를 체크리스트로 문서화한다.
- `Account delete WebGL smoke`를 별도 체크리스트로 문서화한다.
- `Google linking WebGL smoke`를 별도 체크리스트로 문서화한다.
- 각 smoke는 "시도 순서 / 기대 결과 / 실패 시 수집할 로그"까지 남긴다.

최소 시나리오:
- Garage save/load
  - 익명 로그인
  - Garage 변경
  - 저장
  - 브라우저 새로고침 또는 재진입
  - 저장 내용 복원 확인
- Account delete
  - 계정 삭제 2단계 confirm
  - Firestore 문서 정리 확인
  - Auth 삭제 후 재진입 시 새 계정으로 시작되는지 확인
- Google linking
  - 익명 로그인 상태에서 Google 버튼 클릭
  - linking 성공 후 UID 유지 확인
  - `authType == google` 반영 확인

완료 기준:
- 세 smoke가 모두 문서화되어 있고, 각 결과가 `progress.md`에 기록된다.
- 적어도 한 번은 실제 WebGL 빌드 기준으로 성공/실패가 구분된 결과가 남는다.
- Phase 10/11 상태 표기는 이 결과를 기준으로만 올린다.

### Track C. 회귀 테스트 확대

목표:
지금의 약한 테스트 안전망을 "핵심 경로 방어 가능" 수준까지 끌어올린다.

작업:
- Garage 저장/복원 계약을 검증하는 EditMode 테스트를 추가한다.
- Account 규칙성 테스트를 추가한다.
  - 닉네임 cooldown
  - 계정 삭제 순서 관련 순수 로직
  - `UserSettings` 소비 경로
- Lobby/Garage 규칙은 smoke에서 tests/contract로 계속 내린다.
  - Ready unlock / relock
  - save-restores-ready
  - room name / difficulty validation
  - initial energy validation
- 반사 기반 테스트만이 아니라 실제 타입 계약을 직접 검증하는 테스트도 늘린다.
- Unity Test Runner에 잡히는 경로와 repo 루트 `Tests/` 경로를 분리해서 문서화한다.

권장 목표:
- 현재 소수의 핵심 테스트에서 `12~15개` 수준의 회귀 방어선으로 확장
- 적어도 Garage, Account, Lobby 저장 경로에 "깨지면 바로 보이는" 테스트 확보

완료 기준:
- Garage 저장/복원과 Account 핵심 규칙에 대한 자동 검증이 추가된다.
- 새로 찾은 버그가 smoke에만 의존하지 않고 일부는 테스트로 재현 가능해진다.

### Track D. 비동기 UI / 도구 안정화

목표:
디버깅이 어려운 경로를 줄이고, MCP를 "보조 도구"로 더 안정적으로 쓴다.

작업:
- UI 버튼 핸들러의 `async void`를 얇은 wrapper + 내부 `Task` 패턴으로 정리한다.
- 실패 메시지와 취소/중복 클릭 방지를 명확히 한다.
- Unity MCP stable route 기준 `workflow gate -> page-switch smoke -> feature smoke` 루프를 유지한다.
- MCP hang 재현 조건과 우회 절차를 `tools/unity-mcp/README.md` 또는 관련 문서에 남긴다.

완료 기준:
- 계정/저장 UI 경로의 예외 추적이 쉬워진다.
- MCP 불안정이 있어도 수동 smoke 절차가 끊기지 않는다.

### Track E. 구조 복잡도 / lifecycle seam 제거

목표:
큰 클래스 안에 응급 seam처럼 굳은 책임과 scene-crossing 정적 의존을 분리해, `contract -> direct test -> thin smoke` 순서로 다시 검증 가능하게 만든다.

작업:
- `FirestoreRestPort`, `FirebaseAuthRestAdapter`, `GaragePageController`, `GameSceneRoot`의 helper/mapping/transport 책임을 분리한다.
- `AuthTokenProvider` 같은 정적 세션 접근을 injected session access로 교체한다.
- `PlayerSetup.LocalArrived/RemoteArrived`, `EnemySetup.EnemyArrived`, `BattleEntityArrived` 같은 gameplay arrival seam은 scene-local registry 또는 명시적 bootstrap으로 내린다.
- `SoundPlayer.Instance` 직접 참조는 금지하고, runtime config 기반 host factory나 명시적 주입 경로만 허용한다.

완료 기준:
- 대형 클래스는 orchestration 위주로 축소되고, helper는 별도 파일로 이동한다.
- production code에서 scene-crossing 정적 seam이 줄어든다.
- 문서가 임시 seam을 현재 정답처럼 권장하지 않는다.
- `SoundPlayer` 같은 런타임 host는 전역 static instance 없이도 찾고 초기화할 수 있어야 한다.

### Track F. 신뢰도 이후의 UX 폴리시

목표:
신뢰도 이슈를 막은 뒤 Garage 대시보드의 2차 polish를 진행한다.

작업:
- `garage_ui_ux_improvement_plan.md` 순서대로 `Rooms`, 프리뷰 빈 상태, 결과 패널 밀도를 정리한다.
- 계정 카드와 Garage 카드 간 정보 우선순위를 다시 맞춘다.
- 캡처 기반 before/after를 남긴다.

완료 기준:
- 주요 카드가 "동작은 하지만 거칠다" 상태를 벗어난다.
- 시각 polish가 기능 검증을 가리지 않는 순서로 진행된다.

---

## 5. 이번 주 실행 순서

### 1순위

- Track A의 씬 계약 재검증
- `Garage save/load WebGL smoke` 체크리스트 작성 및 1회 실행

### 2순위

- `Account delete WebGL smoke` 실행
- `Google linking WebGL smoke` 실행

### 3순위

- Track E의 구조 복잡도 / lifecycle seam 제거
- Garage / Account 회귀 테스트 확대
- `async void` 정리와 MCP 안정화 보강

---

## 6. 첫 번째 완료 묶음

아래 네 항목이 끝나면 기술부채 심각도는 체감상 가장 크게 내려간다.

1. Lobby/Garage 씬 wiring TODO 정리
2. `Garage save/load WebGL smoke` 1회 성공 또는 실패 원인 명확화
3. `Account delete WebGL smoke` 1회 성공 또는 실패 원인 명확화
4. `Google linking WebGL smoke` 1회 성공 또는 실패 원인 명확화

여기까지 끝나면 "코드는 있는데 믿을 수 없는 상태"에서 "문제가 어디 있는지 아는 상태"로 넘어간다.

---

## 7. 문서 동기화 규칙

- 공식 진행 상태는 항상 [`progress.md`](./progress.md)를 우선한다.
- 이 문서는 우선순위와 실행 순서를 설명하는 계획 문서다.
- 오래된 [`../tech_debt_review.md`](../tech_debt_review.md)는 참고용 이력으로 보고, 현재 우선순위 판단은 이 문서와 `progress.md`를 따른다.
- 각 Track의 첫 실기 결과가 나오면 이 문서보다 먼저 `progress.md`를 갱신한다.
