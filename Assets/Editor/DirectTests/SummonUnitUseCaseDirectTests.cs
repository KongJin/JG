using Features.Unit.Application;
using Features.Unit.Application.Events;
using Features.Unit.Application.Ports;
using NUnit.Framework;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Math;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Tests.Editor
{
    public sealed class SummonUnitUseCaseDirectTests
    {
        [Test]
        public void Execute_PublishesCompletedEventOnce_WhenEnergySpendSucceeds()
        {
            var eventBus = new EventBus();
            var energyPort = new FakeEnergyPort(canSpend: true, currentEnergy: 10f);
            var summonPort = new FakeSummonPort(new DomainEntityId("battle-unit-1"));
            var useCase = new SummonUnitUseCase(energyPort, summonPort, eventBus);
            var playerId = new DomainEntityId("player-1");
            var unitSpec = CreateUnit("unit-1", summonCost: 3);
            var completedCount = 0;
            UnitSummonCompletedEvent lastEvent = default;

            eventBus.Subscribe(this, new System.Action<UnitSummonCompletedEvent>(e =>
            {
                completedCount++;
                lastEvent = e;
            }));

            var result = useCase.Execute(playerId, unitSpec, new Float3(1f, 0f, 2f));

            Assert.IsTrue(result);
            Assert.AreEqual(1, completedCount);
            Assert.AreEqual(playerId, lastEvent.PlayerId);
            Assert.AreEqual(summonPort.SpawnedBattleEntityId, lastEvent.BattleEntityId);
            Assert.AreSame(unitSpec, lastEvent.UnitSpec);
            Assert.AreEqual(1, summonPort.SpawnCount);
        }

        private static UnitSpec CreateUnit(string id, int summonCost)
        {
            return new UnitSpec(
                new DomainEntityId(id),
                "frame",
                "Unit",
                "fire",
                "mobility",
                "",
                0,
                100f,
                10f,
                1f,
                5f,
                3f,
                3f,
                summonCost);
        }

        private sealed class FakeEnergyPort : IUnitEnergyPort
        {
            private readonly bool _canSpend;
            private readonly float _currentEnergy;

            public FakeEnergyPort(bool canSpend, float currentEnergy)
            {
                _canSpend = canSpend;
                _currentEnergy = currentEnergy;
            }

            public bool TrySpendEnergy(DomainEntityId ownerId, float cost)
            {
                return _canSpend;
            }

            public float GetCurrentEnergy(DomainEntityId ownerId)
            {
                return _currentEnergy;
            }
        }

        private sealed class FakeSummonPort : ISummonExecutionPort
        {
            public FakeSummonPort(DomainEntityId spawnedBattleEntityId)
            {
                SpawnedBattleEntityId = spawnedBattleEntityId;
            }

            public DomainEntityId SpawnedBattleEntityId { get; }
            public int SpawnCount { get; private set; }

            public DomainEntityId SpawnBattleEntity(UnitSpec unitSpec, Float3 spawnPosition, DomainEntityId ownerId)
            {
                SpawnCount++;
                return SpawnedBattleEntityId;
            }
        }
    }
}
