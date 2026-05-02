using Features.Garage.Infrastructure;
using NUnit.Framework;
using UnityEditor;

namespace Tests.Editor
{
    public sealed class NovaAssemblyProfileCatalogDirectTests
    {
        private const string AlignmentCatalogPath = "Assets/Data/Garage/NovaGenerated/NovaPartAlignmentCatalog.asset";

        [TestCase("nova_frame_body26_kp")]
        [TestCase("nova_frame_body10_skdr")]
        [TestCase("nova_fire_arm15_hdkn")]
        [TestCase("nova_fire_arm29_sdbt")]
        [TestCase("nova_fire_arm32_sppoo")]
        [TestCase("nova_fire_arm39_hmsk")]
        public void AlignmentCatalog_ContainsOnlySupportedGeneratedAssemblyForms(string partId)
        {
            var catalog = LoadCatalog();

            Assert.IsNull(TryFindEntry(catalog, partId));
        }

        [Test]
        public void AlignmentCatalog_KeepsKnownLegProblemPartsInGxAuditReviewQueue()
        {
            var catalog = LoadCatalog();
            var opterix = FindEntry(catalog, "nova_mob_legs49_otrs");
            var delphinus = FindEntry(catalog, "nova_mob_legs34_dpns");

            Assert.That(opterix.AssemblyAnchorMode, Is.EqualTo("LegBodySocket"));
            Assert.That(delphinus.AssemblyAnchorMode, Is.EqualTo("LegBodySocket"));
            Assert.That(opterix.AssemblyConfidence, Is.EqualTo("derived"));
            Assert.That(delphinus.AssemblyConfidence, Is.EqualTo("derived"));
            Assert.That(opterix.AssemblyReviewResult, Is.EqualTo("pending"));
            Assert.That(delphinus.AssemblyReviewResult, Is.EqualTo("pending"));
        }

        private static NovaPartAlignmentCatalog LoadCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<NovaPartAlignmentCatalog>(AlignmentCatalogPath);
            Assert.NotNull(catalog, "Missing NovaPartAlignmentCatalog asset.");
            return catalog;
        }

        private static NovaPartAlignmentCatalog.Entry FindEntry(NovaPartAlignmentCatalog catalog, string partId)
        {
            var entry = TryFindEntry(catalog, partId);
            if (entry != null)
                return entry;

            Assert.Fail("Missing alignment entry: " + partId);
            return null;
        }

        private static NovaPartAlignmentCatalog.Entry TryFindEntry(NovaPartAlignmentCatalog catalog, string partId)
        {
            for (var i = 0; i < catalog.Entries.Count; i++)
            {
                var entry = catalog.Entries[i];
                if (entry != null && entry.PartId == partId)
                    return entry;
            }

            return null;
        }
    }
}
