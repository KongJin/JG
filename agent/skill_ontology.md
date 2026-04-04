# Skill Ontology — 다축 스킬 분류 체계

## 범위

이 문서는 **LoL 급 복잡도의 스킬을 분류할 수 있는 다축 택소노미**를 정리하되,
**JG(카오스 코옵) MVP에서의 적용 범위와 현재 코드 매핑**을 함께 기술한다.

- LoL 전체 분석용 학술 문서가 아니라, JG 스킬 확장 시 **설계 기준선** 역할을 한다.
- MVP(8스킬)에서 실제로 구분이 필요한 축은 **코어 축**으로, 나머지는 **확장 축**으로 표기한다.
- 코드 enum/SO 필드로의 반영은 Phase 2(스킬 태그 시너지) 시점에서 선택적으로 진행한다.
- **성장(Growth):** Count/Range/Duration/Safety는 "스킬 분류"가 아니라 "성장 시스템"이므로 이 문서에 포함하지 않는다. → [game_design.md](game_design.md) § 성장 구조 참조.

---

## 축 구조 개요

| 계층 | 축 | 핵심 질문 | JG MVP 활용 |
|------|------|-----------|-------------|
| **코어** | EffectRole | 이 스킬은 무엇을 하는가? | 사용 |
| **코어** | Delivery | 효과가 어떻게 전달되는가? | 사용 (코드 `DeliveryType`) |
| **코어** | TargetSelection | 누구/어디를 향하는가? | 사용 (코드 `DeliveryType` + 프렌들리 파이어) |
| **코어** | TemporalProfile | 얼마나 지속되는가? | 사용 (코드 `duration` 필드) |
| **코어** | PlayerInputPattern | 플레이어가 어떻게 입력하는가? | 부분 사용 (방향/위치/셀프) |
| 확장 | CastPipeline | 시전 중 어떤 단계를 거치는가? | 미사용 (MVP 전부 Instant) |
| 확장 | Trigger | 언제 평가/발동되는가? | 미사용 (조건부 스킬 없음) |
| 확장 | Requirement | 발동 가능 조건은 무엇인가? | 미사용 |
| 확장 | StateMutation | 스킬 슬롯/폼이 어떻게 바뀌는가? | 미사용 |
| 보조 | CostCommitment | 자원 소모/환급 규칙 | 부분 (마나만) |
| 보조 | ScalingShape | 데미지 스케일링 종류 | 미사용 (단일 `damage` 값) |
| 보조 | FilterValidity | 대상 필터 (아군/적/미니언 등) | 부분 (프렌들리 파이어 감쇠) |

---

## 코어 축 상세

### 1. EffectRole — "이 스킬은 무엇을 하는가"

스킬이 세계에 미치는 **주된 효과**를 분류한다. 하나의 스킬이 여러 Role을 동시에 가질 수 있다.

**상위 enum**

| 값 | 설명 |
|----|------|
| Damage | 대상에게 피해를 준다 |
| Heal | 체력을 회복한다 |
| Shield | 보호막을 부여한다 |
| CrowdControl | 이동/행동을 제한한다 |
| Move | 자신 또는 대상을 이동시킨다 |
| Buff | 아군에게 유리한 상태를 부여한다 |
| Debuff | 적에게 불리한 상태를 부여한다 |
| Summon | 오브젝트/유닛을 소환한다 |
| Vision | 시야를 제공하거나 제거한다 |
| Utility | 위 어디에도 해당하지 않는 보조 효과 |

**서브축: DamageDetail** (Role에 Damage가 포함될 때)

| 값 | 설명 |
|----|------|
| Physical | 물리 데미지 |
| Magical | 마법 데미지 |
| True | 고정 데미지 |
| MaxHP | 최대 체력 비례 |
| CurrentHP | 현재 체력 비례 |
| MissingHP | 잃은 체력 비례 |
| Execute | 처형형 (체력 구간 조건) |
| Percent | 퍼센트 기반 |
| Hybrid | 복합 |

**서브축: CrowdControlDetail** (Role에 CrowdControl이 포함될 때)

| 값 | 설명 |
|----|------|
| Stun | 경직 |
| Root | 이동 불가 |
| Knockup | 공중으로 띄움 |
| Knockback | 밀어냄 |
| Pull | 당김 |
| Fear | 공포 (무작위 이동) |
| Charm | 매혹 (시전자 쪽으로 이동) |
| Taunt | 도발 (시전자를 공격) |
| Silence | 침묵 (스킬 사용 불가) |
| Disarm | 무장 해제 (기본 공격 불가) |
| Blind | 실명 (기본 공격 빗나감) |
| Slow | 둔화 |
| Suppression | 억압 (행동+이동 불가, QSS로만 해제) |
| Sleep | 수면 (피격 시 해제) |

**원안에서 제거한 항목과 이유:**
- `Transform` → **StateMutation**(확장 축)으로 이동. Role은 "세계에 미치는 효과"만 담당.
- `Resource` → **CostCommitment**(보조 축)에서 다룸. 마나 회복도 Heal/Buff로 충분히 태깅 가능.

---

### 2. Delivery — "효과가 어떻게 전달되는가"

효과가 시전자에서 대상까지 **물리적으로 어떤 경로**로 도달하는지를 분류한다.

**상위 enum**

| 값 | 설명 | JG 코드 대응 |
|----|------|-------------|
| Instant | 즉시 적용 (사거리 내) | `DeliveryType.Targeted`, `.Self` |
| Projectile | 발사체가 날아감 | `DeliveryType.Projectile` |
| Hitscan | 즉시 직선 판정 | - |
| Area | 영역에 효과 발생 | `DeliveryType.Zone` |
| Aura | 캐릭터 주변 지속 영역 | - |
| Beam | 연속 직선 광선 | - |
| Dash | 자신이 돌진하며 효과 | - |
| Trap | 설치 후 조건부 발동 | - |
| Chain | 대상 간 연쇄 전이 | - |
| Zone | 영역 지속 설치 | `DeliveryType.Zone` |
| SummonProxy | 소환체가 대신 전달 | - |

**서브축: ProjectileDetail**

| 값 | 설명 | JG 코드 대응 |
|----|------|-------------|
| Linear | 직선 | `TrajectoryType` |
| Arc | 곡사 | `TrajectoryType` |
| Homing | 유도 | `TrajectoryType` |
| Returning | 왕복 (부메랑) | `TrajectoryType` |
| Bounce | 튕김 | `TrajectoryType` |
| Split | 분열 | - |
| Piercing | 관통 | `HitType` |

**서브축: AreaDetail**

| 값 | 설명 |
|----|------|
| Circle | 원형 |
| Cone | 부채꼴 |
| Line | 직선 띠 |
| Ring | 고리형 |
| Global | 맵 전체 |

**모호성 규칙:**
- `Dash`는 Delivery이면서 EffectRole=Move가 동시에 붙을 수 있다. 한쪽에 억지로 합치지 않는다.
- `Chain`은 TargetSelection의 TargetRule=Nearest/Priority와 함께 태깅하면 점프 규칙이 명확해진다.

---

### 3. TargetSelection — "누구/어디를 향하는가"

**상위 enum**

| 값 | 설명 |
|----|------|
| Self | 자기 자신 |
| Ally | 아군 |
| Enemy | 적 |
| Ground | 지점 (캐릭터 무관) |
| Object | 오브젝트 (와드, 터렛 등) |
| None | 대상 없음 (패시브 등) |

**서브축: TargetCount**

| 값 | 설명 |
|----|------|
| Single | 단일 대상 |
| Multi | 복수 대상 (상한 있음) |
| Unlimited | 범위 내 전부 |

**서브축: TargetRule** (Multi/Unlimited일 때 우선순위)

| 값 | 설명 |
|----|------|
| Nearest | 가장 가까운 순 |
| LowestHP | 체력 낮은 순 |
| HighestHP | 체력 높은 순 |
| MarkedOnly | 마크된 대상만 |
| Random | 무작위 |
| Priority | 우선순위표 참조 |

**서브축: CollisionType** (투사체/빔의 충돌 규칙)

| 값 | 설명 |
|----|------|
| UnitCollision | 유닛에 막힘 |
| TerrainCollision | 지형에 막힘 |
| NoCollision | 관통 |

---

### 4. TemporalProfile — "얼마나 지속되는가"

**상위 enum**

| 값 | 설명 |
|----|------|
| Instant | 즉시 1회 |
| Timed | 일정 시간 후 종료 |
| Periodic | 일정 간격 반복 (틱) |
| Persistent | 조건 충족까지 유지 |
| Stack | 중첩 기반 |
| Infinite | 명시적 해제까지 영구 |

**서브축: TickType** (Periodic일 때)

| 값 | 설명 |
|----|------|
| None | 틱 없음 |
| FixedInterval | 고정 간격 |
| OnEvent | 특정 이벤트 발생 시 |

**서브축: ExpireCondition**

| 값 | 설명 |
|----|------|
| Time | 시간 만료 |
| StacksEnd | 스택 소진 |
| OnTrigger | 조건 발동 시 |
| OnDeath | 대상 사망 시 |
| Manual | 재시전/토글로 해제 |

**겹침 방지 규칙:** ExpireCondition의 `OnTrigger`가 **Trigger** 축(확장)과 겹칠 수 있다. 이 경우 "어떤 이벤트인가"는 **Trigger** 축에, "그 결과 만료된다"는 TemporalProfile에 적는다.

---

### 5. PlayerInputPattern — "플레이어가 어떻게 입력하는가"

**상위 enum**

| 값 | 설명 | JG MVP 해당 |
|----|------|-------------|
| Passive | 자동 발동 | - |
| Active_Target | 대상 클릭 | O (Targeted) |
| Active_Position | 지점 클릭 | O (Zone) |
| Active_Direction | 방향 지정 | O (Projectile) |
| Toggle | 켜기/끄기 | - |
| Charge | 누르는 시간에 비례 | - |
| Channel | 시전 중 유지 | - |
| Recast | 재시전으로 변화 | - |
| AutoTrigger | 조건 충족 시 자동 | - |

---

## 확장 축 상세

MVP에서는 빈 칸이 대부분이다. Phase 2(스킬 태그 시너지) 이후, 또는 LoL 스킬 분석 시에 활용한다.

### 6. CastPipeline — "시전 중 어떤 단계를 거치는가"

JG MVP의 모든 스킬은 **Instant**이므로 당장 구분이 불필요하다. 채널링/차지 스킬 도입 시 활성화.

| 값 | 설명 |
|----|------|
| Instant | 즉시 발동 |
| CastTime | 고정 시전 시간 (이동 불가) |
| Windup | 선딜 (일부 이동 가능) |
| Delayed | 지연 발동 |
| Channel | 유지 시전 (중단 가능) |

**서브축: Interruptibility**

| 값 | 설명 |
|----|------|
| Interruptible | CC로 중단 가능 |
| Uninterruptible | 중단 불가 (언스톱) |

---

### 7. Trigger — "언제 평가/발동되는가"

패시브·조건부 스킬의 **발동 시점**을 기술한다.

| 값 | 설명 |
|----|------|
| OnHit | 적중 시 |
| OnCast | 시전 시 |
| OnKill | 처치 시 |
| OnDamage | 피해를 받을 때 |
| OnMove | 이동 시 |
| OnDash | 돌진 시 |
| OnState | 특정 상태 진입/이탈 시 |
| OnStack | 스택 도달 시 |
| OnTime | 시간 경과 시 |

---

### 8. Requirement — "발동 가능 조건은 무엇인가"

Trigger와 구분: Trigger는 **"언제"**, Requirement는 **"가능한가"**.
하나의 스킬에 Trigger=OnHit + Requirement=LowHP 이면 "적중 시 + 대상 체력이 낮을 때만" 발동.

| 값 | 설명 |
|----|------|
| HasMark | 마크 보유 |
| HasBuff | 특정 버프 보유 |
| LowHP | 체력 하한 이하 |
| HighHP | 체력 상한 이상 |
| InRange | 사거리 이내 |
| OutOfRange | 사거리 밖 |
| BehindTarget | 대상 뒤쪽 |
| FacingTarget | 대상 정면 |
| InBrush | 수풀 안 |
| NearWall | 벽 근처 |

**소모 서브축: Consume** (발동 시 소모)

| 값 | 설명 |
|----|------|
| ConsumeMark | 마크 소모 |
| ConsumeResource | 자원 소모 |
| ConsumeStack | 스택 소모 |

---

### 9. StateMutation — "스킬 슬롯/폼이 어떻게 바뀌는가"

| 값 | 설명 |
|----|------|
| RecastEnable | 재시전 활성화 |
| RecastChange | 재시전 시 스킬 변경 |
| FormSwap | 폼 전환 (스킬셋 교체) |
| WeaponSwap | 무기 교체 |
| Upgrade | 스킬 강화/진화 |
| SkillReplace | 스킬 완전 교체 |
| PassiveToActive | 패시브→액티브 전환 |
| ActiveToPassive | 액티브→패시브 전환 |

**규칙:** StateMutation은 보통 **Trigger 축의 OnX**에 매달린다. 예: "OnKill → RecastEnable". 별도 TriggerType 서브축은 두지 않고 Trigger 축을 참조한다.

---

## 보조 축

### 10. CostCommitment — 자원 소모/환급

| 항목 | 설명 | JG MVP |
|------|------|--------|
| Mana | 마나 소모 | O (`manaCost` 필드) |
| Energy | 에너지 소모 | - |
| Cooldown | 쿨다운 | - (덱 순환이 대체) |
| Charge | 충전 횟수 | - |
| Health | 체력 소모 | - |
| Refund | 캔슬/조건 시 환급 | - |

### 11. ScalingShape — 데미지 스케일링 종류

| 항목 | 설명 | JG MVP |
|------|------|--------|
| FlatDamage | 고정 수치 | O (`damage` 단일 필드) |
| AD_Ratio | 공격력 비례 | - |
| AP_Ratio | 주문력 비례 | - |
| HP_Ratio | 체력 비례 | - |
| Level_Scaling | 레벨 비례 | - |

### 12. FilterValidity — 대상 필터

| 항목 | 설명 | JG MVP |
|------|------|--------|
| ChampionOnly | 챔피언만 | - |
| MinionOnly | 미니언만 | - |
| AllyDamage | 아군 피해 적용 | O (프렌들리 파이어 감쇠) |
| Untargetable | 무적 대상 관통 여부 | - |
| VisionRequired | 시야 필요 여부 | - |

---

## 축 간 겹침 방지 규칙

| 겹치기 쉬운 쌍 | 규칙 |
|----------------|------|
| Delivery=Dash + EffectRole=Move | 둘 다 태깅. 억지로 한쪽에 합치지 않는다 |
| Delivery=Chain + TargetSelection | Chain은 Delivery에, 점프 우선순위는 TargetRule에 |
| TemporalProfile.ExpireCondition + Trigger | "어떤 이벤트인가"는 Trigger, "만료 결과"는 TemporalProfile |
| StateMutation + Trigger | StateMutation의 발동 시점은 Trigger 축 참조. 별도 TriggerType 서브축 없음 |
| CostCommitment.Refund + CastPipeline | 캔슬 시 환급 규칙은 CostCommitment에, 캔슬 가능 여부는 CastPipeline에 |

---

## JG 코드 매핑 표

현재 코드(`SkillData.cs`, `DeliveryResult.cs`, `StatusEffectData`)와 온톨로지 축의 단방향 대응.

| JG 코드 | 온톨로지 축 | 값 |
|---------|-----------|-----|
| `SkillData.gameplayTags` + `SkillGameplayTagResolver` | EffectRole | `SkillGameplayTags` 플래그 (`None`이면 damage·상태·딜리버리로 추론) |
| `DeliveryType.Projectile` | Delivery | Projectile |
| `DeliveryType.Zone` | Delivery | Area 또는 Zone |
| `DeliveryType.Targeted` | Delivery=Instant, TargetSelection=Enemy | |
| `DeliveryType.Self` | Delivery=Instant, TargetSelection=Self | |
| `TrajectoryType` (Linear/Arc/Homing 등) | Delivery 서브 ProjectileDetail | 1:1 대응 |
| `HitType` (Piercing 등) | Delivery 서브 ProjectileDetail 또는 TargetSelection.CollisionType | |
| `damage` (float) | ScalingShape=FlatDamage | |
| `manaCost` (float) | CostCommitment=Mana | |
| `range` (float) | TargetSelection 거리 파라미터 (축 enum 아님) | |
| `duration` (float) | TemporalProfile=Timed 또는 Periodic의 수치 | |
| `projectileCount` (int) | TargetSelection.TargetCount=Multi (count 기반) | |
| `StatusEffectData.type` | EffectRole (Debuff/CrowdControl 등) | type에 따라 매핑 |
| `StatusEffectData.tickInterval` | TemporalProfile.TickType=FixedInterval | |
| `GrowthAxisConfig` | **이 문서 범위 밖** → game_design.md § 성장 구조 | |
| `AllyDamageScale` | FilterValidity=AllyDamage | 감쇠 비율 |

---

## 검증: MVP 8스킬 태깅

각 스킬을 코어 축 5개로 태깅해 축이 실제로 쓸 만한지 확인한다.

| 스킬 | EffectRole | Delivery | TargetSelection | TemporalProfile | PlayerInputPattern |
|------|-----------|----------|----------------|----------------|-------------------|
| 직선 화염탄 | Damage | Projectile (Linear) | Enemy, Single, UnitCollision | Instant | Active_Direction |
| 튕기는 구체 | Damage | Projectile (Bounce) | Enemy, Multi | Instant | Active_Direction |
| 바닥 독장판 | Damage, Debuff | Zone (Circle) | Ground → Enemy, Unlimited | Periodic (FixedInterval) | Active_Position |
| 감속 구역 | CrowdControl (Slow) | Zone (Circle) | Ground → Enemy, Unlimited | Timed | Active_Position |
| 짧은 돌진 베기 | Damage, Move | Dash | Enemy, Single | Instant | Active_Direction |
| 원형 충격파 | Damage | Area (Circle) | Enemy, Unlimited | Instant | Active_Direction |
| 자기 보호막 | Shield | Instant | Self | Timed | Active_Target (Self) |
| 구조 보조 비콘 | Heal, Utility | Zone (Circle) | Ally | Timed | Active_Position |

**관찰:**
- 코어 5축으로 8스킬이 모두 구별된다. 같은 태그 조합의 스킬 쌍이 없다.
- 확장 축(CastPipeline, Trigger, Requirement, StateMutation)은 전부 해당 없음 → MVP에서 채울 필요 없음을 확인.
- `짧은 돌진 베기`에서 Delivery=Dash + EffectRole=Move 동시 태깅이 자연스럽게 동작한다.

---

## 참고 검증: LoL 스킬 태깅 (확장 축 동작 확인용)

확장 축이 실제로 기능하는지 LoL 스킬 3개로 확인한다. JG 구현과는 무관.

### 이즈리얼 Q (신비한 화살)

| 축 | 값 |
|----|-----|
| EffectRole | Damage (Physical) |
| Delivery | Projectile (Linear) |
| TargetSelection | Enemy, Single, UnitCollision |
| TemporalProfile | Instant |
| PlayerInputPattern | Active_Direction |
| CastPipeline | Instant |
| Trigger | OnHit → 모든 스킬 쿨다운 1.5초 감소 |
| Requirement | - |
| StateMutation | - |
| CostCommitment | Mana |
| ScalingShape | FlatDamage + AD_Ratio(1.3) + AP_Ratio(0.15) |

### 야스오 R (최후의 숨결)

| 축 | 값 |
|----|-----|
| EffectRole | Damage (Physical), CrowdControl (Knockup 연장) |
| Delivery | Dash (텔레포트형) |
| TargetSelection | Enemy, Multi (공중에 뜬 적만) |
| TemporalProfile | Instant |
| PlayerInputPattern | Active_Target |
| CastPipeline | Instant, Uninterruptible |
| Trigger | - |
| Requirement | **대상이 공중에 떠 있어야 함** (HasBuff=Airborne) |
| StateMutation | - |
| CostCommitment | Cooldown |
| ScalingShape | FlatDamage + AD_Ratio(2.0) |
| FilterValidity | ChampionOnly 아님 (에픽 몬스터 가능) |

### 소라카 W (성상의 주입)

| 축 | 값 |
|----|-----|
| EffectRole | Heal |
| Delivery | Instant |
| TargetSelection | Ally, Single |
| TemporalProfile | Instant |
| PlayerInputPattern | Active_Target |
| CastPipeline | Instant |
| Trigger | - |
| Requirement | **자신 HP가 5% 초과** (LowHP 역조건) |
| StateMutation | - |
| CostCommitment | **Health** (자기 체력 10% 소모, 마나 아님) |
| ScalingShape | FlatHeal + AP_Ratio(0.45) |

**관찰:**
- **Trigger**: 이즈리얼 Q의 "적중 시 쿨감"이 OnHit으로 자연스럽게 태깅된다.
- **Requirement**: 야스오 R의 "공중에 뜬 적만"과 소라카 W의 "자기 체력 5% 초과"가 각각 HasBuff, LowHP(역)로 표현된다. 두 축이 빈 칸만 채우는 게 아님을 확인.
- **CostCommitment**: 소라카 W의 "체력 소모"가 Mana와 구별되어 태깅 가능.

---

## MVP vs Phase 2 사용 경로

```
MVP (현재)
├── 코어 5축: 스킬 SO 설계 시 머릿속 체크리스트로 활용
├── JG 매핑 표: SkillData 필드 추가/변경 시 참조
└── 코드 enum 변경: 없음

Phase 2 (스킬 태그 시너지 도입 시)
├── 코어 축을 SkillTag 문자열 또는 [Flags] enum으로 코드화
├── 확장 축 중 필요한 것만 선택적으로 추가
├── 시너지 규칙: "같은 EffectRole 2개 이상이면 보너스" 등
└── ScalingShape 확장: AD/AP 비율 필드 추가 검토
```

### 코드화 시 규칙 (anti_patterns 정합)

태그·enum을 코드에 넣을 때는 [anti_patterns.md](anti_patterns.md)를 따른다.

- **Behavioral switch on type enums:** 분기가 커지면 Factory + Strategy로 옮긴다. 단순 커맨드 디스패치·값 매핑용 switch는 허용된다.
- **Silent fallback in exhaustive switch:** 새 enum 값 추가 시 `default`에서 조용히 fallback 하지 않고 `throw new ArgumentOutOfRangeException` 등으로 즉시 실패시킨다.
