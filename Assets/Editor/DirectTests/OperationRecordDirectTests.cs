using System.IO;
using Features.Player.Application;
using Features.Player.Application.Events;
using Features.Player.Application.Ports;
using Features.Player.Domain;
using Features.Player.Infrastructure;
using NUnit.Framework;
using Shared.Kernel;

namespace Editor.DirectTests
{
    public sealed class OperationRecordDirectTests
    {
        [Test]
        public void RecentOperationRecords_TrimsToLatestFive()
        {
            var records = new RecentOperationRecords();

            for (var i = 0; i < 7; i++)
                records.AddOrReplace(CreateRecord($"op-{i}", endedAtUnixMs: i));

            Assert.AreEqual(RecentOperationRecords.MaxRecords, records.Count);
            Assert.AreEqual("op-6", records.Records[0].operationId);
            Assert.AreEqual("op-2", records.Records[4].operationId);
        }

        [Test]
        public void RecentOperationRecords_ReplacesDuplicateOperationId()
        {
            var records = new RecentOperationRecords();

            records.AddOrReplace(CreateRecord("same-op", endedAtUnixMs: 100, reachedWave: 1));
            records.AddOrReplace(CreateRecord("same-op", endedAtUnixMs: 200, reachedWave: 3));

            Assert.AreEqual(1, records.Count);
            Assert.AreEqual(3, records.Records[0].reachedWave);
            Assert.AreEqual(200, records.Records[0].endedAtUnixMs);
        }

        [Test]
        public void OperationRecordFactory_BuildsNormalizedRecordFromGameEndReport()
        {
            var factory = new OperationRecordFactory();
            var report = new GameEndReportRequestedEvent(
                isVictory: true,
                reachedWave: 4,
                playTimeSeconds: 123.4f,
                summonCount: 5,
                unitKillCount: 2,
                contributionCards: new[]
                {
                    new ResultContributionCard(
                        ResultContributionKind.ClearPressure,
                        "압박 정리",
                        "침공 기체 2기를 정리했습니다.",
                        2f,
                        unitId: new DomainEntityId("battle-unit-1"),
                        loadoutKey: "frame|fire|mobility")
                },
                coreRemainingHealth: 80f,
                coreMaxHealth: 100f);

            var record = factory.Build(report, 123456789L);

            Assert.AreEqual("operation-123456789", record.operationId);
            Assert.AreEqual(OperationRecordResult.Held, record.result);
            Assert.AreEqual(4, record.reachedWave);
            Assert.AreEqual(123.4f, record.survivalSeconds);
            Assert.IsTrue(record.hasCoreHealthPercent);
            Assert.AreEqual(0.8f, record.coreHealthPercent);
            Assert.AreEqual("pressure.cleared", record.pressureSummaryKey);
            Assert.AreEqual("frame|fire|mobility", record.primaryRosterUnits[0]);
        }

        [Test]
        public void OperationRecordJsonStore_RoundTripsRecords()
        {
            var path = Path.Combine(Path.GetTempPath(), $"jg-operation-records-{System.Guid.NewGuid():N}.json");
            try
            {
                var store = new OperationRecordJsonStore(path);
                var records = new RecentOperationRecords();
                records.AddOrReplace(CreateRecord("op-1", endedAtUnixMs: 10, reachedWave: 2));

                var saveResult = store.Save(records);
                var loaded = store.Load();

                Assert.IsTrue(saveResult.IsSuccess, saveResult.Error);
                Assert.AreEqual(1, loaded.Count);
                Assert.AreEqual("op-1", loaded.Records[0].operationId);
                Assert.AreEqual(2, loaded.Records[0].reachedWave);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Test]
        public void OperationRecordGameEndHandler_SavesOnlyOncePerSession()
        {
            var eventBus = new Shared.EventBus.EventBus();
            var store = new InMemoryOperationRecordStore();
            _ = new OperationRecordGameEndHandler(
                eventBus,
                new SaveOperationRecordUseCase(store),
                getUnixTimeMs: () => 1000L);

            var report = new GameEndReportRequestedEvent(
                isVictory: false,
                reachedWave: 1,
                playTimeSeconds: 10f,
                summonCount: 1,
                unitKillCount: 0);

            eventBus.Publish(report);
            eventBus.Publish(report);

            Assert.AreEqual(1, store.SaveCount);
            Assert.AreEqual(1, store.Snapshot.Count);
        }

        private static OperationRecord CreateRecord(
            string id,
            long endedAtUnixMs,
            int reachedWave = 0)
        {
            var record = new OperationRecord
            {
                operationId = id,
                endedAtUnixMs = endedAtUnixMs,
                result = OperationRecordResult.Held,
                survivalSeconds = endedAtUnixMs,
                reachedWave = reachedWave,
                summonCount = 1,
                unitKillCount = 0
            };
            record.Normalize();
            return record;
        }

        private sealed class InMemoryOperationRecordStore : IOperationRecordStore
        {
            public int SaveCount { get; private set; }
            public RecentOperationRecords Snapshot { get; private set; } = new();

            public RecentOperationRecords Load()
            {
                return Snapshot.Clone();
            }

            public Result Save(RecentOperationRecords records)
            {
                SaveCount++;
                Snapshot = records.Clone();
                return Result.Success();
            }
        }
    }
}
