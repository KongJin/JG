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
- `Set A`의 다음 판단은 `source html/png -> source facts -> contract draft -> validate -> translate/generate -> capture -> verdict` 루프에서 pass 또는 blocked verdict를 남기는 것이다.

## 현재 포커스

- Stitch-to-Unity lane의 stale layer와 stale evidence를 줄인다.
- Stitch-to-Unity lane에서 per-surface script onboarding을 줄이고 generic source-to-contract route를 닫는다.
- set-specific SceneTool을 다시 늘리지 않고 generic parser coverage를 넓힌다.
- `Set B Garage` visual fidelity final judgment을 닫는다.
- `Set C account-delete-confirm` overlay의 icon/runtime framing fidelity 보정을 이어간다.
- `GameScene` placement drag/drop automation contract와 multiplayer sync smoke 마감
- shared `Account/Garage` validation, WebGL 실기 검증, 설정 동기화 마감
- Lobby/Garage 시각 polish와 공용 validation 보강
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
- Phase 10: WebGL 빌드 smoke 테스트
- Phase 10: Garage save/load WebGL 실기 확인 후속 1회 더 재현
- Phase 10: Garage save action 접근성 / settings interaction을 shared `Account/Garage` validation으로 재확인
- Phase 10: Set B Garage visual fidelity final judgment closeout
- Phase 11: WebGL 빌드에서 Google 로그인 실기 테스트
- Phase 11: 익명->Google 계정 linking 시 UID 유지 확인
- Phase 11: Google 로그인 WebGL smoke 테스트

## 다음 작업

- `GameScene` 쪽은 placement drag/drop automation contract와 multiplayer sync smoke를 우선 마감한다.
- Lobby/Garage 쪽은 mobile-first Garage 단일 구조의 시각 밀도와 review evidence 기준 visual fidelity를 계속 sanity check 한다.
- shared `Account/Garage` lane에서는 Garage save/load WebGL, settings interaction, save action 접근성을 계속 추적한다.
- Stitch lane 쪽은 `Set B/C`를 generic onboarding 기준 샘플로 삼아, `Set A/D/E`와 이후 다시 여는 inventory set을 per-surface script edit 없이 단순 범용 루프의 verdict까지 태울 수 있게 source facts/draft/validate route를 일반화한다.
- 외부 디자인 시안은 `Stitch`를 기본 생성 도구로 두고, 실제 반영은 Unity MCP와 scene/prefab contract 기준으로 번역한다.

## 상세 이력

- dated change log와 이전 구현 메모는 [`progress_changelog.md`](./progress_changelog.md) 에서 본다.
- 자세한 Phase별 작업 항목은 [`game_scene_entry_plan.md`](./game_scene_entry_plan.md) 참고.
- 계정 시스템 상세 계획은 [`account_system_plan.md`](./account_system_plan.md) 참고.
