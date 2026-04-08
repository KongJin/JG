## Qwen Added Memories

### Phase 4 완료 (재소환 시스템)
**이전 세션에서 완료됨**: Phase 0, 1, 2, 3 (컴파일 수정, Unit/Garage 통합, 소환 시스템 기본 구축, Wave/Enemy와 Unit 연결)
**Phase 4 완료됨**: 재소환 시스템

#### Phase 4 완료 요약:
1. **UnitDiedEvent** — Presentation → Application/Events로 이동 (레이어 규칙 준수)
2. **UnitDiedEvent 발행** — `BattleEntityPhotonController.OnDamageApplied()`에서 HP <= 0 시 발행
3. **UnitDeathEventHandler** 생성 — `UnitDiedEvent` 구독, `PhotonNetwork.Destroy`로 프리펩 파괴
4. **재소환 경로** — `SummonUnitUseCase` 재사용 (쿨다운 없음, 에너지 차감 후 동일 UseCase)
5. **UnitSlotView** — 소환 슬롯 UI (아이콘+이름+비용+에너지 부족 오버레이)
6. **UnitSlotsContainer** — 3개 표시 슬롯 컨테이너
7. **GameSceneRoot** — `InitializeSummonSlots()`로 소환 UI 연결

#### 신규 파일:
- `Assets/Scripts/Features/Unit/Application/UnitDeathEventHandler.cs`
- `Assets/Scripts/Features/Unit/Presentation/UnitSlotView.cs`
- `Assets/Scripts/Features/Unit/Presentation/UnitSlotsContainer.cs`

#### 수정된 파일:
- `Assets/Scripts/Features/Unit/Application/Events/UnitSummonEvents.cs` — UnitDiedEvent 추가
- `Assets/Scripts/Features/Unit/Presentation/BattleEntityView.cs` — UnitDiedEvent import 경로 수정
- `Assets/Scripts/Features/Unit/Infrastructure/BattleEntityPhotonController.cs` — UnitDiedEvent 발행 추가
- `Assets/Scripts/Features/Unit/BattleEntitySetup.cs` — UnitDeathEventHandler 연결
- `Assets/Scripts/Features/Player/GameSceneRoot.cs` — UnitSlotsContainer 필드 + InitializeSummonSlots 추가

#### 미완료/향후 작업 (TODO):
- UnitSlotView 배치 영역 선택 (드래그 앤 드롭) — 현재 클릭 소환만
- UnitSlotsContainer 로테이션 완성 (6개 중 3개 표시 전환)
- 네트워크 재소환 (Phase 5) — Late-join, BattleEntity 상태 동기화
- `CreateTemporaryUnitSpec()` — UnitCatalog에서 실제 스펙 조회 필요

**다음 세션 시작점**: Phase 5 — 네트워크 동기화

시작 시 참조 문서:
- `docs/plans/game_scene_entry_plan.md` — 전체 로드맵 (Phase 5 섹션)
- `docs/design/game_design.md` — 게임 디자인 SSOT
- `agent/architecture.md` — 아키텍처 규칙
