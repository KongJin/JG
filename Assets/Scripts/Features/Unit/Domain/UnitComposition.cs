using Shared.Kernel;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Features.Unit.Domain
{
    /// <summary>
    /// 프레임 + 모듈 조합의 유효성 검증 + 결과 계산.
    /// 순수 C# — Unity/Photon 의존성 없음.
    /// 실제 데이터(ScriptableObject)는 Infrastructure 레이어에서 공급.
    /// </summary>
    public static class UnitComposition
    {
        /// <summary>
        /// 프레임 + 모듈 조합에서 최종 스탯을 계산하기 위한 입력 데이터.
        /// ScriptableObject에서 변환한 순수 값을 전달받는다.
        /// </summary>
        public readonly struct CompositionInput
        {
            public float BaseHp { get; }
            public float BaseDefense { get; }
            public float FirepowerDamage { get; }
            public float FirepowerAttackSpeed { get; }
            public float FirepowerRange { get; }
            public float MobilityMoveSpeed { get; }
            public float MobilityMoveRange { get; }
            public int PassiveTraitCostBonus { get; }
            public CostCalculator.StatCostTuning CostTuning { get; }

            public CompositionInput(
                float baseHp,
                float baseDefense,
                float firepowerDamage,
                float firepowerAttackSpeed,
                float firepowerRange,
                float mobilityMoveSpeed,
                float mobilityMoveRange,
                int passiveTraitCostBonus,
                CostCalculator.StatCostTuning costTuning = default)
            {
                BaseHp = baseHp;
                BaseDefense = baseDefense;
                FirepowerDamage = firepowerDamage;
                FirepowerAttackSpeed = firepowerAttackSpeed;
                FirepowerRange = firepowerRange;
                MobilityMoveSpeed = mobilityMoveSpeed;
                MobilityMoveRange = mobilityMoveRange;
                PassiveTraitCostBonus = passiveTraitCostBonus;
                CostTuning = costTuning;
            }
        }

        /// <summary>
        /// 조합 결과 스탯.
        /// </summary>
        public readonly struct ComposedStats
        {
            public string FrameId { get; }
            public string FirepowerModuleId { get; }
            public string MobilityModuleId { get; }
            public float Hp { get; }
            public float Defense { get; }
            public float AttackDamage { get; }
            public float AttackSpeed { get; }
            public float Range { get; }
            public float MoveSpeed { get; }
            public float MoveRange { get; }
            public float AnchorRange => MoveRange;
            public int PassiveTraitCostBonus { get; }
            public int FrameEnergyCost { get; }
            public int FirepowerEnergyCost { get; }
            public int MobilityEnergyCost { get; }
            public int SummonCost => FrameEnergyCost + FirepowerEnergyCost + MobilityEnergyCost;
            public CostCalculator.StatCostTuning CostTuning { get; }
            public UnitRole Role { get; }

            public ComposedStats(
                string frameId,
                string firepowerModuleId,
                string mobilityModuleId,
                float hp,
                float defense,
                float attackDamage,
                float attackSpeed,
                float range,
                float moveSpeed,
                float moveRange,
                int passiveTraitCostBonus,
                int frameEnergyCost,
                int firepowerEnergyCost,
                int mobilityEnergyCost,
                CostCalculator.StatCostTuning costTuning,
                UnitRole role)
            {
                FrameId = frameId;
                FirepowerModuleId = firepowerModuleId;
                MobilityModuleId = mobilityModuleId;
                Hp = hp;
                Defense = defense;
                AttackDamage = attackDamage;
                AttackSpeed = attackSpeed;
                Range = range;
                MoveSpeed = moveSpeed;
                MoveRange = moveRange;
                PassiveTraitCostBonus = passiveTraitCostBonus;
                FrameEnergyCost = frameEnergyCost;
                FirepowerEnergyCost = firepowerEnergyCost;
                MobilityEnergyCost = mobilityEnergyCost;
                CostTuning = costTuning;
                Role = role;
            }

            /// <summary>
            /// 조합 결과를 Unit 엔티티로 변환.
            /// </summary>
            public UnitSpec ToUnit(DomainEntityId id) => new(
                id,
                FrameId,
                FrameId,  // displayName — FrameId를 기본값으로 사용 (추후 매핑 테이블로 교체)
                FirepowerModuleId,
                MobilityModuleId,
                string.Empty,  // passiveTraitId - 프레임에서 조회
                PassiveTraitCostBonus,
                Hp,
                Defense,
                AttackDamage,
                AttackSpeed,
                Range,
                MoveSpeed,
                MoveRange,
                AnchorRange,
                FrameEnergyCost,
                FirepowerEnergyCost,
                MobilityEnergyCost,
                SummonCost);
        }

        /// <summary>
        /// 조합이 유효한지 검사.
        /// </summary>
        /// <param name="moveRange">하단 모듈의 이동범위</param>
        /// <param name="range">상단 모듈의 사거리</param>
        /// <param name="errorMessage">실패 시 오류 메시지</param>
        public static bool Validate(float moveRange, float range, out string errorMessage)
        {
            errorMessage = null;

            // 스탯 간 관계 규칙(unit_module_design.md):
            // 1. 이동범위 ≥ 4m (근접형)
            // 2. 사거리 ≥ 6m (원거리형)
            // 3. 이동범위 ≥ 3m AND 사거리 ≥ 4m (하이브리드형)
            bool isMelee = moveRange >= 4f;
            bool isRanged = range >= 6f;
            bool isHybrid = moveRange >= 3f && range >= 4f;

            if (!isMelee && !isRanged && !isHybrid)
            {
                errorMessage = $"이동범위({moveRange}m)와 사거리({range}m)이 모두 좁습니다. " +
                    $"근접 유닛은 이동범위 ≥ 4m 또는 사거리 ≥ 6m이 필요하거나, " +
                    $"이동범위 ≥ 3m AND 사거리 ≥ 4m(하이브리드)를 만족해야 합니다.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 조합 결과 스탯 계산.
        /// </summary>
        public static ComposedStats Compose(
            string frameId,
            string firepowerModuleId,
            string mobilityModuleId,
            CompositionInput input)
        {
            float hp = input.BaseHp;
            float defense = input.BaseDefense;
            float attackDamage = input.FirepowerDamage;
            float attackSpeed = input.FirepowerAttackSpeed;
            float range = input.FirepowerRange;
            float moveSpeed = input.MobilityMoveSpeed;
            float moveRange = input.MobilityMoveRange;

            var costs = CostCalculator.CalculateParts(
                hp,
                defense,
                input.PassiveTraitCostBonus,
                attackDamage,
                attackSpeed,
                range,
                moveSpeed,
                moveRange,
                input.CostTuning);
            UnitRole role = ClassifyRole(hp, defense, attackDamage, attackSpeed, range, moveRange);

            return new ComposedStats(
                frameId,
                firepowerModuleId,
                mobilityModuleId,
                hp,
                defense,
                attackDamage,
                attackSpeed,
                range,
                moveSpeed,
                moveRange,
                input.PassiveTraitCostBonus,
                costs.Frame,
                costs.Firepower,
                costs.Mobility,
                input.CostTuning,
                role);
        }

        /// <summary>
        /// 스탯 프로필을 기반으로 유닛 역할을 분류.
        /// </summary>
        private static UnitRole ClassifyRole(
            float hp,
            float defense,
            float attackDamage,
            float attackSpeed,
            float range,
            float moveRange)
        {
            float dps = attackDamage * attackSpeed;

            // 탱커: 좁은 이동범위, 짧은 사거리, 높은 HP
            if (moveRange <= 4f && range <= 3f && (hp >= 800f || defense >= 6f))
                return UnitRole.Tanker;

            // 근접딜러: 넓은 이동범위, 짧은 사거리, 높은 DPS
            if (moveRange >= 5f && range <= 3f && dps >= 25f)
                return UnitRole.MeleeDps;

            // 원거리: 좁은 이동범위, 긴 사거리
            if (moveRange <= 4f && range >= 8f)
                return UnitRole.Ranged;

            // 지원: 중간 범위 스탯
            return UnitRole.Support;
        }
    }

    /// <summary>
    /// 유닛 역할 분류.
    /// </summary>
    public enum UnitRole
    {
        Tanker = 0,
        MeleeDps = 1,
        Ranged = 2,
        Support = 3
    }
}
