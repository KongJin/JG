# MVP 재미 검증 — 구현 계획 (압축본)

> 마지막 업데이트: 2026-04-11
> 상태: reference
> doc_id: playtest.mvp-fun-checklist
> role: reference
> owner_scope: MVP 재미 검증용 압축 실행 체크리스트
> upstream: design.game-design, plans.progress
> artifacts: none

이 문서는 MVP 재미 검증 작업의 **실행 체크리스트**만 남긴 압축본이다. 수치, 통과 기준, MVP 문장 자체는 SSOT 문서에서만 바꾼다.

공식 진행 상태는 [`progress.md`](../plans/progress.md)에서 관리한다.

---

## SSOT 위임

| 주제 | 단일 근거 |
|------|-----------|
| 수치·통과 기준·MVP 문장·웨이브 시간표 | [game_design.md](../design/game_design.md) |
| 재미 워크숍·페르소나·질문 풀 | [discussion_game_fun_personas.md](../discussions/discussion_game_fun_personas.md) |
| 플레이 세션 노트 양식 | [playtest_mvp_template.md](./playtest_mvp_template.md) |
| Room/Player CustomProperties 키 소유권 | 실제 키를 쓰는 feature code (`Application/**`, `Infrastructure/**`) + [AGENTS.md](../../AGENTS.md) + [docs/index.md](../index.md) |
| 프로젝트 규칙 | [AGENTS.md](../../AGENTS.md), [docs/index.md](../index.md) |

---

## 이번 문서가 다루는 우선순위

1. **덱 순환 직관 검증**
2. **적 최소 변종 + 웨이브 데이터 정렬**
3. **로비 난이도 표시와 Room CustomProperties 연동**
4. **새 스킬 vs 강화 측정 로그 확인**

---

## 실행 체크리스트

### A. 덱 순환 직관

- `DeckCycleHandler`, `BarView`, `SkillSetup` 기준으로 다음 드로우 미리보기 흐름이 현재 코드 계약과 맞는지 확인
- 게임 씬/프리팹에서 `nextDrawPreviewIcon`, `nextDrawHintLabel` 직렬화 연결 확인
- 플레이 테스트 결과는 `playtest_mvp_template.md` 양식으로 남김

### B. 적 콘텐츠 믹스

- `EnemyData` 변종을 최소 1개 이상 추가해 혼합 스폰 검증이 가능하게 유지
- `DefaultWaveTable.asset`를 `game_design.md` 웨이브 목적과 맞게 정렬
- 스폰 개수 배율이 필요하면 `Wave.Application` 순수 함수로 두고 `WaveBootstrap`에는 산식·분기를 넣지 않음

### C. 로비 난이도

- Room 상태는 `CustomProperties` 단일 근원으로 유지
- Lobby write-side 상수와 Wave read-side 상수의 키 문자열을 동일하게 맞춤
- 로비 UI는 한 줄 표시와 최소 컨트롤만 유지하고, `PhotonNetwork` 직접 호출은 금지
- late-join 클라이언트가 Room 속성으로 난이도 프리셋을 복구하는지 코드 경로를 확인

### D. 측정 로그

- `[MvpReward]` 관련 로그가 `UNITY_EDITOR` 또는 `DEVELOPMENT_BUILD`에서만 노출되는지 확인
- 측정용 UI가 생겨도 집계·판정 로직은 View 밖에 둠
- 통과 임계값 변경은 `game_design.md`에서만 수행

---

## 완료 정의

- `game_design.md` 기준 핵심 측정 1회 이상 수행
- `EnemyData` 2종 이상과 웨이브 데이터가 MVP 방향에 맞게 정렬
- 로비 난이도: Room 동기화 + 인게임 반영 + 로비 한 줄 표시 완료
- 새 Room 키의 owner/read 경로와 관련 문서 반영 완료

---

## 하지 않을 것

- 이 문서만 수정해서 수치·MVP 문장을 바꾸지 않는다
- Room 상태를 `CustomProperties` 외 별도 채널에 이중 저장하지 않는다
- `PhotonTransformView` 등 우회 채널로 난이도만 따로 동기화하지 않는다
