# WebGL Smoke Checklist

> 마지막 업데이트: 2026-04-17
> 상태: active
> doc_id: plans.webgl-smoke-checklist
> role: reference
> owner_scope: WebGL 수동 smoke 절차와 기대 결과
> upstream: plans.progress, plans.account-system
> artifacts: `Build/WebGL`, `artifacts/webgl/`
>
> 생성일: 2026-04-17
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 Phase 10/11의 WebGL 실기 검증을 위한 **수동 smoke 체크리스트**다.

목적은 깊은 테스트가 아니라, 배포 직전 가장 중요한 사용자 여정이 **브라우저에서 끊기지 않는지** 빠르게 확인하는 것이다.

---

## 1. 공통 원칙

- Editor Play Mode 결과와 WebGL 결과를 섞어 기록하지 않는다.
- 각 smoke는 `실행 일시`, `빌드 경로`, `성공/실패`, `실패 지점`, `증거`를 함께 남긴다.
- 실패 시 바로 고치려 하지 말고, 먼저 **어느 단계에서 끊겼는지**를 기록한다.
- `progress.md` 상태 변경은 이 문서의 실기 결과를 기준으로 한다.

---

## 2. 사전 준비

### 빌드 전

- Unity Editor가 compile clean 상태인지 확인
- `AccountConfig`가 실제 WebGL 대상 Firebase 프로젝트를 가리키는지 확인
- Firestore DB와 Auth가 대상 프로젝트에서 활성화되어 있는지 확인
- WebGL 빌드 output 경로를 기록

### 실행 시 확보할 증거

- WebGL 빌드 경로
- 브라우저 콘솔 로그
- Unity player 로그 또는 화면 캡처
- Firestore 문서 생성/변경 여부
- 필요한 경우 Auth 사용자 상태

---

## 3. Smoke A — Garage save/load WebGL

목표:
익명 로그인 후 Garage 변경이 저장되고, 새로고침 또는 재진입 후 같은 내용으로 복원되는지 확인한다.

### 절차

1. 최신 WebGL 빌드를 실행한다.
2. 익명 로그인 완료 후 Lobby/Garage 대시보드 진입을 확인한다.
3. Garage에서 눈에 띄는 변경을 만든다.
   - 슬롯 1의 Frame/Firepower/Mobility를 기본값과 다른 조합으로 변경
4. `Save Draft`를 눌러 저장 성공 피드백을 확인한다.
5. Firestore의 `garage/roster` 문서가 생성 또는 갱신됐는지 확인한다.
6. 브라우저를 새로고침하거나 앱을 완전히 다시 연다.
7. 같은 계정으로 재진입 후 Garage 구성이 저장한 상태와 일치하는지 확인한다.

### 기대 결과

- 저장 직후 UI가 성공 상태로 바뀐다.
- Firestore `garage/roster`가 갱신된다.
- 새로고침 후에도 저장한 Garage가 다시 로드된다.

### 실패 시 기록

- 실패 단계 번호
- 화면에 보인 메시지
- 브라우저 콘솔 에러
- Firestore 문서 존재 여부
- 새로고침 전후 UID 동일 여부

---

## 4. Smoke B — Account delete WebGL

목표:
계정 삭제가 UI confirm에서 끝나지 않고, 실제 Auth/Firestore 정리까지 이어지는지 확인한다.

### 절차

1. 익명 로그인 또는 기존 테스트 계정으로 진입한다.
2. Garage `AccountCard`에서 `Delete Account` 2단계 confirm을 진행한다.
3. 삭제 처리 중 상태 메시지를 확인한다.
4. Firestore의 해당 `accounts/{uid}` 문서가 정리됐는지 확인한다.
5. 삭제 후 앱 상태를 확인한다.
   - 로그아웃 또는 새 익명 계정으로 재시작되는지
6. 다시 진입했을 때 이전 UID가 아닌지 확인한다.

### 기대 결과

- 삭제 요청 후 에러 없이 흐름이 마무리된다.
- 기존 UID의 Firestore 데이터가 정리된다.
- 재진입 시 이전 계정 상태가 남아 있지 않다.

### 실패 시 기록

- confirm 이후 멈춘 단계
- 브라우저 콘솔 에러
- Firebase Auth 삭제 성공/실패 징후
- Firestore 문서 잔존 여부
- 삭제 후 재진입 UID

---

## 5. Smoke C — Google linking WebGL

목표:
익명 계정이 Google 계정으로 linking될 때 UID가 유지되고, UI에 연결 상태가 반영되는지 확인한다.

### 절차

1. 익명 로그인 직후 현재 UID를 기록한다.
2. `Google Sign In` 버튼을 눌러 Google linking을 진행한다.
3. linking 성공 후 `authType`이 `google`로 바뀌는지 확인한다.
4. 현재 UID가 linking 전과 같은지 확인한다.
5. 필요 시 새로고침 후에도 Google linked 상태가 유지되는지 확인한다.

### 기대 결과

- Google 계정 선택 후 linking 성공 메시지가 보인다.
- `authType == google`
- UID가 linking 전후 동일하다.

### 실패 시 기록

- Google popup/redirect 단계에서의 실패 여부
- 브라우저 콘솔 에러
- linking 후 UID 변화 여부
- UI에 남은 상태 메시지

---

## 6. 결과 기록 형식

아래 형식으로 `progress.md` 또는 관련 계획 문서에 남긴다.

```md
- done|blocked: Garage save/load WebGL smoke 1회 실행
  - env: WebGL build `<경로>` / `<브라우저>`
  - result: success|failure
  - observed: `<핵심 관찰 1~2줄>`
  - evidence: `<스크린샷 경로 / 콘솔 로그 / Firestore 확인>`
  - next: `<필요 시 후속 조치>`
```

---

## 7. 완료 판정

아래 세 항목이 각각 최소 1회 이상 성공 또는 실패 원인 명확화 상태가 되면, WebGL 실기 검증은 "막연한 TODO"에서 "관리 가능한 작업"으로 내려온다.

- `Garage save/load WebGL smoke`
- `Account delete WebGL smoke`
- `Google linking WebGL smoke`
