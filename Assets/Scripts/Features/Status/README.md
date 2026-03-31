# Status Feature

런타임 버프/디버프 상태 레이어를 담당한다. PlayerSpec을 직접 수정하지 않고, 별도의 상태 컨테이너로 효과를 관리한다.

## 현재 책임

- 상태 적용(Apply), 갱신(Refresh), 만료(Expire), 틱 피해(Burn) 처리
- 중첩 규칙: Refresh(Haste, Slow), Independent(Burn 최대 3스택, Expand/Extend/Multiply/Count 최대 10스택)
- 이동속도 변경(Haste/Slow)을 Player에 제공 (`ISpeedModifierPort`)
- 범위/지속/개수 변경(Expand/Extend/Multiply)을 Skill에 제공 (`IStatusQueryPort`)
- 네트워크 동기화: 적용/틱 데미지를 RPC로 복제

## v1 포함 상태

| 상태 | 효과 | 중첩 | 소비자 |
|---|---|---|---|
| Haste | 이동속도 증가 | Refresh | Player (자체 완결) |
| Slow | 이동속도 감소 | Refresh | Player (자체 완결) |
| Burn | 지속 피해 | Independent, 최대 3스택 | Status 자체 (틱 데미지 발행) |
| Expand | 범위 증가 | Independent, 최대 10스택 | Skill (발동 시 조회) |
| Extend | 쿨다운 감소 | Independent, 최대 10스택 | Skill (발동 시 조회) |
| Multiply | 데미지 증폭 | Independent, 최대 10스택 | Skill (발동 시 조회) |
| Count | 발사/스폰 수 증가 | Independent, 최대 10스택 | Skill (발동 시 조회) |

## 데이터 흐름

### 상태 적용

```text
StatusApplyRequestedEvent (다른 피처에서 발행)
  → StatusEventHandler.OnApplyRequested()
    → StatusUseCases.ApplyStatus()
      → StatusContainer.Apply(effect) — 중첩 규칙 적용
      → IStatusNetworkCommandPort.SendApplyStatus() — RPC 전송
      → StatusAppliedEvent 발행
```

### 트리거 경로 (StatusTriggerHandler)

```text
ProjectileHitEvent (StatusPayload 포함)
  → StatusTriggerHandler.OnProjectileHit()
    → StatusApplyRequestedEvent 발행

SelfRequestedEvent (SkillSpec.StatusPayload 포함)
  → StatusTriggerHandler.OnSelfRequested()
    → StatusApplyRequestedEvent 발행 (CasterId = TargetId)

ZoneTickEvent (StatusPayload 포함)
  → StatusTriggerHandler.OnZoneTick()
    → StatusApplyRequestedEvent 발행
```

StatusPayload는 SkillData에서 설정되어 SkillSpec → 네트워크 데이터 → 이벤트를 거쳐 전달된다.

### 틱 처리 (매 프레임)

```text
StatusTickController.Update()
  → StatusTickUseCase.Tick(deltaTime)
    → 각 StatusEffect.Tick(deltaTime)
    → Burn 틱 판정 (Master만): StatusTickDamageEvent + RPC
    → 만료된 효과 제거: StatusExpiredEvent 발행
```

### 이동속도 소비 (Player)

```text
PlayerUseCases.Move()
  → ISpeedModifierPort.GetModifiedSpeed(playerId, baseSpeed)
    → SpeedModifierAdapter → StatusContainer.GetCombinedMagnitude(Haste/Slow)
    → StatusRule.ApplySpeedModifier(base, haste, slow)
  → Player.CalculateMovement(input, deltaTime, modifiedSpeed)
```

### 스킬 파라미터 소비 (Skill)

```text
CastSkillUseCase.Execute()
  → IStatusQueryPort.GetMagnitude(casterId, Expand)
    → StatusQueryAdapter → StatusContainer.GetCombinedMagnitude(Expand)
  → range *= (1 + expandMagnitude), radius *= (1 + expandMagnitude)
```

## 네트워크 모델

| 행위 | 권한자 | 방식 |
|---|---|---|
| 상태 적용 | 트리거 소유자 | RPC (SendApplyStatus) |
| Burn 틱 데미지 | Master | RPC (SendTickDamage) |
| 상태 만료 | 각 클라이언트 | 로컬 타이머 (적용 시 duration 포함) |

## 씬 의존성

- `StatusSetup`은 GameSceneBootstrap과 같은 씬에 배치
- `StatusTickController`는 `StatusSetup`이 Inspector에서 참조
- `StatusNetworkAdapter`는 플레이어 프리팹에 컴포넌트로 부착 (PhotonView 공유)
- GameSceneBootstrap이 로컬 플레이어의 `StatusNetworkAdapter`를 `StatusSetup.Initialize()`에 전달
- 원격 플레이어가 접속하면 `GameSceneBootstrap.ConnectPlayer()`에서 `StatusSetup.RegisterRemoteCallbackPort()`로 해당 adapter의 콜백도 연결
- `StatusNetworkEventHandler.WireCallbackPort()`가 여러 adapter의 RPC 콜백을 동일한 `StatusUseCases`로 라우팅

## 레이어 메모

- **Domain**: `StatusType`, `StackPolicy`, `StatusEffect`, `StatusContainer`, `StatusRule`, `StatusPayload`, `GrowthRule`
- **Application**: `StatusUseCases`, `StatusTickUseCase`, `StatusEventHandler`, `StatusTriggerHandler`, `StatusNetworkEventHandler`, `StatusContainerRegistry`, `SpeedModifierAdapter`, `StatusQueryAdapter` (`IStatusQueryPort` + `IUpgradeQueryPort` 구현), 이벤트 4종, 포트 2종 (`IStatusNetworkCommandPort`, `IStatusNetworkCallbackPort`)
- **Infrastructure**: `StatusNetworkAdapter` (CommandPort + CallbackPort 구현, 플레이어 프리팹에 부착)
- **Presentation**: `StatusTickController` (Update 루프에서 틱 orchestration)
- **Bootstrap**: `StatusSetup` (순수 조립 — `StatusQuery`, `UpgradeQuery` 프로퍼티 노출, 비즈니스 로직 없음)

## 피처 의존성

### Status가 의존하는 피처

- **Player**: `ISpeedModifierPort` 구현을 위해 Player Application 포트 참조
- **Skill**: `IStatusQueryPort` 구현을 위해 Skill Application 포트 참조
- **Wave**: `IUpgradeQueryPort` 구현을 위해 Wave Application 포트 참조
- **Shared**: `EventBus`, `DomainEntityId`, `DisposableScope`

### Status에 의존하는 피처

- **Player**: `ISpeedModifierPort`를 통해 이동속도 수정 (선택적 의존)
- **Skill**: `IStatusQueryPort`를 통해 스킬 파라미터 수정 (선택적 의존)
- **Wave**: `IUpgradeQueryPort`를 통해 업그레이드 스택 조회 (선택적 의존)
- **Projectile, Zone, Skill 등**: `StatusApplyRequestedEvent`를 발행하여 상태 적용 요청

## 소비자 포트 위치

| 포트 | 정의 위치 | 구현 위치 |
|---|---|---|
| `ISpeedModifierPort` | Player/Application/Ports | Status/Application (SpeedModifierAdapter) |
| `IStatusQueryPort` | Skill/Application/Ports | Status/Application (StatusQueryAdapter) |
| `IUpgradeQueryPort` | Wave/Application/Ports | Status/Application (StatusQueryAdapter) |
