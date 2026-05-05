# Runtime Validation Checklist

> 마지막 업데이트: 2026-05-05
> 상태: active
> doc_id: playtest.runtime-validation-checklist
> role: reference
> owner_scope: Lobby, Garage, Game/BattleScene 진입과 전투 루프의 수동 runtime validation checklist template, GameScene multiplayer residual closeout checklist
> upstream: docs.index, ops.unity-ui-authoring-workflow
> artifacts: none

하네스는 runtime UI flow를 자동 스모크하지 않는다. 이 문서는 Lobby/Garage/Game 진입 관련 회귀를 수동으로 확인하고 기록하는 checklist reference다.
GameScene 2-client multiplayer residual은 별도 active plan을 만들기 전까지 이 checklist가 current owner로 맡는다. 실제 실행 evidence나 artifact 전용 소유자가 열리면 그 plan 또는 artifact로 넘긴다.

씬 구조, Inspector wiring, Bootstrap 순서의 SSOT는 `*Setup.cs`, `*Bootstrap.cs`, 실제 scene/prefab serialized contract, MCP contract verification route가 소유한다. 이 문서는 hierarchy path가 아니라 "행동/결과" 기준으로만 적는다.

## Active Owner Boundary

- 이 문서는 현재 `GameScene / Multiplayer Sync` residual의 checklist owner다.
- 2-client runner 구현, long-lived evidence directory, 또는 여러 세션의 fix plan이 열리면 새 active plan으로 넘기고 이 문서는 reference checklist로 유지한다.
- runner가 없으면 `blocked: two-client runner unavailable`을 유지하되, 실제 blocker owner와 다음 확보 방법만 짧게 남긴다.

---

## 메타

| 항목 | 값 |
|---|---|
| 날짜 | |
| 브랜치/커밋 | |
| 빌드/에디터 | |
| 플레이 인원 | |
| 검증자 | |
| 판정 | success / blocked / mismatch |

---

## 체크리스트

| 항목 | 기대 결과 | 결과 (Pass/Fail/Skip/Blocked/Mismatch) | 메모 |
|---|---|---|---|
| Lobby 진입 후 기본 UI 렌더링 | Lobby 진입 직후 핵심 패널과 기본 상호작용이 정상적으로 보인다. | | |
| Garage 탭 진입/복귀 | Garage 탭으로 이동했다가 Lobby로 돌아와도 화면과 입력 상태가 깨지지 않는다. | | |
| invalid draft 격리 | 유효하지 않은 draft 편집이 committed roster를 오염시키지 않는다. | | |
| valid draft 저장 반영 | 유효한 draft 저장 후 다시 열었을 때 최신 roster가 반영된다. | | |
| `Clear` 동작 | `Clear` 뒤 roster count가 기대한 만큼 감소한다. | | |
| Ready auto-cancel / Ready gating | 조건이 깨지면 Ready가 자동 해제되고, 조건 충족 전에는 Ready/Start 흐름이 막힌다. | | |
| BattleScene 직접 진입 | BattleScene을 직접 실행해도 HUD, energy, unit slot, placement area가 렌더링되고 새 console error가 없다. | | |
| Lobby -> BattleScene 진입 | 로비 경로로 전투에 들어가도 직접 진입과 같은 HUD/input 상태가 보인다. | | |
| 슬롯 선택 -> 배치 프리뷰 | 슬롯 선택 직후 배치 영역이 활성화되고 expected spawn point, anchor radius, attack range preview state가 갱신된다. | | |
| tap placement 소환 | 슬롯 선택 후 배치 가능 영역을 탭하면 소환 피드백이 보이고 preview/선택 상태가 정리된다. | | |
| drag placement 소환 | 슬롯 drag 중 배치 가능/불가 상태가 바뀌고, drop 성공/실패 뒤 preview/선택 상태가 정리된다. | | |
| 에너지 부족/충분 표시 | 에너지 부족 슬롯은 비활성 affordance와 오류 피드백이 명확하고, 충분 슬롯은 선택 가능하다. | | |
| 소환 유닛 anchor 자동 교전 | 소환된 유닛이 자기 anchor radius 안의 적을 획득해 이동/공격하고, 반경 밖 적을 무한 추적하지 않는다. | | |
| enemy target priority | core, unit, player 후보가 있을 때 enemy가 `core -> unit -> player` 우선순위로 target을 선택하거나, 다른 결과면 mismatch로 남긴다. | | |
| BattleEntity 상태 전이 | target acquire -> move/attack -> damage/death 또는 target switch 흐름이 최소 1회 재현되고 dead/despawn target을 다시 물지 않는다. | | |
| Wave/Core runtime loop | wave start -> enemy spawn -> core damage -> victory/defeat 중 하나의 종료 상태까지 runtime event가 이어진다. | | |
| Wave/Core HUD 유지 | wave 시작, core damage, victory/defeat까지 wave/core/result HUD가 사라지거나 겹치지 않는다. | | |
| Result HUD actual player-flow | actual player-flow에서 victory 또는 defeat 이후 result HUD 통계/CTA가 보이고 lobby return이 동작한다. direct/EditMode test만으로 success 처리하지 않는다. | | |
| late-join hydration | 2-client 검증 시 joiner가 기존 BattleEntity HP/position/dead state, Energy, WaveState를 복구하거나 blocked owner를 남긴다. | | |
| 2-client sync evidence | host/joiner 2개 클라이언트에서 late-join, BattleEntity, Energy, WaveState 결과를 각각 기록한다. runner가 없으면 `blocked: two-client runner unavailable`을 유지한다. | | |
| 중복 event 방지 | summon, damage, death, reward/result가 같은 입력 1회에 중복 발행되지 않는다. | | |
| GameScene blocker owner 분리 | 실패 시 runtime event/state 문제인지 HUD/input/prefab 문제인지 메모에 분리한다. | | |
| 신규 console error 없음 | 검증 중 새 `Error/Exception/Assert`가 발생하지 않는다. | | |

---

## GameScene 기록 기준

- `success`: current owner나 residual route의 acceptance를 실제 실행으로 비교했고 기대 결과와 맞는다.
- `blocked`: 실행 환경, Photon 2-client setup, scene contract, compile state 때문에 해당 owner/residual acceptance를 아직 판정할 수 없다.
- `mismatch`: 실행은 했지만 결과가 기대와 다르다. 재현 절차와 owner/residual lane을 메모에 남긴다.
- Runtime owner: summon runtime, BattleEntity, anchor combat, enemy priority, wave/core/victory-defeat, late-join runtime state.
- UI/UX owner: HUD layout, slot/input, placement preview, error/feedback, Wave/Core/result 표시.
- Phase 5 residual: 별도 active multiplayer plan 없이 이 checklist가 2-client session, late-join hydration, BattleEntity/Energy/WaveState sync smoke를 추적한다.
- 2-client evidence는 single-client actual flow, direct EditMode tests, Phase 5 preflight JSON만으로 대체하지 않는다.

---

## 기록 메모

- 재현 절차:
- 실제 결과:
- owner/residual lane:
- 관련 로그/스크린샷 경로:
- 후속 조치:
