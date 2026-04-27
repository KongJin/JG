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
            var energyPort = new FakeEnergyPort(currentEnergy: 10f);
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
            Assert.AreEqual(7f, energyPort.CurrentEnergy);
            Assert.AreEqual(0f, energyPort.RefundedEnergy);
        }

        [Test]
        public void Execute_PublishesFailedEventWithoutSpawning_WhenEnergyIsInsufficient()
        {
            var eventBus = new EventBus();
            var energyPort = new FakeEnergyPort(currentEnergy: 2f);
            var summonPort = new FakeSummonPort(new DomainEntityId("battle-unit-1"));
            var useCase = new SummonUnitUseCase(energyPort, summonPort, eventBus);
            var playerId = new DomainEntityId("player-1");
            var unitSpec = CreateUnit("unit-1", summonCost: 3);
            UnitSummonFailedEvent failed = default;
            var failedCount = 0;

            eventBus.Subscribe(this, new System.Action<UnitSummonFailedEvent>(e =>
            {
                failed = e;
                failedCount++;
            }));

            var result = useCase.Execute(playerId, unitSpec, new Float3(1f, 0f, 2f));

            Assert.IsFalse(result);
            Assert.AreEqual(1, failedCount);
            Assert.AreEqual(playerId, failed.PlayerId);
            StringAssert.Contains("Not enough energy", failed.Reason);
            Assert.AreEqual(0, summonPort.SpawnCount);
            Assert.AreEqual(2f, energyPort.CurrentEnergy);
            Assert.AreEqual(0f, energyPort.RefundedEnergy);
        }

        [Test]
        public void Execute_RefundsEnergyAndPublishesFailedEvent_WhenSpawnThrows()
        {
            var eventBus = new EventBus();
            var energyPort = new FakeEnergyPort(currentEnergy: 10f);
            var summonPort = new ThrowingSummonPort();
            var useCase = new SummonUnitUseCase(energyPort, summonPort, eventBus);
            var playerId = new DomainEntityId("player-1");
            var unitSpec = CreateUnit("unit-1", summonCost: 3);
            UnitSummonFailedEvent failed = default;
            UnitSummonCompletedEvent completed = default;
            var failedCount = 0;
            var completedCount = 0;

            eventBus.Subscribe(this, new System.Action<UnitSummonFailedEvent>(e =>
            {
                failed = e;
                failedCount++;
            }));
            eventBus.Subscribe(this, new System.Action<UnitSummonCompletedEvent>(e =>
            {
                completed = e;
                completedCount++;
            }));

            var result = useCase.Execute(playerId, unitSpec, new Float3(1f, 0f, 2f));

            Assert.IsFalse(result);
            Assert.AreEqual(1, failedCount);
            Assert.AreEqual(0, completedCount);
            Assert.AreEqual(default(UnitSummonCompletedEvent), completed);
            StringAssert.Contains("BattleEntity spawn failed", failed.Reason);
            Assert.AreEqual(10f, energyPort.CurrentEnergy);
            Assert.AreEqual(3f, energyPort.RefundedEnergy);
            Assert.AreEqual(1, summonPort.SpawnCount);
        }

        [Test]
        public void Execute_RefundsEnergyAndPublishesFailedEvent_WhenSpawnReturnsEmptyId()
        {
            var eventBus = new EventBus();
            var energyPort = new FakeEnergyPort(currentEnergy: 10f);
            var summonPort = new EmptyIdSummonPort();
            var useCase = new SummonUnitUseCase(energyPort, summonPort, eventBus);
            var playerId = new DomainEntityId("player-1");
            var unitSpec = CreateUnit("unit-1", summonCost: 3);
            UnitSummonFailedEvent failed = default;
            var failedCount = 0;
            var completedCount = 0;

            eventBus.Subscribe(this, new System.Action<UnitSummonFailedEvent>(e =>
            {
                failed = e;
                failedCount++;
            }));
            eventBus.Subscribe(this, new System.Action<UnitSummonCompletedEvent>(_ => completedCount++));

            var result = useCase.Execute(playerId, unitSpec, new Float3(1f, 0f, 2f));

            Assert.IsFalse(result);
            Assert.AreEqual(1, failedCount);
            Assert.AreEqual(0, completedCount);
            StringAssert.Contains("empty id", failed.Reason);
            Assert.AreEqual(10f, energyPort.CurrentEnergy);
            Assert.AreEqual(3f, energyPort.RefundedEnergy);
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
            public FakeEnergyPort(float currentEnergy)
            {
                CurrentEnergy = currentEnergy;
            }

            public float CurrentEnergy { get; private set; }
            public float RefundedEnergy { get; private set; }

            public bool TrySpendEnergy(DomainEntityId ownerId, float cost)
            {
                if (CurrentEnergy < cost)
                    return false;

                CurrentEnergy -= cost;
                return true;
            }

            public void RefundEnergy(DomainEntityId ownerId, float amount)
            {
                RefundedEnergy += amount;
                CurrentEnergy += amount;
            }

            public float GetCurrentEnergy(DomainEntityId ownerId)
            {
                return CurrentEnergy;
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

        private sealed class ThrowingSummonPort : ISummonExecutionPort
        {
            public int SpawnCount { get; private set; }

            public DomainEntityId SpawnBattleEntity(UnitSpec unitSpec, Float3 spawnPosition, DomainEntityId ownerId)
            {
                SpawnCount++;
                throw new System.InvalidOperationException("spawn unavailable");
            }
        }

        private sealed class EmptyIdSummonPort : ISummonExecutionPort
        {
            public int SpawnCount { get; private set; }

            public DomainEntityId SpawnBattleEntity(UnitSpec unitSpec, Float3 spawnPosition, DomainEntityId ownerId)
            {
                SpawnCount++;
                return default;
            }
        }
    }
}
