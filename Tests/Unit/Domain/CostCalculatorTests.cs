using NUnit.Framework;
using Features.Unit.Domain;

namespace Tests.Unit.Domain
{
    /// <summary>
    /// CostCalculator 테스트.
    /// 가중치 + 분산 페널티 공식 검증.
    /// </summary>
    public class CostCalculatorTests
    {
        [Test]
        public void Calculate_극한근접딜러_기댓값()
        {
            // unit_module_design.md 예시: 유닛 A
            // HP: 200, 공격력: 80, 공격속도: 1.5, 사거리: 2.5, 이동범위: 6.0
            int cost = CostCalculator.Calculate(
                hp: 200f,
                attackDamage: 80f,
                attackSpeed: 1.5f,
                range: 2.5f,
                moveRange: 6.0f,
                passiveTraitCost: 0
            );

            // 기본 비용: 4 + 40 + 4.5 + 5 + 9 = 62.5
            // 분산 페널티 ≈ 4.35
            // 최종 ≈ 67
            Assert.AreEqual(67, cost);
        }

        [Test]
        public void Calculate_균형형_기댓값()
        {
            // unit_module_design.md 예시: 유닛 B
            // HP: 500, 공격력: 40, 공격속도: 1.0, 사거리: 5.0, 이동범위: 5.0
            int cost = CostCalculator.Calculate(
                hp: 500f,
                attackDamage: 40f,
                attackSpeed: 1.0f,
                range: 5.0f,
                moveRange: 5.0f,
                passiveTraitCost: 0
            );

            // 기본 비용: 10 + 20 + 3 + 10 + 7.5 = 50.5
            // 분산 페널티 ≈ 1.68
            // 최종 ≈ 52
            Assert.AreEqual(52, cost);
        }

        [Test]
        public void Calculate_최소비용_하한선()
        {
            int cost = CostCalculator.Calculate(
                hp: 10f,
                attackDamage: 1f,
                attackSpeed: 0.1f,
                range: 0.5f,
                moveRange: 0.5f,
                passiveTraitCost: 0
            );

            Assert.AreEqual(15, cost); // MinCost = 15
        }

        [Test]
        public void Calculate_최대비용_상한선()
        {
            int cost = CostCalculator.Calculate(
                hp: 2000f,
                attackDamage: 100f,
                attackSpeed: 2.0f,
                range: 12f,
                moveRange: 10f,
                passiveTraitCost: 10
            );

            Assert.AreEqual(80, cost); // MaxCost = 80
        }

        [Test]
        public void Calculate_고유특성보정_반영됨()
        {
            int costWithout = CostCalculator.Calculate(
                hp: 500f, attackDamage: 40f, attackSpeed: 1.0f,
                range: 5.0f, moveRange: 5.0f, passiveTraitCost: 0
            );

            int costWith = CostCalculator.Calculate(
                hp: 500f, attackDamage: 40f, attackSpeed: 1.0f,
                range: 5.0f, moveRange: 5.0f, passiveTraitCost: 10
            );

            Assert.AreEqual(10, costWith - costWithout);
        }

        [Test]
        public void Calculate_극한빌드가_균형형보다_비쌈()
        {
            int extremeCost = CostCalculator.Calculate(
                hp: 200f, attackDamage: 80f, attackSpeed: 1.5f,
                range: 2.5f, moveRange: 6.0f, passiveTraitCost: 0
            );

            int balancedCost = CostCalculator.Calculate(
                hp: 500f, attackDamage: 40f, attackSpeed: 1.0f,
                range: 5.0f, moveRange: 5.0f, passiveTraitCost: 0
            );

            Assert.Greater(extremeCost, balancedCost, "극한빌드가 균형형보다 비싸야 함 (분산 페널티)");
        }
    }
}
