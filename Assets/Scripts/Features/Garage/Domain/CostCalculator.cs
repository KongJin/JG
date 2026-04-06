using System;

namespace Features.Garage.Domain
{
    /// <summary>
    /// 스탯 기반 소환 비용 자동 계산.
    /// 가중치 합산 + 분산 페널티 적용.
    /// 순수 C# — Unity/Photon 의존성 없음.
    /// </summary>
    public static class CostCalculator
    {
        // 가중치 상수 (unit_module_design.md 공식)
        private const float WeightHp = 0.02f;
        private const float WeightAttackDamage = 0.5f;
        private const float WeightAttackSpeed = 3.0f;
        private const float WeightRange = 2.0f;
        private const float WeightMoveRange = 1.5f;

        // 분산 페널티 계수
        private const float DispersionPenaltyFactor = 0.3f;

        // 비용 범위
        private const int MinCost = 15;
        private const int MaxCost = 80;

        /// <summary>
        /// ComposedStats로부터 소환 비용 계산.
        /// </summary>
        public static int Calculate(UnitComposition.ComposedStats stats)
        {
            return Calculate(
                stats.Hp,
                stats.AttackDamage,
                stats.AttackSpeed,
                stats.Range,
                stats.MoveRange,
                stats.PassiveTraitCostBonus);
        }

        /// <summary>
        /// 개별 스탯 값으로부터 소환 비용 계산.
        /// </summary>
        public static int Calculate(
            float hp,
            float attackDamage,
            float attackSpeed,
            float range,
            float moveRange,
            int passiveTraitCost)
        {
            // 1. 가중치 적용된 값 계산
            float weightedHp = hp * WeightHp;
            float weightedDamage = attackDamage * WeightAttackDamage;
            float weightedSpeed = attackSpeed * WeightAttackSpeed;
            float weightedRange = range * WeightRange;
            float weightedMoveRange = moveRange * WeightMoveRange;

            // 2. 기본 비용
            float baseCost = weightedHp + weightedDamage + weightedSpeed + weightedRange + weightedMoveRange;

            // 3. 분산 페널티 (표준편차 기반)
            Span<float> values = stackalloc float[5];
            values[0] = weightedHp;
            values[1] = weightedDamage;
            values[2] = weightedSpeed;
            values[3] = weightedRange;
            values[4] = weightedMoveRange;
            float dispersionPenalty = CalculateStandardDeviation(values) * DispersionPenaltyFactor;

            // 4. 최종 비용
            int finalCost = (int)Math.Round(baseCost + dispersionPenalty + passiveTraitCost);

            // 5. 범위 제한
            if (finalCost < MinCost) return MinCost;
            if (finalCost > MaxCost) return MaxCost;
            return finalCost;
        }

        /// <summary>
        /// 배열의 표준편차 계산.
        /// </summary>
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
