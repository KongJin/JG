# 진행 상황 (Game Scene Entry)

> 마지막 업데이트: 2026-04-25
> 상태: active
> doc_id: plans.progress
> role: plan
> owner_scope: 레포 전체 현재 상태, 현재 포커스, 다음 작업
> upstream: docs.index
> artifacts: `artifacts/unity/`, `artifacts/webgl/`

## 상태 주석

- 중요: Phase 0~9의 `완료` 표기는 주로 코드 경로 기준이다.
- 2026-04-18 확인 시 실제 `Assets/Scenes/GameScene.unity` 씬 에셋은 삭제된 상태였고, 플레이 가능한 전투 씬 완성도와 문서 표기가 어긋나 있었다.
- 현재는 새 hand-authored `GameScene.unity`와 최소 `BattleEntity.prefab`을 다시 만들었고, build settings 등록과 required-field audit까지 복구했다.
- legacy lobby-to-game end-to-end summon smoke는 과거 통과 이력만 남아 있으며 현재 acceptance route가 아니다.
- click summon 기준 `GameScene` wave/core/victory loop는 다시 통과했고, 현재 남은 핵심 리스크는 placement drag/drop 자동화와 멀티플레이 동기화 smoke다.
- 2026-04-24 repo audit 기준, 현재 committed repo에는 `Assets/Scenes/GameScene.unity`, legacy lobby scene, build settings scene entry가 존재하지 않는다.
- 따라서 2026-04-18 전후의 scene/smoke artifact는 historical recovery evidence로만 보고, reset 중의 current committed SSOT와 분리해서 읽는다.
- Lobby/Garage UI lane의 현재 active recovery surface는 `Set B Garage`이고, Set C는 `account-delete-confirm` overlay translation lane을 별도로 복구 중이다.
- `Set C account-delete-confirm`는 `source html/png -> execution contracts -> translation evidence`까지 연결됐다.
- 최신 translation artifact 기준 `presentation.applied = true`이고, source에서 바로 execution contract를 준비해 translation으로 이어지는 상태다. 최신 review capture는 `artifacts/unity/set-c-account-delete-confirm-scene-capture.png`다.
- `Set C common-error-dialog`도 같은 루프로 닫혔고, 최신 translation artifact 기준 `presentation.applied = true`를 유지한다.
- 최신 `common-error-dialog` review capture는 `artifacts/unity/set-c-common-error-dialog-scene-capture.png`다.
- `Set B Garage`는 `source freeze -> execution contracts -> prefab target -> fresh translation/review evidence` current route를 다시 맞췄고, compiled contract에도 `summary-card` meaning block이 복구됐다.
- 현재 Set B lane의 직접 남은 일은 dedicated runtime smoke 부재가 아니라 visual fidelity final judgment이다.
- Garage save/load WebGL, settings interaction, shared runtime correctness는 Set B prefab lane이 아니라 shared `Account/Garage` validation lane에서 계속 관리한다.
- 현재 남은 Set C 핵심 리스크는 `warning icon glyph` asset 미해결과 `Prefab Mode SceneView capture != runtime/mobile framing` 차이다.
- set별 전용 Stitch review/prefab SceneTool은 제거했고, 현재 review route는 generic tool만 남겨뒀다.
- `Set A`는 `set-a-create-room-modal`과 `set-a-lobby-populated` 모두 `source html/png -> source facts -> contract draft -> validate -> translate/generate -> capture -> verdict` 루프에서 pipeline `passed` verdict를 남겼다.
- 최신 Set A evidence는 `artifacts/unity/set-a-create-room-modal-pipeline-result.json`, `artifacts/unity/set-a-create-room-modal-scene-capture.png`, `artifacts/unity/set-a-lobby-populated-pipeline-result.json`, `artifacts/unity/set-a-lobby-populated-scene-capture.png`다.
- Set A의 남은 판단은 신규 Lobby prefab 생성에 대한 workflow policy 승인과 lobby capture visual fidelity pass다.
- 남은 Stitch source surfaces도 generic overlay draft route로 모두 onboarding했다: `set-c-login-loading-overlay`, `set-c-room-detail-panel`, `set-d-battle-hud-baseline`, `set-d-low-core-warning`, `set-d-unit-stats-popup`, `set-e-mission-defeat-overlay`, `set-e-mission-victory-overlay`.
- 위 7개 surface는 draft validation, translation/generation, SceneView capture까지 pipeline `passed` verdict를 남겼다.
- Battle 쪽 추가 source freeze인 `set-d-gamescene-hud-full`도 `artifacts/stitch/11729197788183873077/bf3d08890f2d4a4e98f81c25e14d6073/`의 `GameScene HUD` source에서 generic draft route로 가져왔고, draft validation, translation/generation, SceneView capture까지 pipeline `passed` verdict를 남겼다.
- 현재 Stitch UI lane의 남은 공통 판단은 신규 prefab workflow policy guard 승인과 visual fidelity final pass다.
- `LobbyScene` runtime assembly는 scene 생성, Build Settings 등록, required-field validation, Play Mode Lobby/Garage tab smoke까지 통과했다. 초기 overlay state, `BattleScene` 연결명, Garage 기본 tab density와 1차 typography/copy polish는 정리됐고, 최신 clean captures는 `artifacts/unity/lobby-scene-lobby-tab-clean.png`, `artifacts/unity/lobby-scene-garage-tab-clean.png`다. 남은 visual 판단은 `Set B Garage` final fidelity다.
- Nova1492 audio 첫 pass로 SFX 11개/BGM 3개가 제한 staging됐고, 기존 SoundPlayer 경로에 SFX/BGM 채널, Lobby/Battle/Result BGM 전환, 저장된 `master/bgm/sfxVolume` 소비가 연결됐다. WebGL 오디오 로드/재생 smoke는 아직 남아 있다.
- Nova1492 `.GX` 모델 후보는 converter prototype으로 871개 중 865개를 OBJ로 변환했고, `Assets/Art/Nova1492/GXConverted/` 아래에 category별로 정리했다. `LobbyScene` 적용 Phase 0 shortlist와 Phase 1 preview prefab pack 15개는 고정됐고, `GarageUnitPreviewView`에는 9개 runtime ID -> prefab mapping을 scene serialized reference로 연결했다. Play Mode Lobby -> Garage tab smoke와 AudioListener 경고 재확인은 통과했으며, 남은 일은 preview visual active-state/capture 확인이다.

## 현재 포커스

- Stitch-to-Unity lane의 stale layer와 stale evidence를 줄인다.
- Stitch-to-Unity lane에서 per-surface script onboarding을 줄이고 generic source-to-contract route를 닫는다.
- set-specific SceneTool을 다시 늘리지 않고 generic parser coverage를 넓힌다.
- `Set B Garage` visual fidelity final judgment을 닫는다.
- `Set C account-delete-confirm` overlay의 icon/runtime framing fidelity 보정을 이어간다.
- `GameScene` placement drag/drop automation contract와 multiplayer sync smoke 마감
- shared `Account/Garage` validation, WebGL 실기 검증, 설정 동기화 마감
- Lobby/Garage 시각 polish와 공용 validation 보강
- LobbyScene UI prefab instance/override 관리와 assembly helper 안전화 정리
- Stitch contract-first -> prefab-first reset 루프 정착

## Phase 진행률

| Phase | 상태 | 요약 |
|---|---|---|
| Phase 0: 씬 진입 전 | ✅ 완료 | GarageRoster 직렬화, Room 진입 시 동기화 |
| Phase 1: GameScene 초기화 | ✅ 완료 | EventBus, Unit/Garage Setup, Unit 스펙 계산 |
| Phase 2: 소환 시스템 | ✅ 완료 | SummonUnitUseCase, Energy 시스템, UnitSlot UI (드래그+클릭), 로테이션 |
| Phase 3: Wave/Enemy와 Unit 연결 | ✅ 완료 | GameStartEvent 조건 제거, Enemy -> Unit 타겟팅, BattleEntity Combat 등록 |
| Phase 4: 재소환 시스템 | ✅ 완료 | UnitDiedEvent, UnitDeathEventHandler, 재소환 UI |
| Phase 5: 네트워크 동기화 | ✅ 완료 | Energy/Mana 통합, IPlayerSpecProvider, GetLocalPlayerRoster, BattleEntity late-join HP sync |
| Phase 6: 게임 종료 | ✅ 완료 | GameEndEvent 재설계, GameEndAnalytics, WaveEndView 개선, Lobby 복귀, Firebase Analytics |
| Phase 7: 배치 시스템 완성 | ✅ 완료 | PlacementArea, PlacementAreaView, 드래그 피드백, 영역 검증, MaterialFactory, ErrorView |
| Phase 8: Energy 재생 증가 곡선 | ✅ 완료 | EnergyRegenCurve (시간 기반 60s->180s, 3->5/s), EnergyRegenCurveConfig, TickRegen wiring |
| Phase 9: 네트워크 완성 | ✅ 완료 | BattleEntityPhotonController (IPunObservable HP/pos/dead sync), BattleEntityDespawnAdapter, WaveEndView 통계 |
| Phase 10: 계정 시스템 | 🟨 복구 진행 중 | Firestore/Garage 저장·로드·삭제·재시도 핵심 경로는 연결됐고, 남은 과제는 WebGL 실기 검증과 설정 동기화, 계정 UX 마무리 |
| Phase 11: Google 로그인 | 🟨 실동작 검증 전 | Google linking 경로 코드는 존재하지만 UID 유지와 WebGL 실기 동작은 아직 검증되지 않음 |

## 미완료 TODO

- `GameScene` rebuild: placement area drag/drop, wave start, core victory/defeat loop 검증
- `GameScene` rebuild: 멀티플레이 smoke로 late-join, BattleEntity sync, Energy sync 확인
- Phase 9: 실제 멀티플레이어 smoke 테스트 (late-join, BattleEntity sync, Energy sync)
- Phase 10: Firebase Console 설정 (API Key, Project ID, Firestore DB 생성)
- Phase 10: 설정 Firestore 동기화 마무리 (저장 UI, language 소비 경로)
- Phase 10: 사운드 설정은 런타임 소비까지 연결됨. 설정 UI 저장 확장과 WebGL 오디오 실기 검증은 후속
- Phase 10: WebGL 빌드 smoke 테스트
- Phase 10: Garage save/load WebGL 실기 확인 후속 1회 더 재현
- Phase 10: Garage save action 접근성 / settings interaction을 shared `Account/Garage` validation으로 재확인
- Phase 10: Set B Garage visual fidelity final judgment closeout
- `LobbyScene` completion pass residual: Garage final visual fidelity는 `Set B Garage` 판단으로 분리해 추적
- Phase 11: WebGL 빌드에서 Google 로그인 실기 테스트
- Phase 11: 익명->Google 계정 linking 시 UID 유지 확인
- Phase 11: Google 로그인 WebGL smoke 테스트

## 다음 작업

- `GameScene` 쪽은 placement drag/drop automation contract와 multiplayer sync smoke를 우선 마감한다.
- `LobbyScene` 쪽은 [`lobby_scene_completion_plan.md`](./lobby_scene_completion_plan.md)를 evidence/residual 기준으로 유지하고, 새 blocker가 없으면 Garage final fidelity만 `Set B Garage` 판단과 함께 본다.
- LobbyScene UI/prefab 관리 부채는 [`lobby_scene_ui_prefab_management_plan.md`](./lobby_scene_ui_prefab_management_plan.md)에서 assembly helper 안전화, prefab override audit, preview placeholder 정리 순서로 본다.
- 변환된 Nova1492 GX 모델은 [`lobby_scene_nova1492_model_application_plan.md`](./lobby_scene_nova1492_model_application_plan.md)에 따라 preview visual active-state/capture 확인 후 scene template 중복 정리로 넘어간다.
- Lobby/Garage 쪽은 mobile-first Garage 단일 구조의 시각 밀도와 review evidence 기준 visual fidelity를 계속 sanity check 한다.
- shared `Account/Garage` lane에서는 Garage save/load WebGL, settings interaction, save action 접근성을 계속 추적한다.
- Stitch lane 쪽은 `Set A/B/C/D/E`와 추가 `GameScene HUD` source freeze를 generic onboarding 기준 샘플로 삼아, 이후 다시 여는 inventory set을 per-surface script edit 없이 단순 범용 루프의 verdict까지 태울 수 있게 source facts/draft/validate route를 일반화한다.
- 외부 디자인 시안은 `Stitch`를 기본 생성 도구로 두고, 실제 반영은 Unity MCP와 scene/prefab contract 기준으로 번역한다.

## 상세 이력

- dated change log와 이전 구현 메모는 [`progress_changelog.md`](./progress_changelog.md) 에서 본다.
- 자세한 Phase별 작업 항목은 [`game_scene_entry_plan.md`](./game_scene_entry_plan.md) 참고.
- 계정 시스템 상세 계획은 [`account_system_plan.md`](./account_system_plan.md) 참고.
