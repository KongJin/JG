# Runtime Smoke Tooling Stabilization Plan

> 마지막 업데이트: 2026-04-27
> 상태: active
> doc_id: plans.runtime-smoke-tooling-stabilization
> role: plan
> owner_scope: Unity MCP runtime smoke helper의 lock/process, timeout/transport, UI path contract, evidence artifact 안정화
> upstream: plans.progress, tools.unity-mcp-readme, ops.acceptance-reporting-guardrails, ops.unity-ui-authoring-workflow
> artifacts: `tools/unity-mcp/*.ps1`, `tools/workflow/*.ps1`, `Assets/Editor/UnityMcp/`, `artifacts/unity/`, `Temp/UnityMcp/runtime-smoke.lock`

이 문서는 최근 생성된 runtime smoke/helper script에서 반복된 문제를 기능 owner와 분리해 닫기 위한 계획이다.
GameScene, Lobby, Garage의 실제 acceptance는 각 feature plan이 소유하고, 이 문서는 "검증 도구가 결과를 믿을 수 있게 만드는가"만 소유한다.

---

## 왜 새 계획인가

최근 script failure는 단일 기능 결함보다 검증 tooling 결함이 반복되는 양상이다.
같은 Play Mode, 같은 MCP bridge, 같은 UI path wait 계층을 여러 smoke가 공유하면서 아래 문제가 되풀이됐다.

- background probe나 `-NoMcpLock` 실행이 남아 다음 smoke의 Play Mode와 console buffer를 흔든다.
- `/ui/wait-*` 계열 long wait가 client timeout과 어긋나 504 또는 transport write error로 번지고, 게임 오류와 도구 오류가 섞인다.
- `LobbyPageRoot`, `SetCRoomDetailPanelRoot`, `BattleSceneSystems`, `GameSceneRoot` 같은 path 후보가 script마다 흩어져 stale hierarchy에 약하다.
- 실패 중간에 process가 끊기면 artifact가 없거나 `changedFiles`/run metadata가 없어 evidence scope를 다시 판단해야 한다.
- overlay/root active 상태를 authoring helper가 끄고 runtime presenter가 coroutine을 시작하는 식의 UI root visibility 문제가 smoke 실패처럼 보인다.
- 직접 `.ps1` 실행과 `ExecutionPolicy Bypass` wrapper가 섞여 command 재현성이 낮다.

## Scope

In scope:

- Unity MCP runtime smoke helper의 lock, stale process, Play Mode final-state guard
- UI wait timeout과 MCP transport failure 분류
- runtime smoke path 후보와 scene/UI contract preflight
- smoke artifact schema와 incremental evidence 기록
- `Invoke-GameScenePlacementSmoke.ps1` 계열의 step 분리와 repeatability
- closeout helper 실행 wrapper의 문서화 또는 표준화

Out of scope:

- GameScene 2-client sync gameplay acceptance
- 모바일 HUD visual framing 자체 수정
- BattleScene combat model assembly
- Lobby/Garage UI fidelity 판단
- Unity MCP bridge 전체 재작성

## 현재 반복 문제

| ID | 증상 | 영향 | 판정 |
|---|---|---|---|
| T1 | stale background smoke/probe가 Play Mode를 다시 시작하거나 stop한다 | 다음 실행의 scene, console, result가 오염된다 | tooling blocker |
| T2 | `-NoMcpLock`가 자동화 실행에도 쉽게 쓰인다 | lane 간 MCP ownership이 깨진다 | tooling blocker |
| T3 | long wait endpoint가 client timeout 뒤에도 server-side write를 시도한다 | 504/transport error가 Unity console error처럼 보인다 | bridge/tooling blocker |
| T4 | UI path 후보가 helper마다 중복된다 | current runtime UI hierarchy 변경 때 stale path fail이 반복된다 | path contract blocker |
| T5 | artifact가 finally에서만 쓰이거나 필드가 부족하다 | blocked/mismatch/success를 사후에 구분하기 어렵다 | evidence blocker |
| T6 | inactive root 위에서 runtime presenter/coroutine이 시작된다 | UI state 실패가 gameplay 실패처럼 보인다 | UI contract blocker |
| T7 | direct PowerShell 실행이 policy에 막힌다 | 검증 명령 재현성이 낮다 | workflow friction |

## 2026-04-27 Slice 1

Applied:

- `McpHelpers.ps1` now clears a runtime smoke lock when the recorded holder PID no longer exists, while keeping live process locks authoritative.
- `Invoke-GameScenePlacementSmoke.ps1` now writes owner/script/command, start/end UTC, active scene path, lock owner, Play Mode final state, step verdicts, transport error bucket, mismatch reason, and evidence scope reason.
- `Invoke-GameScenePlacementSmoke.ps1` now classifies transport-style failures separately from path-contract blockers.
- `Invoke-GameSceneMobileHudFramingSmoke.ps1` now owns the runtime smoke lock for the whole placement + screenshot flow and calls the nested placement helper under a recorded parent lock.
- `tools/unity-mcp/README.md` examples now use `powershell -NoProfile -ExecutionPolicy Bypass -File ...`.

Verified:

- PowerShell parser passed for `McpHelpers.ps1`, `Invoke-GameScenePlacementSmoke.ps1`, and `Invoke-GameSceneMobileHudFramingSmoke.ps1`.
- A dead-PID temp lock was cleared and replaced by a live helper lock, then released.
- A forced transport-blocked placement run produced a JSON artifact with `blockedReason = transport-error`, step verdicts, transport error count, and no leftover lock.
- A forced live-lock mobile HUD run produced a blocked artifact without removing the live holder lock.
- Real editor placement smoke wrote `game-scene-placement-stable-smoke-1.json`; helper artifact and cleanup worked, while the runtime flow blocked at StartGameButton 409 before the interactable wait patch.
- After adding StartGameButton interactable wait, `game-scene-placement-stable-smoke-2.json` reached StartGameButton click success and then blocked as `scene-transition` because `BattleScene` did not become active.
- A follow-up placement run wrote `game-scene-placement-stable-smoke-3.json` and blocked cleanly with `runtime-smoke-lock-held` while a live `GameSceneMobileHudFraming` helper owned `Temp/UnityMcp/runtime-smoke.lock`.
- Generated artifact scope check passed for the new placement smoke artifacts; current smoke artifacts remain accepted as `no-changed-files-field`, and the existing UI workflow policy artifact was scoped against the current dirty worktree.
- A hung mobile HUD helper exceeded its runtime window while Play Mode was already stopped, so only that PowerShell smoke process was terminated. The next placement run cleaned its dead-PID lock and proceeded.
- `game-scene-placement-stable-smoke-4.json` classified the remaining blocker as `scene-transition` and captured console delta with `newErrorCount = 0`; the decisive runtime warning was `[Lobby] All players must be ready and room must have at least two members.`
- The placement helper now waits for ReadyButton label `Cancel` before pressing Start, but that path still needs verification because a new live mobile HUD helper lock blocked the immediate follow-up.
- `LobbyAccountBootstrapFlow` and `LobbySetup` now avoid calling `LoginLoadingView` after it has been destroyed during Play Mode cleanup, preventing smoke-induced MissingReference console errors.
- `game-scene-placement-stable-smoke-7.json` passed: Lobby create -> ready label `Cancel` -> start -> BattleScene -> slot click -> placement confirm -> preview hidden -> console delta `newErrorCount = 0` -> Play Mode stopped.
- `game-scene-placement-stable-smoke-8.json` blocked at room detail path wait with console delta `newErrorCount = 0`; classification was corrected so path waits no longer become transport errors just because the text contains "Timed out".
- `game-scene-placement-stable-smoke-9.json` passed the same `ResultMode None` route again with console delta `newErrorCount = 0`, proving repeatable placement smoke after the Ready-state wait.

Residual:

- The full GameScene `ResultMode None` runtime smoke repeatability acceptance is satisfied by `game-scene-placement-stable-smoke-7.json` and `game-scene-placement-stable-smoke-9.json`.
- Step artifact writing is currently top-level artifact only; per-step files are still future work.
- Remaining tooling residual is per-step artifact files; mobile HUD helper hang hardening and placement path contract inventory now have first-pass coverage.

## 2026-04-27 Slice 2

Applied:

- `Invoke-GameScenePlacementSmoke.ps1` now keeps the repeated Lobby/Battle UI path candidates in a single `SmokeUiPaths` contract map and writes `pathContractVersion` plus `pathCandidates` into the top-level artifact.
- `Invoke-GameSceneMobileHudFramingSmoke.ps1` now records owner/script/command, UTC start/end, step verdicts, transport errors, mismatch reason, and evidence scope reason.
- Mobile HUD UI snapshot helpers now call `/ui/get-state` with a bounded request timeout instead of relying on unbounded UI state convenience wrappers.
- Mobile HUD blocked classification now separates live runtime lock, path contract, and transport failures.

Verified:

- PowerShell parser passed for `Invoke-GameScenePlacementSmoke.ps1` and `Invoke-GameSceneMobileHudFramingSmoke.ps1`.
- NaturalVictory mobile HUD framing smoke passed end-to-end with nested placement under the parent lock: `artifacts/unity/game-flow/game-scene-mobile-hud-framing-smoke-latest.json`.
- The same run produced `390x844` GameView screenshot evidence at `artifacts/unity/game-flow/game-scene-mobile-hud-framing-latest.png`, visible Stitch victory overlay, `newErrorCount = 0` in nested placement evidence, and `playModeStopped = true`.

## Workstreams

### 1. Lock and Process Ownership

목표:

- automated runtime smoke는 기본적으로 `Temp/UnityMcp/runtime-smoke.lock`을 잡는다.
- `-NoMcpLock`는 수동 supervised debug에서만 허용하거나 helper 이름/argument에 manual intent가 드러나게 한다.
- helper 시작 전 stale smoke process와 held lock을 감지해 success가 아니라 `blocked`로 보고한다.
- timeout, Ctrl+C, exception 뒤에도 Play Mode final state와 lock cleanup 여부를 artifact에 남긴다.

Done when:

- 같은 smoke를 연속 2회 실행해도 이전 process가 다음 실행을 stop/start하지 않는다.
- helper가 실패해도 lock owner, process id, play mode stop 여부가 artifact에 남는다.
- 다른 lane lock이 있으면 그 lane을 죽이지 않고 `blocked: runtime-smoke-lock-held`로 끝난다.

### 2. Step-Based Runtime Smoke

목표:

- monolithic GameScene smoke를 아래 step으로 나누고 각 step이 즉시 artifact를 쓴다.
- top-level helper는 step artifact를 aggregate만 한다.

Steps:

1. MCP and compile preflight
2. Lobby scene and room UI readiness
3. room create/ready/start to BattleScene transition
4. BattleScene system and HUD readiness
5. unit slot select and placement confirm
6. result mode action, if requested
7. console delta and final Play Mode cleanup

Done when:

- 실패 지점이 `Lobby UI blocked`, `BattleScene transition blocked`, `placement mismatch`, `transport failure`처럼 한 단계로 분류된다.
- 중간 실패에도 마지막으로 성공한 step과 다음 blocker가 JSON으로 남는다.

### 3. UI Path Contract Inventory

목표:

- helper마다 흩어진 path 후보를 central map 또는 small shared function으로 모은다.
- scene load 뒤에는 path 존재와 active state를 먼저 preflight하고, 실패하면 gameplay mismatch로 올리지 않는다.
- authoring helper가 whole-root inactive override를 남긴 경우 runtime smoke 전에 visibility contract로 잡는다.

Done when:

- `LobbyPageRoot`, `SetCRoomDetailPanelRoot`, `BattleSceneSystems`, `GameSceneRoot`, `UnitSlot-0`, result overlay 후보가 한 곳에서 관리된다.
- path fail은 `blocked: path-contract`로 artifact에 남고, GameScene acceptance 실패와 분리된다.

### 4. Timeout and Transport Hardening

목표:

- HTTP client timeout, endpoint wait timeout, retry budget을 같은 단위로 맞춘다.
- `/ui/wait-*` long wait는 client timeout보다 짧게 쪼개거나 polling helper로 감싼다.
- MCP transport/504 failure는 Unity console error와 별도 bucket에 기록한다.

Done when:

- UI wait 실패가 `newErrorCount`를 오염하지 않고 `transportErrors` 또는 `toolingErrors`로 분리된다.
- disconnected client 뒤 server-side write error가 console acceptance failure로 보고되지 않는다.

### 5. Evidence Artifact Contract

목표:

모든 runtime smoke artifact는 최소 아래 필드를 가진다.

- `owner`
- `script`
- `command`
- `startedAtUtc`
- `endedAtUtc`
- `activeScenePath`
- `lockOwner`
- `playModeStarted`
- `playModeStopped`
- `stepVerdicts`
- `newErrorCount`
- `transportErrors`
- `blockedReason`
- `mismatchReason`
- `changedFiles` 또는 `evidenceScopeReason`

Done when:

- success, blocked, mismatch 모두 JSON artifact를 남긴다.
- `Test-GeneratedArtifactScope.ps1`가 신규 smoke artifact를 scoped로 판단하거나, `changedFiles`를 둘 수 없는 artifact는 명시적 `evidenceScopeReason`을 가진다.

### 6. Verification Wrapper

목표:

- PowerShell script 실행은 `powershell -NoProfile -ExecutionPolicy Bypass -File ...` 형태를 기본 예시로 통일한다.
- closeout pack, compile check, runtime smoke helper가 같은 wrapper style로 재현된다.

Done when:

- README와 plan의 validation command가 직접 실행 정책에 막히지 않는다.
- helper failure report에 실제 command line이 남는다.

## Acceptance

이 계획은 아래가 모두 충족되기 전에는 `reference`로 내리지 않는다.

- automated runtime smoke에서 `-NoMcpLock` 사용이 제거되거나, manual-only path로 명확히 분리된다.
- stale runtime smoke process가 있으면 다음 smoke가 성공/실패로 섞이지 않고 blocked로 끝난다.
- smoke가 timeout 또는 exception으로 끝나도 lock cleanup과 Play Mode final state가 기록된다.
- UI path wait 실패가 게임 runtime mismatch와 분리되어 `blocked: path-contract` 또는 `blocked: transport`로 보고된다.
- GameScene `ResultMode None` placement smoke가 같은 command로 2회 연속 실행되어 stale process 간섭 없이 artifact를 만든다.
- 신규 smoke artifact가 step verdict, blocked/mismatch reason, transport error bucket, evidence scope 정보를 가진다.
- `npm run --silent rules:lint`가 통과한다.

## Validation Commands

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\workflow\Invoke-CloseoutPack.ps1 -PlanOnly
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\check-compile-errors.ps1
npm run --silent rules:lint
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\workflow\Test-GeneratedArtifactScope.ps1 -ArtifactPath artifacts/unity/game-flow/game-scene-placement-stable-smoke.json -ExpectedPattern "Assets/Scripts/Features/Unit/"
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\unity-mcp\Invoke-GameScenePlacementSmoke.ps1 -Owner GameSceneCloseout -ResultMode None -OutputPath artifacts/unity/game-flow/game-scene-placement-stable-smoke.json -TimeoutSec 180 -LeavePlayMode
```

## Blocked / Residual Handling

- Unity Editor가 프로젝트를 열고 있어 batchmode/direct tests가 막히면 `blocked: open-editor-owns-project`로 남기고 smoke success로 확장하지 않는다.
- two-client runner 부재는 Phase 5 sync plan residual이며 이 plan의 tooling success로 해결했다고 말하지 않는다.
- mobile HUD framing은 GameScene flow/UI residual이며 path/timeout 안정화와 섞지 않는다.
- Unity MCP bridge handler 수정이 필요하면 `Assets/Editor/UnityMcp/` owner 변경으로 분리하고, helper script patch만으로 해결됐다고 보지 않는다.
- artifact schema 변경은 기존 historical artifact를 소급 수정하지 않고 신규 artifact부터 적용한다.

## 문서 재리뷰

- 과한점 리뷰: GameScene gameplay, 2-client sync, HUD visual, model assembly를 이 계획의 success 조건으로 가져오지 않고 tooling reliability만 소유한다.
- 부족한점 리뷰: 최근 반복된 lock/process, timeout/transport, path contract, artifact evidence, execution wrapper 문제를 acceptance와 workstream으로 분리했다.
- owner impact: primary `plans.runtime-smoke-tooling-stabilization`; secondary `plans.progress`, `docs.index`, `tools.unity-mcp-readme`; feature acceptance owner는 `plans.game-scene-flow-validation-closeout`와 `plans.game-scene-phase5-multiplayer-sync`로 유지한다.
- doc lifecycle checked: runtime smoke helper 안정화는 여러 active feature plan의 공통 blocker라 새 active plan으로 등록한다. Acceptance가 pass 또는 명확한 owner residual로 이관되면 reference 전환한다.
- plan rereview: clean for document shape / residual for execution - scope, stop conditions, workstreams, validation commands, and acceptance are explicit; implementation starts at lock/process ownership and step artifact contract.
