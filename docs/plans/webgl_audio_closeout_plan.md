# WebGL Audio Closeout Plan

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: plans.webgl-audio-closeout
> role: plan
> owner_scope: WebGL audio load/playback and sound settings persistence product smoke closeout
> upstream: ops.acceptance-reporting-guardrails
> artifacts: `artifacts/webgl/audio/`

이 문서는 WebGL 오디오 product smoke의 active owner다.
Account/Garage account acceptance, Firebase Auth/Firestore, Google linking은 현재 별도 active plan이 아니라 WebGL smoke checklist에서 추적한다.
`SoundPlayer` runtime host/template residual은 scene-owned BGM/SFX AudioSource contract로 전환됐으며, WebGL product smoke 성공의 대체 증거로 쓰지 않는다.

## Current Judgment

- 사운드 설정 UI 저장, WebGL 오디오 로드/재생, 브라우저 autoplay 또는 unlock 제약은 같은 WebGL audio closeout 이유로 묶는다.
- Unity compile pass, asset hygiene, account WebGL smoke, SoundPlayer runtime repair를 audio product success로 확장하지 않는다.

## Acceptance

| Item | Required evidence | Closeout |
|---|---|---|
| Audio load WebGL | WebGL build에서 BGM/SFX asset load와 console error 없음 확인 | success / blocked / mismatch |
| Playback unlock | 사용자 입력 뒤 BGM/SFX 재생 시작 또는 브라우저 정책 blocked reason 확인 | success / blocked / mismatch |
| Settings persistence | sound settings UI 변경, 저장, reload 후 유지 확인 | success / blocked / mismatch |
| Runtime audio contract | scene-owned `SoundPlayer` host가 BGM/SFX AudioSource 계약을 만족하고 runtime 생성 repair가 없는지 확인 | success / blocked / mismatch |

## Execution Rule

- Browser/WebGL 실기 evidence 없이 오디오 product acceptance를 success로 올리지 않는다.
- 브라우저 autoplay policy, missing audio asset, hosting MIME/config 문제처럼 실행 환경 때문에 판정할 수 없으면 `blocked`로 남긴다.
- 실행됐지만 설정 저장, reload persistence, 실제 재생 상태가 기대와 다르면 `mismatch`로 남긴다.
- `SoundPlayer` runtime contract 검증은 WebGL product smoke의 대체 증거가 아니다. 둘 중 하나만 통과하면 나머지는 `blocked`로 남긴다.

## Residual

- 사운드 설정 UI 저장 확장과 WebGL 오디오 로드/재생 smoke가 남아 있다.
- SoundPlayer AudioSource/template runtime contract는 scene-owned BGM/SFX AudioSource host로 전환됐다. WebGL product smoke는 여전히 별도 evidence가 필요하다.

owner impact:

- primary: `plans.webgl-audio-closeout`
- secondary: `plans.progress`
- out-of-scope: account auth/cloud acceptance, Firebase setup, broad audio UX redesign, sound asset production

doc lifecycle checked:

- active 유지. WebGL audio acceptance가 success/blocked/mismatch로 닫히고 residual이 이관되면 reference 압축 또는 삭제 후보로 재검토한다.
- plan rereview: clean - WebGL audio product smoke scope and out-of-scope residual routing checked
