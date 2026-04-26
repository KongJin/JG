# 진행 상황 (Game Scene Entry)

> 마지막 업데이트: 2026-04-26
> 상태: active
> doc_id: plans.progress
> role: plan
> owner_scope: 레포 전체 현재 상태, 현재 포커스, 다음 작업
> upstream: docs.index
> artifacts: `artifacts/unity/`, `artifacts/webgl/`

## 상태 주석

- Phase 0~9의 `완료` 표기는 주로 code path 기준이다. 현재 직접 남은 GameScene 리스크는 placement drag/drop 자동화와 멀티플레이 동기화 smoke다.
- Phase 10/11은 Firestore/Garage 핵심 경로와 Google linking 코드가 있으나 WebGL 실기 검증, 설정 동기화, UID 유지 확인이 남아 있다.
- Stitch-to-Unity는 set별 전용 SceneTool을 줄이고 generic source facts -> draft -> validate route로 모으는 중이다. 남은 공통 판단은 신규 prefab workflow policy guard와 visual fidelity final pass다.
- `Set B Garage`는 visual fidelity final judgment가 직접 residual이다. Garage save/load, settings, accessibility는 shared `Account/Garage` validation lane에서 본다.
- `LobbyScene` runtime assembly, 초기 overlay state, `BattleScene` 연결명, Garage 기본 density/copy polish는 기능상 닫혔다. 남은 Garage visual 판단은 `Set B Garage`로 분리한다.
- Nova1492 audio는 SFX/BGM 채널과 런타임 소비까지 연결됐고, WebGL 오디오 로드/재생 smoke가 남아 있다.
- Nova1492 `.GX` 모델은 제한 변환/staging과 Garage preview mapping이 들어갔다. 다음 판단은 Lobby 장식 후보를 별도 inactive variant로 둘지 여부다.
- Garage 모바일 single vertical scroll 구조와 code cleanup은 완료 기록으로 내렸고, 남은 밀도/시각 판단은 Set B Garage fidelity로 본다.

## 현재 포커스

- `GameScene` placement drag/drop automation contract와 multiplayer sync smoke 마감
- `Set B Garage` visual fidelity final judgment
- shared `Account/Garage` WebGL save/load, settings interaction, save action accessibility 검증
- Stitch generic source-to-contract route 정착과 신규 prefab workflow policy guard 판단
- Lobby/Garage prefab instance 관리 부채와 Nova1492 preview 후속 판단

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
- `LobbyScene` 쪽은 runtime/completion 기록을 reference로 보고, 새 blocker가 없으면 Garage final fidelity만 `Set B Garage` 판단과 함께 본다.
- LobbyScene UI/prefab 관리 부채는 [`lobby_scene_ui_prefab_management_plan.md`](./lobby_scene_ui_prefab_management_plan.md)에서 assembly helper 안전화, prefab override audit, preview placeholder 정리 순서로 본다.
- 변환된 Nova1492 GX 모델은 [`lobby_scene_nova1492_model_application_plan.md`](./lobby_scene_nova1492_model_application_plan.md)에 따라 Phase 4 로비 장식 후보를 별도 inactive variant로 검토한다.
- Garage 모바일 scroll 구조와 코드 정리 완료 기록은 [`garage_mobile_scroll_recovery_plan.md`](./garage_mobile_scroll_recovery_plan.md), [`garage_mobile_scroll_code_cleanup_plan.md`](./garage_mobile_scroll_code_cleanup_plan.md)에서 reference로 본다.
- Lobby/Garage 쪽은 mobile-first Garage 단일 구조의 시각 밀도와 review evidence 기준 visual fidelity를 계속 sanity check 한다.
- shared `Account/Garage` lane에서는 Garage save/load WebGL, settings interaction, save action 접근성을 계속 추적한다.
- Stitch lane 쪽은 `Set A/B/C/D/E`와 추가 `GameScene HUD` source freeze를 generic onboarding 기준 샘플로 삼아, 이후 다시 여는 inventory set을 per-surface script edit 없이 단순 범용 루프의 verdict까지 태울 수 있게 source facts/draft/validate route를 일반화한다.
- 외부 디자인 시안은 `Stitch`를 기본 생성 도구로 두고, 실제 반영은 Unity MCP와 scene/prefab contract 기준으로 번역한다.

## 상세 이력

- dated change log와 이전 구현 메모는 [`progress_changelog.md`](./progress_changelog.md) 에서 본다.
- 자세한 Phase별 작업 항목은 [`game_scene_entry_plan.md`](./game_scene_entry_plan.md) 참고.
- 계정 시스템 상세 계획은 [`account_system_plan.md`](./account_system_plan.md) 참고.
