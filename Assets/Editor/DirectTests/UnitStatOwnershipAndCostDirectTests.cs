using Features.Combat.Domain;
using Features.Garage.Application.Ports;
using Features.Unit.Application;
using Features.Unit.Domain;
using Features.Unit.Infrastructure;
using NUnit.Framework;
using Shared.Kernel;
using Shared.Math;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Tests.Editor
{
    public sealed class UnitStatOwnershipAndCostDirectTests
    {
        [Test]
        public void CalculateParts_UsesOwnedStats_AndTotalIsPartSum()
        {
            var tuning = new CostCalculator.StatCostTuning(
                hpWeight: 0.1f,
                defenseWeight: 1f,
                attackDamageWeight: 1f,
                attackSpeedWeight: 1f,
                rangeWeight: 1f,
                moveSpeedWeight: 1f,
                moveRangeWeight: 1f,
                dispersionPenaltyFactor: 0f);

            var costs = CostCalculator.CalculateParts(
                hp: 100f,
                defense: 2f,
                passiveTraitCost: 7,
                attackDamage: 10f,
                attackSpeed: 1f,
                range: 5f,
                moveSpeed: 4f,
                moveRange: 3f,
                tuning);

            Assert.AreEqual(19, costs.Frame);
            Assert.AreEqual(16, costs.Firepower);
            Assert.AreEqual(7, costs.Mobility);
            Assert.AreEqual(costs.Frame + costs.Firepower + costs.Mobility, costs.Total);
        }

        [Test]
        public void CalculatePart_AddsHigherPenaltyForImbalancedStats()
        {
            var balanced = CostCalculator.CalculatePart(1f, fixedBonus: 0, 10f, 10f, 10f);
            var imbalanced = CostCalculator.CalculatePart(1f, fixedBonus: 0, 30f, 0f, 0f);

            Assert.Greater(imbalanced, balanced);
        }

        [Test]
        public void ComposeUnit_MapsStatsToNewOwners()
        {
            var useCase = new ComposeUnitUseCase(new FakeCompositionPort());

            var result = useCase.Execute(
                new DomainEntityId("unit-1"),
                "frame",
                "fire",
                "mobility");

            Assert.IsTrue(result.IsSuccess, result.Error);
            var unit = result.Value;
            Assert.AreEqual(240f, unit.FinalHp);
            Assert.AreEqual(5f, unit.FinalDefense);
            Assert.AreEqual(1.25f, unit.FinalAttackSpeed);
            Assert.AreEqual(4.2f, unit.FinalMoveSpeed);
            Assert.AreEqual(5.5f, unit.FinalMoveRange);
            Assert.AreEqual(unit.FinalMoveRange, unit.FinalAnchorRange);
            Assert.AreEqual(
                unit.FrameEnergyCost + unit.FirepowerEnergyCost + unit.MobilityEnergyCost,
                unit.SummonCost);
        }

        [Test]
        public void BattleEntityDefenseFeedsFixedDamageReduction()
        {
            var unit = new UnitSpec(
                new DomainEntityId("unit-1"),
                "frame",
                "Unit",
                "fire",
                "mobility",
                string.Empty,
                0,
                100f,
                7f,
                10f,
                1f,
                5f,
                4f,
                3f,
                3f,
                1,
                1,
                1,
                3);
            var entity = new BattleEntity(
                new DomainEntityId("battle-1"),
                unit,
                new DomainEntityId("player-1"),
                Float3.Zero);
            var provider = new BattleEntityCombatTargetProvider(entity);

            Assert.AreEqual(7f, provider.GetDefense());
            Assert.AreEqual(5f, DamageRule.Calculate(12f, provider.GetDefense(), DamageType.Physical));
        }

        private sealed class FakeCompositionPort : IUnitCompositionPort
        {
            public ModuleStats GetFrameBaseStats(string frameId) => new(
                hpBonus: 240f,
                defense: 5f);

            public ModuleStats GetFirepowerStats(string moduleId) => new(
                attackDamage: 18f,
                attackSpeed: 1.25f,
                range: 6f);

            public ModuleStats GetMobilityStats(string moduleId) => new(
                moveSpeed: 4.2f,
                moveRange: 5.5f);

            public CostCalculator.StatCostTuning GetCostTuning() => new(
                hpWeight: 0.1f,
                defenseWeight: 1f,
                attackDamageWeight: 1f,
                attackSpeedWeight: 1f,
                rangeWeight: 1f,
                moveSpeedWeight: 1f,
                moveRangeWeight: 1f,
                dispersionPenaltyFactor: 0f);

            public string GetPassiveTraitId(string frameId) => string.Empty;

            public int GetPassiveTraitCostBonus(string frameId) => 3;
        }
    }
}
