# 계정 시스템 복구 계획

> 생성일: 2026-04-11
> 최종 업데이트: 2026-04-17
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 Account Feature의 "기능 추가 계획"이 아니라, 이미 들어간 계정/차고 연동 코드와 실제 동작 사이의 간극을 메우는 **복구 SSOT**다.

핵심 메시지는 아래 세 가지로 고정한다.

- 현재 계정 시스템은 **컴파일은 통과하지만**, 클라우드 저장/복원/삭제/재시도/테스트 경로가 미완성이다.
- 우선순위는 새 기능 추가가 아니라 **이미 약속한 흐름을 실제로 동작하게 복구**하는 것이다.
- Garage와 Account의 장기 SSOT는 **Firestore + Photon 이중 경로**를 유지하되, 지금은 누락된 연결을 먼저 메운다.

---

## 최상위 문제 목록

### 1. Garage Firestore 저장 경로 미연결
- `GarageSetup`는 `IAccountDataPort`를 `SaveRosterUseCase.ICloudGaragePort`로 캐스팅해 주입하지만, 실제 구현체가 이 인터페이스를 구현하지 않아 항상 `null`이 들어간다.
- 결과적으로 `SaveRosterUseCase`의 "클라우드 저장 + Photon 동기화" 중 **Photon 동기화만 실행**된다.

### 2. Garage 복원 경로 미작동
- `InitializeGarageUseCase`는 현재 로컬 JSON만 읽는다.
- 하지만 로컬 JSON 저장도 실질적으로 연결되지 않았고, Firestore 로드 결과도 Garage 초기화에 연결되지 않았다.

### 3. 로그인 실패 자동 재시도 미구현
- `LoginLoadingView`는 실패 시 문구만 `Retrying...`로 바꾸고 실제 로그인 재호출은 하지 않는다.
- 문서에는 자동 재시도 3회가 적혀 있지만, 현재 코드는 **첫 실패에서 멈출 수 있다.**

### 4. Firebase Auth 계정 삭제 REST 형식 오류
- `accounts:delete`는 요청 body에 `idToken`을 넣는 형태가 기준이다.
- 현재 어댑터는 빈 JSON과 `Authorization` 헤더를 사용하고 있어 실기에서 실패할 가능성이 높다.

### 5. 닉네임 월 1회 제한 규칙 불완전
- 현재 규칙은 `createdAtUnixMs` 기준이라 "계정 생성 후 30일 이내"만 제한되고, 그 이후에는 사실상 무제한 변경이 가능하다.
- 월 1회 제한은 `lastNicknameChangeUnixMs` 기준으로 바꿔야 한다.

---

## 1. 현재 실제 상태

### 구현된 것
- Firebase Auth REST 기반 익명 로그인과 Google `signInWithIdp` 호출 코드가 존재한다.
- `AccountSetup`, `LoginLoadingView`, `AccountSettingsView` 골격과 `CodexLobbyScene` wiring 코드가 존재한다.
- Firestore용 `IAccountDataPort`와 `FirestoreRestPort`가 존재한다.
- Garage 저장 시 Photon `CustomProperties["garageRoster"]` 동기화 경로는 유지된다.

### 아직 실제로 동작한다고 볼 수 없는 것
- Garage Firestore 저장
- Garage Firestore 우선 복원
- 로그인 실패 자동 재시도
- Firebase Auth 계정 삭제
- 닉네임 월 1회 제한
- stale 상태인 `Tests/Garage/Domain/GarageRosterTests.cs`
- `Tests/`가 Unity compile check와 별개로 관리되는 구조

### 현재 씬에서 실제 소비하는 데이터
- `Profile`
  - 사용 중: `uid`, `displayName`, `authType`
- `Garage`
  - 의도는 사용 중이지만, 현재는 Firestore 로드 결과가 Garage 초기화에 연결되지 않았다.
- `Settings`
  - 모델은 존재하지만 현재 Lobby/Garage에서 실소비 경로가 약하다.
- `Stats`
  - 모델은 존재하지만 현재 로그인 직후 실소비 경로가 없다.

---

## 2. 복구 목표와 성공 기준

### 복구 목표
- Account와 Garage 사이의 저장/복원/삭제 경로를 실제 동작하도록 연결한다.
- 문서의 "완료" 표현을 실제 코드와 맞춘다.
- stale 테스트와 미연결 테스트 경로를 정리해 이후 리팩터링의 안전망을 회복한다.

### 성공 기준
- Garage 저장 시 Firestore 문서와 Photon `CustomProperties`가 모두 갱신된다.
- 앱 재시작 시 Garage는 Firestore 우선으로 복원되고, Firestore 실패 시 로컬 JSON fallback이 동작한다.
- 익명 로그인 실패 시 최대 3회 자동 재시도 후 에러 패널이 뜬다.
- 계정 삭제는 Firestore 문서 삭제 후 Firebase Auth 삭제까지 성공한다.
- 닉네임 변경은 성공 시 cooldown timestamp를 저장하고, 다음 변경은 cooldown 이후에만 허용된다.
- `GarageRosterTests`는 현행 `GarageRoster` API 기준으로 갱신되고, 최소한 Unity Test Runner에 연결할 작업 항목이 명시된다.

---

## 3. 구현 변경 계획

### Firestore 저장 어댑터
- `FirestoreRestPort`는 `SaveRosterUseCase.ICloudGaragePort`를 직접 구현한다.
- `SaveGarageAsync(GarageRoster roster)` 내부에서 현재 UID와 ID 토큰은 `AuthTokenProvider`로 조회한다.
- `IAccountDataPort.SaveGarage`와 `IAccountDataPort.LoadGarage`의 `object` 타입은 `GarageRoster`로 좁힌다.
- Firestore 문서 모델은 유지하되, Garage 문서는 `GarageRoster` 직렬화/역직렬화가 실제로 가능한 형태로 고정한다.

### Garage 초기화 경로
- `InitializeGarageUseCase`는 "Firestore 우선, 실패 시 로컬 JSON fallback"으로 바꾼다.
- 장기 SSOT는 Firestore다.
- 로컬 JSON은 복구용 보조 경로이며, Firestore가 정상일 때는 이를 덮어써 최신 캐시로 유지한다.

### 로컬 JSON 처리
- `GarageJsonPersistence`는 즉시 삭제하지 않는다.
- 역할을 "주 저장소"에서 "fallback 캐시"로 낮춘다.
- Firestore 저장 성공 시 로컬 JSON도 함께 갱신해 오프라인/장애 복구용으로 사용한다.

### 로그인 재시도
- `LoginLoadingView`는 최대 3회 자동 재시도를 전제로 동작한다.
- 각 재시도 사이 대기는 1초로 고정한다.
- 재시도 실행 주체는 `LobbySetup`의 로그인 호출을 다시 트리거하는 `await` 기반 재호출 경로다.
- 3회 실패 후에는 에러 패널과 수동 재시도 버튼을 노출한다.

### 계정 삭제
- `FirebaseAuthRestAdapter.DeleteAccount`는 `{"idToken":"..."}` body 형식으로 수정한다.
- 삭제 순서는 Firestore 문서 삭제 후 Firebase Auth 삭제를 유지한다.
- Firestore 삭제와 Auth 삭제의 성공/실패 로그는 구분해서 남긴다.

### 닉네임 규칙
- `AccountProfile`에 `lastNicknameChangeUnixMs` 필드를 추가한다.
- 최초 생성 후 기본 닉네임 상태에서는 즉시 1회 변경을 허용한다.
- 변경 성공 시 `lastNicknameChangeUnixMs`를 현재 시각으로 저장한다.
- 이후 제한은 `createdAtUnixMs`가 아니라 `lastNicknameChangeUnixMs` 기준으로 계산한다.

### 계정 로드 소비 경로
- `LoadAccountUseCase`는 로그인 성공 후 한 번 호출되도록 `LobbySetup`에 연결한다.
- 로그인 성공 직후 가져온 `Profile`, `Garage`, `Settings`, `Stats` 중 실제 씬에서 사용하는 항목과 미사용 항목을 분리해 다룬다.
- 현재 즉시 소비 대상은 `Profile`과 `Garage`이며, `Settings`/`Stats`는 후속 wiring 전까지 "로드는 하되 미소비 가능" 상태로 문서화한다.

### 테스트 정리
- `Tests/Garage/Domain/GarageRosterTests.cs`는 현행 `GarageRoster` API 기준으로 전면 갱신한다.
- 현재 `Tests/`는 Unity compile check에 포함되지 않는 사실을 유지 문서에 명시한다.
- 복구 범위에는 "최소한 Unity Test Runner에 계정/Garage 관련 테스트를 연결하는 작업"을 포함한다.

### 기본값과 가정
- 이번 복구는 기능 확장이 아니라 미연결 경로 복구다.
- 로컬 JSON은 폐기하지 않고 fallback으로 남긴다.
- WebGL 실기 테스트는 여전히 최종 게이트다.
- 문서에는 "코드 존재"와 "실동작 검증 완료"를 명확히 구분해 쓴다.

---

## 4. 테스트 계획

아래 시나리오는 복구 완료 판단용 acceptance criteria다.

| 시나리오 | 기대 결과 |
|---|---|
| 익명 로그인 1회 실패 | 자동 재시도 후 로비 진입 |
| 익명 로그인 3회 연속 실패 | 에러 패널 + 수동 재시도 버튼 노출 |
| Garage 저장 | Firestore 문서 + Photon `CustomProperties` 동시 갱신 |
| 앱 재시작 | Firestore 우선 복원, Firestore 실패 시 로컬 JSON fallback |
| Google linking | UID 유지 + `authType == "google"` 반영 |
| 계정 삭제 | Firestore 문서 삭제 후 Firebase Auth 삭제 성공 |
| 닉네임 변경 | 변경 직후 재변경 차단, cooldown 이후 재허용 |
| `GarageRosterTests` | 현행 API 기준으로 통과 |
| 테스트 연결성 | Unity compile check 외에 계정/Garage 테스트가 Test Runner에 잡힘 |

### 검증 원칙
- 로컬 컴파일 성공만으로 완료 판정을 하지 않는다.
- WebGL 실기 테스트 전에는 "코드 경로 존재" 상태로만 표시한다.
- Phase 10/11 완료 표기는 문서가 아니라 smoke 결과를 기준으로 올린다.

---

## 5. 문서/SSOT 동기화 규칙

- Account 관련 공식 진행 상태는 항상 [`progress.md`](./progress.md)를 먼저 갱신한다.
- 이 문서는 "무엇을 복구해야 하는가"에 집중하고, 세부 완료/미완료 상태는 `progress.md`와 일치해야 한다.
- "완료" 표현은 반드시 아래 조건을 충족할 때만 사용한다.
  - 코드가 연결되어 있고
  - 해당 경로의 smoke 또는 테스트가 존재하며
  - 문서에 남은 blocker가 없다
- Google 로그인은 "코드가 있다"와 "실동작 검증 완료"를 분리해 기록한다.
- Garage/Account 저장 경로에 설계 판단이 바뀌면 이 문서와 `progress.md`를 같은 턴에 같이 고친다.
