using Features.Account.Infrastructure;
using Features.Player.Domain;
using NUnit.Framework;

namespace Tests.Editor
{
    public sealed class FirestoreMapperDirectTests
    {
        [Test]
        public void BuildRawJsonDocument_RoundTripsEscapedStringField()
        {
            const string rawJson = "{\"name\":\"Alpha\\nBeta\",\"quote\":\"\\\"test\\\"\"}";

            string documentJson = FirestoreFieldSerializer.BuildRawJsonDocument(rawJson);
            string restoredJson = FirestoreFieldReader.GetString(documentJson, "json");

            Assert.AreEqual(rawJson, restoredJson);
        }

        [Test]
        public void Reader_ParsesNumericFields_FromFirestoreDocument()
        {
            const string documentJson =
                "{\"fields\":{" +
                "\"count\":{\"integerValue\":3}," +
                "\"ratio\":{\"doubleValue\":1.5}," +
                "\"name\":{\"stringValue\":\"pilot\"}" +
                "}}";

            Assert.AreEqual(3, FirestoreFieldReader.GetInt(documentJson, "count"));
            Assert.AreEqual(1.5f, FirestoreFieldReader.GetFloat(documentJson, "ratio"));
            Assert.AreEqual("pilot", FirestoreFieldReader.GetString(documentJson, "name"));
        }

        [Test]
        public void OperationRecordMapper_RoundTripsRecentRecords()
        {
            var records = new RecentOperationRecords();
            records.AddOrReplace(new OperationRecord
            {
                operationId = "op-1",
                endedAtUnixMs = 100,
                result = OperationRecordResult.Held,
                reachedWave = 3,
                survivalSeconds = 125f,
                summonCount = 2,
                unitKillCount = 4,
                pressureSummaryKey = "pressure.cleared"
            });

            string documentJson = FirestoreFieldSerializer.BuildRawJsonDocument(
                FirestoreOperationRecordMapper.ToJson(records));
            var restored = FirestoreOperationRecordMapper.FromDocument(documentJson);

            Assert.AreEqual(1, restored.Count);
            Assert.AreEqual("op-1", restored.Records[0].operationId);
            Assert.AreEqual(3, restored.Records[0].reachedWave);
            Assert.AreEqual(OperationRecordResult.Held, restored.Records[0].result);
        }
    }
}
