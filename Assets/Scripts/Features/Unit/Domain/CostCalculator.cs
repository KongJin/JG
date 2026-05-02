using System;

namespace Features.Unit.Domain
{
    /// <summary>
    /// 스탯 기반 소환 비용 자동 계산.
    /// 가중치 합산 + 분산 페널티 적용.
    /// 순수 C# — Unity/Photon 의존성 없음.
    /// </summary>
    public static class CostCalculator
    {
        public readonly struct StatCostTuning
        {
            public StatCostTuning(
                float hpWeight,
                float defenseWeight,
                float attackDamageWeight,
                float attackSpeedWeight,
                float rangeWeight,
                float moveSpeedWeight,
                float moveRangeWeight,
                float dispersionPenaltyFactor)
            {
                HpWeight = hpWeight;
                DefenseWeight = defenseWeight;
                AttackDamageWeight = attackDamageWeight;
                AttackSpeedWeight = attackSpeedWeight;
                RangeWeight = rangeWeight;
                MoveSpeedWeight = moveSpeedWeight;
                MoveRangeWeight = moveRangeWeight;
                DispersionPenaltyFactor = dispersionPenaltyFactor;
            }

            public float HpWeight { get; }
            public float DefenseWeight { get; }
            public float AttackDamageWeight { get; }
            public float AttackSpeedWeight { get; }
            public float RangeWeight { get; }
            public float MoveSpeedWeight { get; }
            public float MoveRangeWeight { get; }
            public float DispersionPenaltyFactor { get; }

            public bool HasAnyWeight =>
                HpWeight != 0f ||
                DefenseWeight != 0f ||
                AttackDamageWeight != 0f ||
                AttackSpeedWeight != 0f ||
                RangeWeight != 0f ||
                MoveSpeedWeight != 0f ||
                MoveRangeWeight != 0f;

            public static StatCostTuning Default => new(
                hpWeight: 0.02f,
                defenseWeight: 2.0f,
                attackDamageWeight: 0.5f,
                attackSpeedWeight: 3.0f,
                rangeWeight: 2.0f,
                moveSpeedWeight: 3.0f,
                moveRangeWeight: 1.5f,
                dispersionPenaltyFactor: 0.3f);
        }

        public readonly struct PartEnergyCosts
        {
            public PartEnergyCosts(int frame, int firepower, int mobility)
            {
                Frame = frame;
                Firepower = firepower;
                Mobility = mobility;
            }

            public int Frame { get; }
            public int Firepower { get; }
            public int Mobility { get; }
            public int Total => Frame + Firepower + Mobility;
        }

        /// <summary>
        /// ComposedStats로부터 소환 비용 계산.
        /// </summary>
        public static int Calculate(UnitComposition.ComposedStats stats)
        {
            return CalculateParts(
                stats.Hp,
                stats.Defense,
                stats.PassiveTraitCostBonus,
                stats.AttackDamage,
                stats.AttackSpeed,
                stats.Range,
                stats.MoveSpeed,
                stats.MoveRange,
                stats.CostTuning).Total;
        }

        /// <summary>
        /// 프레임, 화력, 기동 부품별 에너지 비용을 계산한다.
        /// </summary>
        public static PartEnergyCosts CalculateParts(
            float hp,
            float defense,
            int passiveTraitCost,
            float attackDamage,
            float attackSpeed,
            float range,
            float moveSpeed,
            float moveRange,
            StatCostTuning tuning)
        {
            tuning = ResolveTuning(tuning);

            int frameCost = CalculatePart(
                tuning.DispersionPenaltyFactor,
                passiveTraitCost,
                hp * tuning.HpWeight,
                defense * tuning.DefenseWeight);

            int firepowerCost = CalculatePart(
                tuning.DispersionPenaltyFactor,
                fixedBonus: 0,
                attackDamage * tuning.AttackDamageWeight,
                attackSpeed * tuning.AttackSpeedWeight,
                range * tuning.RangeWeight);

            int mobilityCost = CalculatePart(
                tuning.DispersionPenaltyFactor,
                fixedBonus: 0,
                moveSpeed * tuning.MoveSpeedWeight,
                moveRange * tuning.MoveRangeWeight);

            return new PartEnergyCosts(frameCost, firepowerCost, mobilityCost);
        }

        public static int CalculatePart(float dispersionPenaltyFactor, int fixedBonus, params float[] weightedValues)
        {
            float baseCost = 0f;
            for (int i = 0; i < weightedValues.Length; i++)
                baseCost += weightedValues[i];

            float dispersionPenalty = CalculateStandardDeviation(weightedValues) * dispersionPenaltyFactor;
            return (int)Math.Round(baseCost + dispersionPenalty + fixedBonus);
        }

        /// <summary>
        /// 배열의 표준편차 계산.
        /// </summary>
        private static StatCostTuning ResolveTuning(StatCostTuning tuning)
        {
            return tuning.HasAnyWeight ? tuning : StatCostTuning.Default;
        }

        private static float CalculateStandardDeviation(ReadOnlySpan<float> values)
        {
            if (values.Length == 0) return 0f;

            // 평균
            float mean = 0f;
            for (int i = 0; i < values.Length; i++)
            {
                mean += values[i];
            }
            mean /= values.Length;

            // 분산
            float variance = 0f;
            for (int i = 0; i < values.Length; i++)
            {
                float diff = values[i] - mean;
                variance += diff * diff;
            }
            variance /= values.Length;

            // 표준편차
            return (float)Math.Sqrt(variance);
        }
    }
}
