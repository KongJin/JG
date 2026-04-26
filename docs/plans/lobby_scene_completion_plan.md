# LobbyScene 완성도 정리 계획

> 마지막 업데이트: 2026-04-25
> 상태: reference
> doc_id: plans.lobby-scene-completion
> role: plan
> owner_scope: `Assets/Scenes/LobbyScene.unity`의 초기 표시 상태, Lobby/Garage visual pass, BattleScene 전환 연결 closeout
> upstream: plans.progress, plans.lobby-scene-runtime, ops.unity-ui-authoring-workflow
> artifacts: `Assets/Scenes/LobbyScene.unity`, `artifacts/unity/lobby-scene-*.png`
>
> 진행 상황 SSOT: [`progress.md`](./progress.md)

이 문서는 `LobbyScene`이 "조립됨" 상태에서 실제 사용 가능한 첫 화면으로 닫히기까지의 남은 작업만 다룬다.
씬 생성과 runtime wiring 조립 이력은 [`lobby_scene_runtime_plan.md`](./lobby_scene_runtime_plan.md)가 맡고, 현재 우선순위는 [`progress.md`](./progress.md)를 따른다.

## 현재 확인 결과

- `Assets/Scenes/LobbyScene.unity`는 존재하고 Build Settings 첫 번째 enabled scene으로 등록되어 있다.
- Unity compile check는 `ERRORS: 0`이다. 현재 dotnet warning은 기존 UnityMCP model 미할당 필드 경고로 분리한다.
- `Tools/Validate Required Fields` 실행 후 console error는 0이다.
- Play Mode 진입 후 console error는 0이다.
- `/LobbyCanvas/LobbyGarageNavBar/GarageTabButton` invoke 후 `GaragePageRoot`가 active로 전환된다.
- 최신 clean capture는 `artifacts/unity/lobby-scene-lobby-tab-clean.png`, `artifacts/unity/lobby-scene-garage-tab-clean.png`다.
- `AccountSettingsView`, create/detail/error overlays, login loading overlay는 scene default inactive로 정리됐다.
- `SetCLoginLoadingOverlayRoot`는 runtime `Show/Hide/ShowError`에서 root active state를 직접 관리한다.
- `DefaultGameSceneName`과 Lobby scene-load target은 `BattleScene` 기준으로 정리됐다.
- 2차 pass에서 Garage 기본 tab의 preview/result/right-rail 기본 노출을 제거해 roster + part editor + bottom actions 중심으로 정리했다.
- Garage polish pass에서 선택 슬롯 full-cover border, slot/selector text overlap, mobile tab bracket labels, 깨져 보이던 saved-state copy를 정리했다.
- 남은 visual residual은 LobbyScene blocker가 아니라 `Set B Garage` final fidelity judgment다.

## 목표

- Play Mode 첫 진입 시 하나의 명확한 Lobby 화면만 보이게 한다.
- account settings, login loading, room detail, create room, error dialog는 필요한 상태에서만 보이게 한다.
- Lobby/Garage tab 전환이 시각적으로도 겹침 없이 읽히게 한다.
- 시작 게임 전환 후보를 `BattleScene` 기준으로 정리한다.
- functional smoke pass와 visual pass를 분리해서 closeout한다.

## 제외 범위

- 새 Stitch source 생성
- 기존 Stitch prefab 삭제 또는 이동
- Account/Garage 저장 로직 재설계
- BattleScene gameplay loop 검증
- WebGL 실기 로그인/저장 검증
- code-driven rebuild route를 기본 authoring 경로로 재도입

## 실행 순서

1. **초기 overlay state 정리**
   - Play Mode 첫 프레임에서 `AccountSettingsView`, `SetCLoginLoadingOverlayRoot`, room/create/error overlays가 동시에 노출되지 않게 scene serialized default를 정리한다.
   - 기본 Lobby 본문과 `LobbyGarageNavBar`만 첫 화면에서 읽히는지 확인한다.

2. **Lobby/Garage visual pass**
   - Lobby tab capture와 Garage tab capture를 각각 새로 만든다.
   - 겹침, density, bottom nav 가림, active tab 강조를 확인한다.
   - runtime binding을 위해 필요한 hidden/control object가 visible surface를 덮지 않는지 확인한다.

3. **Lobby -> BattleScene 연결 정리**
   - `DefaultGameSceneName` 또는 동등한 serialized scene name owner를 `BattleScene` 기준으로 바꿀지 확인한다.
   - Build Settings에 `BattleScene`이 enabled로 등록되어 있는지 확인한다.
   - start-game end-to-end는 BattleScene readiness 범위와 분리하되, scene name mismatch는 LobbyScene blocker로 닫는다.

4. **interaction smoke**
   - Play Mode 진입
   - Lobby tab 상태 확인
   - Garage tab invoke 후 `GaragePageRoot active` 확인
   - account/settings/login overlay가 예상 상태에서만 active인지 확인
   - console error 0 확인

5. **evidence 갱신**
   - `artifacts/unity/lobby-scene-lobby-tab-clean.png`
   - `artifacts/unity/lobby-scene-garage-tab-clean.png`
   - 필요한 경우 start-game route 결과를 별도 smoke artifact로 남긴다.

## Acceptance

- 첫 Play Mode capture에서 Lobby 본문, bottom nav, 주요 상태 텍스트가 서로 덮이지 않는다.
- Garage tab 전환 capture에서 Garage roster/editor 영역과 bottom nav가 서로 덮이지 않는다.
- default active overlay는 하나의 의도된 상태만 남고, account/loading/error/create/detail overlay가 동시에 떠 있지 않다.
- `LobbyGarageNavBar`의 Lobby/Garage tab invoke가 console error 없이 동작한다.
- `DefaultGameSceneName` 계열 값이 현재 전투 씬 후보와 불일치하지 않는다.
- compile check, required-field validation, Play Mode smoke, `npm run --silent rules:lint`가 통과한다.

## Blocked / Residual 처리

- Firebase/Auth 실제 응답 대기 때문에 loading overlay가 반드시 떠야 하면, 이를 visual bug가 아니라 runtime state residual로 분리한다.
- `BattleScene` gameplay readiness 때문에 start-game이 끝까지 닫히지 않으면, scene name 연결만 LobbyScene acceptance로 닫고 gameplay smoke는 BattleScene lane으로 넘긴다.
- Set A/B/C prefab 자체의 visual fidelity 문제가 남으면, LobbyScene scene-state 문제와 prefab fidelity 문제를 분리해서 기록한다.
- Garage tab 내부 세부 typography/copy polish가 남으면, LobbyScene initial overlay completion이 아니라 `Set B Garage` visual fidelity residual로 넘긴다.

## 검증 명령

- `powershell -ExecutionPolicy Bypass -File .\tools\check-compile-errors.ps1`
- Unity MCP `Tools/Validate Required Fields`
- Unity MCP Play Mode smoke
- Unity MCP GameView capture
- `powershell -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-UnityUiAuthoringWorkflowPolicy.ps1`
- `npm run --silent rules:lint`

## 문서 재리뷰

- 과한점 리뷰: 새 규칙이나 새 owner 기준을 만들지 않고, LobbyScene completion 작업만 분리했다.
- 부족한점 리뷰: 현재 상태, 제외 범위, 실행 순서, acceptance, blocked/residual, 검증 명령을 포함했다.
- 수정 후 재리뷰: obvious 과한점/부족한점 없음.
- 반복 재리뷰 반영: 새 active plan 생성이므로 `owner impact`와 `doc lifecycle checked`를 closeout에 명시한다.
- owner impact: primary `plans.lobby-scene-completion`; secondary `plans.progress`, `docs.index`; out-of-scope `plans.lobby-scene-runtime`, `ops.unity-ui-authoring-workflow`, scene/prefab mutation.
- doc lifecycle checked: 새 문서는 active plan으로 등록하고, 기존 `lobby_scene_runtime_plan.md`는 runtime assembly plan으로 유지하며 대체/삭제하지 않는다.
- 2026-04-25 polish update rereview: 상태 문구만 최신 evidence에 맞췄고, 새 owner/새 규칙/새 artifact gate는 추가하지 않았다. 남은 판단은 `Set B Garage` final fidelity로 분리된다.
- plan rereview: clean
- 2026-04-26 simplification pass: LobbyScene 초기 표시와 BattleScene 연결은 완료 기록으로 보고 reference로 내린다. 남은 Garage visual 판단은 `plans.garage-ui-ux-improvement`와 `plans.progress`에서 본다.
