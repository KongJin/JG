# Account Garage WebGL Closeout Plan

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: plans.account-garage-webgl-closeout
> role: plan
> owner_scope: Account/Garage WebGL product acceptance, Google linking, settings/accessibility residual closeout
> upstream: playtest.webgl-smoke-checklist, ops.firebase-hosting, ops.acceptance-reporting-guardrails
> artifacts: `Build/WebGL`, `artifacts/webgl/account/`

이 문서는 Account/Garage WebGL 실기 acceptance의 active residual owner다.
Firestore/Garage code path, Google linking code path, WebGL smoke 절차의 본문은 각각 구현 코드와 reference checklist를 따른다.

## Current Judgment

- Firestore/Garage 핵심 경로와 Google linking code path는 존재하지만 WebGL 실기 evidence가 아직 부족하다.
- WebGL save/load, settings/accessibility, save action 도달성, Firebase Console 설정 확인은 같은 product acceptance 이유로 함께 닫는다.
- Google anonymous -> Google linking UID 유지와 linked 상태 persistence는 별도 성공으로 분리하되, 같은 WebGL account closeout owner에서 추적한다.
- Audio WebGL smoke는 account acceptance와 성공 기준이 다르므로 별도 audio owner lane으로 분리한다.

## Acceptance

| Item | Required evidence | Closeout |
|---|---|---|
| Garage save/load WebGL | WebGL build에서 anonymous login, Garage edit, `Save Draft`, Firestore update, reload restore 확인 | success / blocked / mismatch |
| Account delete WebGL | WebGL build에서 delete confirm 이후 Auth/Firestore cleanup과 재진입 UID 확인 | success / blocked / mismatch |
| Google linking WebGL | WebGL build에서 anonymous UID 기록, Google linking, same UID, `authType=google`, reload persistence 확인 | success / blocked / mismatch |
| Settings/accessibility | WebGL build에서 settings interaction, save action 도달성, keyboard/touch basic path 확인 | success / blocked / mismatch |

## Execution Rule

- Manual smoke 절차와 결과 기록 형식은 [`../playtest/webgl_smoke_checklist.md`](../playtest/webgl_smoke_checklist.md)를 따른다.
- Editor Play Mode, Unity compile pass, UI Toolkit preview evidence를 WebGL product acceptance success로 확장하지 않는다.
- Firebase Console 설정, browser popup/redirect, hosting build 환경 때문에 판정할 수 없으면 `blocked`로 남긴다.
- 실행됐지만 UID, Firestore state, reload persistence, visible UI state가 기대와 다르면 `mismatch`로 남긴다.

## Residual

- `Garage save/load WebGL`, `Account delete WebGL`, `Google linking WebGL`, `Settings/accessibility` smoke가 남아 있다.
- Audio WebGL residual은 별도 audio owner lane이 소유한다.

owner impact:

- primary: `plans.account-garage-webgl-closeout`
- secondary: `plans.progress`, `playtest.webgl-smoke-checklist`
- out-of-scope: Firebase project policy 변경, account code/API 변경, UI Toolkit candidate authoring, audio runtime implementation

doc lifecycle checked:

- active 유지. WebGL account acceptance가 success/blocked/mismatch로 닫히고 residual이 이관되면 reference 압축 또는 삭제 후보로 재검토한다.
- plan rereview: clean
