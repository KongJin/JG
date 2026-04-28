# 진행 상황 (Game Scene Entry)

> 마지막 업데이트: 2026-04-28
> 상태: active
> doc_id: plans.progress
> role: plan
> owner_scope: 레포 전체 현재 상태, 현재 포커스, 다음 작업
> upstream: docs.index
> artifacts: `artifacts/unity/`, `artifacts/webgl/`

## 상태 주석

- Phase 0~9의 `완료` 표기는 주로 code path 기준이다. 2026-04-26 single-client smoke에서 `LobbyScene -> BattleScene`, 적 스폰/제거, core damage -> defeat overlay까지 에러 0으로 재현됐다. 같은 날 actual UI path로 Lobby room create -> room detail -> ready -> start -> BattleScene 진입, actual placement/summon stat smoke, diagnostic victory result smoke도 통과했다. 2026-04-27에는 current UI hierarchy 기준 placement path, placement center contract, natural final-wave victory, defeat regression smoke가 모두 `newErrorCount: 0`으로 통과했다. Summon failure rollback/enemy priority/drag-drop direct test asset은 추가됐고 compile-clean은 통과했으며, rollback 변경 뒤 natural victory smoke도 `newErrorCount: 0`으로 통과했다. Phase 5 preflight는 WebGL build와 single-client baseline은 확인했지만 repo-local 2-client runner가 없어 `blocked: two-client runner unavailable`로 남겼다. Mobile HUD framing은 actual Lobby path 기반 `390x844` portrait screenshot과 visible Stitch victory overlay로 통과했다. 실제 플로우 acceptance는 [`game_scene_flow_validation_closeout_plan.md`](./game_scene_flow_validation_closeout_plan.md)가 소유하며, 남은 리스크는 direct EditMode 실행 확인과 Phase 5 멀티플레이어 수동/runner smoke다.
- Phase 10/11은 Firestore/Garage 핵심 경로와 Google linking 코드가 있으나 WebGL 실기 검증, 설정 동기화, UID 유지 확인이 남아 있다.
- UI 작업의 현재 실행 기준은 Stitch source freeze -> UI Toolkit candidate surface -> preview capture/report다. SetB Garage runtime binding/replacement는 evidence와 함께 닫혔고, native/mixed migration route는 [`non_stitch_ui_stitch_reimport_plan.md`](./non_stitch_ui_stitch_reimport_plan.md)가 소유한다. Account/Sync와 Connection/Reconnect는 UI Toolkit candidate surface와 isolated preview capture/report까지 생겼지만 runtime replacement 후보로만 남긴다. Operation Memory / Shared Shell source 후보와 preview evidence는 owner report, `design/ui_foundations.md`, Stitch `meta.json` 상태로 이관됐다.
- `Set B Garage`는 runtime evidence와 visual/candidate evidence를 분리해 닫았다. `Operation Memory - Accepted Dark Dock`은 2026-04-28 source freeze 됐고 `RETURN TO LOBBY` primary CTA, dark Nova1492 Garage panel tone, recent-5 structure를 반영했다. Operation Memory와 Shared Shell의 UI Toolkit candidate files, isolated preview scenes, GameView captures가 생겼지만 runtime replacement 후보로 보지는 않는다. Garage WebGL save/load, settings, accessibility는 shared `Account/Garage` validation lane에서 본다.
- Nova1492 audio는 SFX/BGM 채널과 런타임 소비까지 연결됐고, WebGL 오디오 로드/재생 smoke가 남아 있다.
- Nova1492 `.GX` 모델은 제한 변환/staging과 Garage preview mapping이 들어갔다. UnitParts Core 321은 catalog, generated preview prefab, playable SO, `ModuleCatalog.asset` append, Garage Nova Parts panel 검색/필터/적용 smoke, generated 조합 validation, local/Firestore mapper save-load roundtrip까지 닫혔다. socket/pivot/scale 자동 1차 alignment data는 `auto_ok` 부품에 한해 Garage preview runtime 조립에 적용됐고, BattleScene 전투 유닛 모델 교체는 아직 후속 범위다. 남은 밸런스/이름/사용권 판단과 Lobby 장식 후보 판단은 playable 승격 acceptance 밖의 residual이다.
- Garage 모바일 single vertical scroll 구조, code cleanup, LobbyScene runtime assembly, 초기 overlay state, `BattleScene` 연결명, Garage 기본 density/copy polish는 기능상 닫혔다. 남은 Garage 판단은 Set B visual fidelity와 runtime replacement gap으로 분리한다.
- Runtime smoke tooling broad stabilization은 닫혔다. 남은 helper 신뢰성 문제는 해당 active owner plan이나 targeted tooling pass에서만 다시 연다.
- 문서관리 lane은 완료된 draft/reference plan과 dated changelog를 제거하고 `docs/index.md`를 active/current owner 중심 registry로 줄였다. 현재 진행 판단은 이 문서와 각 active owner plan을 우선한다.

## 현재 포커스

- `GameScene` actual-flow closeout: direct EditMode 실행 확인, drag/drop residual, 2-client sync/late-join hydration
- `GameScene` Phase 5 multiplayer sync smoke: runner 부재를 수동 2-client session 또는 runner 구현으로 닫기
- `Technical debt recurrence prevention`: Setup/Root drift, runtime lookup, dynamic repair 재발 방지 기준을 [`technical_debt_recurrence_prevention_plan.md`](./technical_debt_recurrence_prevention_plan.md)에 따라 유지
- `Non-Stitch UI` migration route: Stitch source freeze가 없는 native/mixed UI를 UI Toolkit candidate 대상으로 다시 가져오기
- `Account/Connection UI` candidate: Nova1492 Garage UI tone 기준 Account/Sync와 Connection/Reconnect 후보는 UI Toolkit candidate surface와 preview evidence까지 확보, runtime integration은 별도 pass
- shared `Account/Garage` WebGL save/load, settings interaction, save action accessibility 검증
- Nova1492 후속 밸런스/이름/사용권 residual

## Phase 진행률

| Phase | 상태 | 요약 |
|---|---|---|
| Phase 0: 씬 진입 전 | ✅ 완료 | GarageRoster 직렬화, Room 진입 시 동기화 |
| Phase 1: GameScene 초기화 | ✅ 완료 | EventBus, Unit/Garage Setup, Unit 스펙 계산 |
| Phase 2: 소환 시스템 | ✅ 완료 | SummonUnitUseCase, Energy 시스템, UnitSlot UI (드래그+클릭), 로테이션 |
| Phase 3: Wave/Enemy와 Unit 연결 | ✅ 완료 | GameStartEvent 조건 제거, Enemy -> Unit 타겟팅, BattleEntity Combat 등록 |
| Phase 4: 재소환 시스템 | ✅ 완료 | UnitDiedEvent, UnitDeathEventHandler, 재소환 UI |
| Phase 5: 네트워크 동기화 | ✅ code path 완료 / 🟨 smoke 남음 | Energy/Mana 통합, IPlayerSpecProvider, GetLocalPlayerRoster, BattleEntity late-join HP sync. 2-client acceptance는 `game_scene_phase5_multiplayer_sync_plan.md`가 소유 |
| Phase 6: 게임 종료 | ✅ 완료 | GameEndEvent 재설계, GameEndAnalytics, WaveEndView 개선, Lobby 복귀, Firebase Analytics |
| Phase 7: 배치 시스템 완성 | ✅ 완료 | PlacementArea, PlacementAreaView, 드래그 피드백, 영역 검증, MaterialFactory, ErrorView |
| Phase 8: Energy 재생 증가 곡선 | ✅ 완료 | EnergyRegenCurve (시간 기반 60s->180s, 3->5/s), EnergyRegenCurveConfig, TickRegen wiring |
| Phase 9: 네트워크 완성 | ✅ 완료 | BattleEntityPhotonController (IPunObservable HP/pos/dead sync), BattleEntityDespawnAdapter, WaveEndView 통계 |
| Phase 10: 계정 시스템 | 🟨 복구 진행 중 | Firestore/Garage 저장·로드·삭제·재시도 핵심 경로는 연결됐고, 남은 과제는 WebGL 실기 검증과 설정 동기화, 계정 UX 마무리 |
| Phase 11: Google 로그인 | 🟨 실동작 검증 전 | Google linking 경로 코드는 존재하지만 UID 유지와 WebGL 실기 동작은 아직 검증되지 않음 |

## 미완료 TODO

| Lane | 남은 TODO |
|---|---|
| `GameScene` runtime | Phase 5 preflight artifact는 `two-client runner unavailable`로 blocked. 다음은 수동 2-client session 또는 runner 구현으로 BattleEntity/Energy/Wave hydration mismatch 여부 확인, player avatar commander/base contract |
| `GameScene` flow closeout | summon failure rollback, enemy target priority, drag/drop direct test asset은 추가됐고 compile-clean은 통과. Placement center automation, natural victory flow closeout smoke, mobile HUD framing smoke는 `newErrorCount: 0` 기준으로 통과. EditMode 실행은 open-editor-owned project로 blocked이며, 다음은 2-client sync/late-join과 GameEnd result HUD actual player-flow checklist 갱신 |
| `GameScene` operation record | 남은 EditMode test 실행과 WebGL player Firestore restore smoke는 shared `Account/Garage` validation lane에서 본다 |
| `GameScene` HUD/input | placement center confirm automation은 pass, mobile result UI는 actual Lobby path 기반 portrait screenshot에서 pass. drag/drop direct test asset은 추가됐고 실행 확인이 남음 |
| `GameScene` multiplayer | Phase 5 preflight 기준 repo-local 2-client runner 없음. wave start, core victory/defeat baseline은 single-client로 확보됐고, late-join/BattleEntity/Energy multiplayer sync smoke는 수동 세션 또는 runner 구현 필요 |
| `Runtime smoke tooling` | 새 helper 신뢰성 문제가 나오면 broad stabilization이 아니라 해당 step/tool owner의 targeted pass에서만 다시 연다 |
| `Account/Garage` WebGL | Firebase Console 설정, WebGL build smoke, Garage save/load 재현, settings 저장/소비, save action 접근성, settings interaction |
| `Audio` WebGL | 사운드 설정 UI 저장 확장과 WebGL 오디오 로드/재생 smoke |
| `Set B Garage UITK runtime` | runtime evidence는 닫혔고, WebGL save/load/settings/accessibility는 shared `Account/Garage` lane에서 본다 |
| `Non-Stitch UI migration` | Account/Sync와 Connection/Reconnect는 UI Toolkit candidate surface와 preview evidence까지 확보. 다음은 runtime replacement가 필요한지 별도 pass로 판단하거나, 남은 native/mixed 후보를 Stitch accepted baseline부터 시작한다 |
| `Prefab management` | closeout 완료. 남은 항목은 runtime-referenced UI/feedback prefab의 UITK replacement 판단이다 |
| `Google Login` | WebGL Google login smoke와 anonymous -> Google linking UID 유지 확인 |

## 다음 작업

- `GameScene` 쪽은 [`game_scene_flow_validation_closeout_plan.md`](./game_scene_flow_validation_closeout_plan.md)에 따라 single-client actual UI path/victory/defeat/placement center/mobile HUD pass를 기준선으로 두고, summon rollback/enemy priority/drag-drop EditMode 실행 확인과 Phase 5 수동 2-client session 또는 runner 구현을 success/blocked/mismatch로 분리해 닫는다.
- Runtime smoke helper와 작전 기록 / 세계 기억 쪽 새 blocker가 나오면 현재 active owner plan이나 shared `Account/Garage` validation lane으로 이관한다.
- `LobbyScene` 쪽은 runtime/completion이 닫혔으므로, 새 blocker가 없으면 Garage final fidelity만 `Set B Garage` 판단과 함께 본다.
- Lobby/Garage UI 변경은 UI Toolkit candidate surface route에서 시작한다.
- Prefab 관리 빈틈은 inventory, approval manifest, review/import stale path fail, missing prefab fallback 차단, override drift report, Resources migration 연결까지 닫혔다. 남은 runtime-referenced UI/feedback prefab은 `non_stitch_ui_stitch_reimport_plan.md`에서 UITK replacement 후보로 추적한다.
- 변환된 Nova1492 GX 모델 중 Garage UnitParts 대량 승격은 catalog manifest, preview prefab pack, playable SO, Garage Nova Parts panel search/apply smoke, generated Firepower x Mobility 9,760조합 validation, local/Firestore mapper save-load roundtrip, socket/pivot/scale alignment data 준비와 Garage preview runtime 적용까지 완료됐다. Lobby 장식 후보와 BattleScene 전투 유닛 모델 교체는 별도 후속 범위로 남긴다.
- Lobby/Garage 및 Operation Memory 쪽 source 판단은 닫혔고, runtime 적용 여부가 필요한 경우 새 owner pass로 다시 연다.
- shared `Account/Garage` lane에서는 Garage save/load WebGL, settings interaction, save action 접근성을 계속 추적한다.
- Stitch lane 쪽은 이후 다시 여는 inventory set을 UI Toolkit candidate 루프의 verdict까지 태울 수 있게 source facts/draft/validate route를 일반화한다.
- Stitch source가 없는 Unity-native/mixed UI는 [`non_stitch_ui_stitch_reimport_plan.md`](./non_stitch_ui_stitch_reimport_plan.md)에 따라 먼저 Stitch accepted screen/source freeze를 만든 뒤 UI Toolkit candidate surface 대상으로 다룬다.
- 외부 디자인 시안은 `Stitch`를 기본 생성 도구로 두고, 실제 반영은 UI Toolkit candidate surface와 Unity runtime replacement 검증으로 번역한다.

## 상세 참고

- 계정 시스템 상세 계획은 [`account_system_plan.md`](./account_system_plan.md) 참고.
