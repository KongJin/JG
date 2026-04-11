# 기술 부채 심각도 리뷰 (20년차 개발자 시점)

> **작성일**: 2026-04-11  
> **검토 범위**: `Assets/Scripts/Features/`, `Assets/Scripts/Shared/`, `Tests/`  
> **아키텍처 기준**: Feature-first Clean Architecture (Domain, Application, Presentation, Infrastructure)
> **수정일**: 
> - 2026-04-11 — Critical #1, #2, #3 해결 완료, README 전면 제거
> - 2026-04-11 — High #5, #6, #7 해결 완료
> - 2026-04-11 — Low #14 네이밍 불일치: Bootstrap→Setup 전면 통합, 2계층→1계층 통합

---

## 🚨 치명적 (CRITICAL) — 즉시 해결 필요

### ~~1. **아키텍처 위반: `GetComponent` 사용~~ ✅ 해결됨
- **해결**: `PlayerSetup.LocalArrived` static 이벤트 패턴으로 변경. `GameSceneRoot`가 `GetComponent` 없이 `_localPlayerSetup`을 받음.

### ~~2. **사망 코드: Mana 시스템 잔존~~ ✅ 해결됨
- **해결**: `Player.cs`에서 `MaxMana`, `CurrentMana`, `SpendMana`, `RegenMana` 제거. `IPlayerNetworkCommandPort.cs`에서 `SyncMana` 제거. `PlayerNetworkAdapter.cs`에서 `SyncMana` 구현 제거. `ManaRegenTicker.cs` 파일 삭제.

### ~~3. **과도한 런타임 null 체크~~ ✅ 해결됨
- **해결**: `PlayerHealthHudView.cs`에서 `[Required]` 필드에 대한 null 체크 제거. `CombatBootstrap.cs`, `PlayerSceneRegistry.cs`는 메서드 파라미터 체크이므로 유지 (방어적 코딩으로 정당).

### ~~4. **README 누락 (11개 중 7개)~~ ✅ 전면 제거로 해결
- **해결**: README를 문서화 정책에서 제거. 대신 `/agent/*.md` 전역 규칙과 각 feature의 `*Setup.cs` / `*Bootstrap.cs`를 SSOT로 활용.

---

## ⚠️ 높음 (HIGH) — 단기 해결

### ~~5. **하드코딩 Player Spec~~ ✅ 해결됨
- **해결**: `PlayerSpecConfig` ScriptableObject 생성. `DefaultPlayerSpecProvider`가 `Resources.Load`로 Config를 로드하여 Inspector에서 밸런스 조정 가능. Config가 없으면 기본값 폴백.

### ~~6. **UI/UX 미완성~~ ✅ 해결됨
- **UnitSlotView**: `FrameId` 대신 `DisplayName` 표시. `Unit` 도메인에 `DisplayName` 프로퍼티 추가.
- **UnitSlotsContainer**: `Vector3.zero` 대신 `PlacementArea.Center` 기반 소환 위치 사용. 슬롯 UI 위치도 `RectTransform.anchoredPosition`으로 계산.

### ~~7. **Firebase Analytics 스텁 가능성~~ ✅ 확인 완료
- **결과**: 스텁 아님. `UNITY_WEBGL && !UNITY_EDITOR` 조건부 컴파일로 WebGL 빌드에서는 실제 Firebase JS SDK 호출. 에디터에서는 디버깅용 콘솔 로그. 주석 추가로 명확화.

### 8. **Debug.Log 과다 사용**
- 프로젝트 코드 전반에 60+개. Production에서 로그 노이즈

### 9. **Infrastructure의 무거운 MonoBehaviour**
- 15개 클래스가 MonoBehaviour 상속. Scene 수동 배치 부담

---

## 📋 중간 (MEDIUM) — 중기 해결

### 10. **테스트 커버리지 극히 부족**
- **테스트 파일**: 단 3개 (GarageRoster, CostCalculator, UnitComposition)
- **미테스트**: Combat, Enemy, Player, Projectile, Skill, Status, Wave, Zone, Lobby, EventBus 등 전체
- **위험도**: 리팩토링 안전망 부족

### 11. **이벤트 체인 깊이 초과**
- **규칙**: 최대 3단계
- **실제**: 5단계 체인 존재 (SkillCasted → ProjectileRequested → ProjectileHit → DamageApplied → PlayerHealthChanged)
- **해결**: 중간 단계 UseCase 직접 호출로 대체 필요

### 12. **Phase 9 미완료**
- 명시적 Death Sync (`[PunRPC]`) 미구현
- Late-join 시 사망 상태 동기화 보장 안됨

### 13. **멀티플레이어 스모크 테스트 부재**
- Late-join, BattleEntity sync, Energy sync 실제 멀티 검증 안됨

---

## 💡 낮음 (LOW) — 기회 있을 때

### ~~14. **네이밍 불일치~~ ✅ 전면 통합 완료
- **결과**: 모든 `*Bootstrap` → `*Setup`으로 통일.
  - `LobbyBootstrap` → `LobbySetup`
  - `CombatBootstrap` → `CombatSetup`
  - `WaveBootstrap` → `WaveSetup`
  - `CoreObjectiveBootstrap` → `CoreObjectiveSetup`
  - `UnitBootstrap` + `UnitSetup(순수C#)` → 통합 `UnitSetup`(MonoBehaviour, 1계층)
  - `GarageBootstrap` + `GarageSetup(순수C#)` → 통합 `GarageSetup`(MonoBehaviour, 1계층)
- **규칙**: `*Setup` = 씬 레벨 wiring + Composition Root (유일한 명명 규칙)

### ~~15. **Obsolete 파일~~ ✅ 해결됨
- `ManaRegenTicker.cs` 삭제 완료 (Critical #2)

### 16. **Shared README 부족**
- SoundPlayer, GameObjectPool 등 신규 공통 유틸 미기술

---

## 📊 요약 지표

| 지표 | 값 |
|------|-----|
| Feature 전체 | 11개 |
| 테스트 파일 | 3개 |
| TODO/FIXME (프로젝트 코드) | ~2개 (Low #16 만 남음) |
| GetComponent 위반 | 0개 ✅ |
| 사망 코드 (Mana) | 0개 ✅ |
| 하드코딩 Spec | 0개 ✅ (ScriptableObject 기반 외부화) |
| UI TODO (FrameId/Vector3.zero) | 0개 ✅ |
| 네이밍 불일치 | ✅ 전면 통합 (Bootstrap→Setup) |
| Debug.Log 계열 | 60+개 |
| Infrastructure MonoBehaviour | 15개 |
| Phase 9 완료 | 부분 완료 (Death Sync 미구현) |

---

## 🎯 우선순위 제언

1. **즉시**: Mana 시스템 제거, GetComponent 수정
2. **이번 스프린트**: README 7개 작성, Firebase 스텁 확인, 하드코딩 Spec 외부화
3. **다음 스프린트**: 테스트 10개 이상 추가, Phase 9 Death Sync 구현
4. **지속**: 이벤트 체인 리팩토링, Debug.Log 정리

---

## 📝 전체 소감

전체적으로 **아키텍처는 잘 설계되어 있으나, 실행과 문서화가 따라가지 못하는 상태**입니다. Phase 9 완료와 멀티플레이어 검증이 시급합니다.
