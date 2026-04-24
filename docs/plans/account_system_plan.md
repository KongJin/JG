# 계정 시스템 복구 계획

> 마지막 업데이트: 2026-04-25
> 상태: active
> doc_id: plans.account-system
> role: plan
> owner_scope: 계정과 차고 복구 작업의 현재 범위와 우선순위
> upstream: plans.progress, design.game-design
> artifacts: `Assets/Scripts/Features/Account/`, `Assets/Scripts/Features/Garage/`
>
> 생성일: 2026-04-11
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 Account Feature의 "기능 추가 계획"이 아니라, 이미 들어간 계정/차고 연동 코드와 실제 동작 사이의 간극을 메우는 **복구 SSOT**다.

핵심 메시지는 아래 세 가지로 고정한다.

- 현재 계정 시스템은 **복구가 많이 진행됐지만**, Google linking 실기와 설정 소비 경로가 아직 남아 있다.
- 우선순위는 새 기능 추가가 아니라 **이미 약속한 흐름을 실제로 동작하게 복구**하는 것이다.
- Garage와 Account의 장기 SSOT는 **Firestore + Photon 이중 경로**를 유지하되, 지금은 누락된 연결을 먼저 메운다.

---

## 최상위 남은 문제 목록

### 1. Google linking WebGL 실기 검증 미완료
- Google linking 코드와 WebGL 브리지는 존재하지만, 실제 브라우저에서 `UID 유지 + authType == google`은 아직 검증되지 않았다.

### 2. Settings 소비 경로가 부분 완료 상태
- `masterVolume`은 런타임 소비 경로가 연결됐지만, `language`와 나머지 설정 UI/저장 흐름은 아직 얇다.

### 3. WebGL 회귀 검증은 여전히 얇다
- `Garage save/load`, `Account delete`는 실기 1차를 통과했지만, 반복 회귀를 막는 acceptance layer는 더 늘려야 한다.

### 4. 테스트/문서 기준선이 구현 속도를 따라가야 한다
- Account/Garage 규칙은 smoke만으로 충분하지 않아서, EditMode 테스트와 WebGL 체크리스트를 계속 같이 키워야 한다.

### 5. Set B Garage 이후 shared validation ownership 정리
- `Set B Garage` prefab lane은 `source freeze -> execution contracts -> prefab target -> fresh review evidence`까지를 직접 닫는다.
- 그 이후 Garage 저장 접근성, settings overlay interaction, Garage save/load WebGL validation은 이 문서가 소유하는 shared `Account/Garage` validation lane으로 계속 관리한다.
- 이 항목들은 `Garage-only smoke`를 새로 만드는 것이 아니라, 기존 Account/Garage validation backlog의 일부로 본다.

---

## 1. 현재 실제 상태

### 구현된 것
- Firebase Auth REST 기반 익명 로그인과 Google `signInWithIdp` 호출 코드가 존재한다.
- `AccountSetup`, `LoginLoadingView`, `AccountSettingsView` 골격과 `CodexLobbyScene` wiring 코드가 존재한다.
- Firestore용 `IAccountDataPort`와 `FirestoreRestPort`가 존재한다.
- Garage 저장 시 Photon `CustomProperties["garageRoster"]` 동기화 경로는 유지된다.
- Garage Firestore 저장/로드와 Photon roster handoff가 실제 runtime 경로에 연결됐다.
- 익명 로그인 자동 재시도와 계정 삭제 REST 형식 수정이 반영됐다.
- WebGL 익명 세션 지속성과 `Garage save/load`, `Account delete` 실기 smoke 1차가 통과했다.

### 아직 실제로 동작한다고 볼 수 없는 것
- Google linking WebGL smoke
- 설정 저장/소비 경로의 마무리
- 반복 회귀를 막는 추가 WebGL smoke와 테스트 확장

### 현재 씬에서 실제 소비하는 데이터
- `Profile`
  - 사용 중: `uid`, `displayName`, `authType`
- `Garage`
  - Firestore와 Photon handoff를 통해 실제 소비 중
- `Settings`
  - `masterVolume`은 소비 중, 나머지는 후속 wiring 여지 있음
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
- Google linking은 WebGL에서 UID 유지까지 확인된다.
- Account/Garage 핵심 규칙은 smoke 외에 EditMode/Test Runner 경로로도 방어된다.

---

## 3. 현재 구현 기준

### Firestore 저장 어댑터
- `FirestoreRestPort`는 Garage 저장/로드 경로를 실제 runtime에서 사용한다.
- Garage 저장은 Firestore + Photon custom properties를 같이 갱신한다.
- 과거의 `AuthTokenProvider` 정적 우회는 제거 대상으로 본다.
- 현재 기준 세션 접근은 `AccountSetup -> FirebaseAuthRestAdapter -> FirestoreRestPort`의 injected session access로 유지한다.

### Garage 초기화 경로
- `InitializeGarageUseCase`는 Firestore 우선 복원과 committed roster 재동기화를 담당한다.
- 새 씬 인스턴스는 기존 Photon player properties에서 roster/ready cache를 hydrate할 수 있어야 한다.
- 현재 active host scene 부재 때문에 Set B prefab lane에서 Garage 전용 runtime smoke를 따로 만들지는 않는다.

### 로컬 JSON 처리
- `GarageJsonPersistence`는 즉시 삭제하지 않는다.
- 역할을 "주 저장소"에서 "fallback 캐시"로 낮춘다.
- Firestore 저장 성공 시 로컬 JSON도 함께 갱신해 오프라인/장애 복구용으로 사용한다.

### 로그인 재시도
- 최대 3회 자동 재시도 기준이 구현돼 있다.
- 남은 것은 실기 회귀를 더 쌓는 일이지, 기본 재시도 경로를 다시 설계하는 일은 아니다.

### 계정 삭제
- `FirebaseAuthRestAdapter.DeleteAccount`는 `idToken` body 형식을 사용한다.
- 삭제 순서는 `Firestore -> Firebase Auth -> local sign-out -> scene reload`를 유지한다.
- WebGL `Account delete` smoke 1차는 이미 성공했다.

### 닉네임 규칙
- `AccountProfile`에 `lastNicknameChangeUnixMs` 필드를 추가한다.
- 최초 생성 후 기본 닉네임 상태에서는 즉시 1회 변경을 허용한다.
- 변경 성공 시 `lastNicknameChangeUnixMs`를 현재 시각으로 저장한다.
- 이후 제한은 `createdAtUnixMs`가 아니라 `lastNicknameChangeUnixMs` 기준으로 계산한다.

### 계정 로드 소비 경로
- `LoadAccountUseCase`는 로그인 성공 후 한 번 호출되도록 `LobbySetup`에 연결한다.
- 로그인 성공 직후 가져온 `Profile`, `Garage`, `Settings`, `Stats` 중 실제 소비 경로와 미소비 경로를 계속 분리해서 본다.
- 현재 즉시 소비 대상은 `Profile`, `Garage`, 일부 `Settings(masterVolume)`이다.

### 테스트 정리
- `Tests/`와 Unity Test Runner 경로는 계속 구분해서 관리한다.
- Account/Garage 규칙은 smoke만이 아니라 EditMode/Test Runner로도 늘려 간다.
- Set B 이후 shared validation 최소 항목은 아래로 본다.
  - Garage save action이 실제 사용자 흐름에서 도달 가능함
  - Garage settings overlay open -> close가 유지됨
  - Garage save/load WebGL validation이 current Set B UI 기준으로 계속 유효함

### 기본값과 가정
- 이번 복구는 기능 확장이 아니라 미연결 경로 복구다.
- 로컬 JSON은 폐기하지 않고 fallback으로 남긴다.
- WebGL 실기 테스트는 여전히 최종 게이트다.
- 문서에는 "코드 존재"와 "실동작 검증 완료"를 명확히 구분해 쓴다.
- 순환 의존 회피를 이유로 gameplay/runtime 경로에 정적 토큰 제공자를 다시 도입하지 않는다.
- Garage 전용 runtime host, Garage 전용 smoke entry, Garage 전용 result artifact는 이번 recovery 기준으로 추가하지 않는다.

---

## 4. 테스트 계획

아래 시나리오는 복구 완료 판단용 acceptance criteria다.

| 시나리오 | 기대 결과 |
|---|---|
| 익명 로그인 1회 실패 | 자동 재시도 후 로비 진입 |
| 익명 로그인 3회 연속 실패 | 에러 패널 + 수동 재시도 버튼 노출 |
| Garage 저장 | Firestore 문서 + Photon `CustomProperties` 동시 갱신 |
| 앱 재시작 | Firestore 우선 복원, Firestore 실패 시 로컬 JSON fallback |
| Garage settings interaction | settings overlay open -> close가 current Set B UI 기준으로 유지 |
| Google linking | UID 유지 + `authType == "google"` 반영 |
| 계정 삭제 | Firestore 문서 삭제 후 Firebase Auth 삭제 성공 |
| 닉네임 변경 | 변경 직후 재변경 차단, cooldown 이후 재허용 |
| 테스트 연결성 | Unity compile check 외에 계정/Garage 테스트가 Test Runner에 잡힘 |

### 검증 원칙
- 로컬 컴파일 성공만으로 완료 판정을 하지 않는다.
- WebGL 실기 테스트 전에는 "코드 경로 존재" 상태로만 표시한다.
- Phase 10/11 완료 표기는 문서가 아니라 smoke 결과를 기준으로 올린다.
- Set B prefab lane의 direct success와 shared `Account/Garage` validation 미완료는 같은 상태로 뭉뚱그려 보고하지 않는다.

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
