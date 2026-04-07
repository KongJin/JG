using Shared.Kernel;

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
            public float BaseAttackSpeed { get; }
            public float FirepowerDamage { get; }
            public float FirepowerAttackSpeed { get; }
            public float FirepowerRange { get; }
            public float MobilityHpBonus { get; }
            public float MobilityMoveRange { get; }
            public float MobilityAnchorRange { get; }  // 앵커 반경 추가
            public int PassiveTraitCostBonus { get; }

            public CompositionInput(
                float baseHp,
                float baseAttackSpeed,
                float firepowerDamage,
                float firepowerAttackSpeed,
                float firepowerRange,
                float mobilityHpBonus,
                float mobilityMoveRange,
                float mobilityAnchorRange,
                int passiveTraitCostBonus)
            {
                BaseHp = baseHp;
                BaseAttackSpeed = baseAttackSpeed;
                FirepowerDamage = firepowerDamage;
                FirepowerAttackSpeed = firepowerAttackSpeed;
                FirepowerRange = firepowerRange;
                MobilityHpBonus = mobilityHpBonus;
                MobilityMoveRange = mobilityMoveRange;
                MobilityAnchorRange = mobilityAnchorRange;
                PassiveTraitCostBonus = passiveTraitCostBonus;
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
            public float AttackDamage { get; }
            public float AttackSpeed { get; }
            public float Range { get; }
            public float MoveRange { get; }
            public float AnchorRange { get; }  // 앵커 반경 추가
            public int PassiveTraitCostBonus { get; }
            public UnitRole Role { get; }

            public ComposedStats(
                string frameId,
                string firepowerModuleId,
                string mobilityModuleId,
                float hp,
                float attackDamage,
                float attackSpeed,
                float range,
                float moveRange,
                float anchorRange,
                int passiveTraitCostBonus,
                UnitRole role)
            {
                FrameId = frameId;
                FirepowerModuleId = firepowerModuleId;
                MobilityModuleId = mobilityModuleId;
                Hp = hp;
                AttackDamage = attackDamage;
                AttackSpeed = attackSpeed;
                Range = range;
                MoveRange = moveRange;
                AnchorRange = anchorRange;
                PassiveTraitCostBonus = passiveTraitCostBonus;
                Role = role;
            }

            /// <summary>
            /// 조합 결과를 Unit 엔티티로 변환.
            /// </summary>
            public Unit ToUnit(DomainEntityId id) => new(
                id,
                FrameId,
                FirepowerModuleId,
                MobilityModuleId,
                string.Empty,  // passiveTraitId - 프레임에서 조회
                PassiveTraitCostBonus,
                Hp,
                AttackDamage,
                AttackSpeed,
                Range,
                MoveRange,
                AnchorRange,
                CostCalculator.Calculate(Hp, AttackDamage, AttackSpeed, Range, MoveRange, PassiveTraitCostBonus));
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
            float hp = input.BaseHp + input.MobilityHpBonus;
            float attackDamage = input.FirepowerDamage;
            // 공격속도: 모듈 배율 × 프레임 기본값
            float attackSpeed = input.FirepowerAttackSpeed * input.BaseAttackSpeed;
            float range = input.FirepowerRange;
            float moveRange = input.MobilityMoveRange;
            float anchorRange = input.MobilityAnchorRange;

            UnitRole role = ClassifyRole(hp, attackDamage, attackSpeed, range, moveRange);

            return new ComposedStats(
                frameId,
                firepowerModuleId,
                mobilityModuleId,
                hp,
                attackDamage,
                attackSpeed,
                range,
                moveRange,
                anchorRange,
                input.PassiveTraitCostBonus,
                role);
        }

        /// <summary>
        /// 스탯 프로필을 기반으로 유닛 역할을 분류.
        /// </summary>
        private static UnitRole ClassifyRole(
            float hp,
            float attackDamage,
            float attackSpeed,
            float range,
            float moveRange)
        {
            float dps = attackDamage * attackSpeed;

            // 탱커: 좁은 이동범위, 짧은 사거리, 높은 HP
            if (moveRange <= 4f && range <= 3f && hp >= 800f)
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
