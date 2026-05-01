# WebGL Audio Closeout Plan

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: plans.webgl-audio-closeout
> role: plan
> owner_scope: WebGL audio load/playback and sound settings persistence product smoke closeout
> upstream: ops.acceptance-reporting-guardrails
> artifacts: `artifacts/webgl/audio/`

이 문서는 WebGL 오디오 product smoke의 active owner다.
Account/Garage account acceptance, Firebase Auth/Firestore, Google linking은 현재 별도 active plan이 아니라 `plans.progress` WebGL account residual과 WebGL smoke checklist에서 추적한다.
`SoundPlayer` runtime host/template residual은 technical-debt recurrence reference gate로 보되, 실행이 열리면 해당 feature/runtime owner pass로 분리한다.

## Current Judgment

- 사운드 설정 UI 저장, WebGL 오디오 로드/재생, 브라우저 autoplay 또는 unlock 제약은 같은 WebGL audio closeout 이유로 묶는다.
- Unity compile pass, asset hygiene, account WebGL smoke, SoundPlayer runtime repair를 audio product success로 확장하지 않는다.

## Acceptance

| Item | Required evidence | Closeout |
|---|---|---|
| Audio load WebGL | WebGL build에서 BGM/SFX asset load와 console error 없음 확인 | success / blocked / mismatch |
| Playback unlock | 사용자 입력 뒤 BGM/SFX 재생 시작 또는 브라우저 정책 blocked reason 확인 | success / blocked / mismatch |
| Settings persistence | sound settings UI 변경, 저장, reload 후 유지 확인 | success / blocked / mismatch |

## Execution Rule

- Browser/WebGL 실기 evidence 없이 오디오 product acceptance를 success로 올리지 않는다.
- 브라우저 autoplay policy, missing audio asset, hosting MIME/config 문제처럼 실행 환경 때문에 판정할 수 없으면 `blocked`로 남긴다.
- 실행됐지만 설정 저장, reload persistence, 실제 재생 상태가 기대와 다르면 `mismatch`로 남긴다.

## Residual

- 사운드 설정 UI 저장 확장과 WebGL 오디오 로드/재생 smoke가 남아 있다.
- SoundPlayer AudioSource/template residual은 `plans.technical-debt-recurrence-prevention` reference gate에서 판단하고, 실행이 필요하면 runtime cleanup pass로 연다.

owner impact:

- primary: `plans.webgl-audio-closeout`
- secondary: `plans.progress`
- out-of-scope: account auth/cloud acceptance, Firebase setup, broad audio UX redesign, sound asset production

doc lifecycle checked:

- active 유지. WebGL audio acceptance가 success/blocked/mismatch로 닫히고 residual이 이관되면 reference 압축 또는 삭제 후보로 재검토한다.
- plan rereview: clean - WebGL audio product smoke scope and out-of-scope residual routing checked
