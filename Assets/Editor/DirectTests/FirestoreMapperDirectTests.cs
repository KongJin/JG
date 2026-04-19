using Features.Account.Infrastructure;
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
    }
}
