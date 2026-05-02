using Features.Garage.Infrastructure;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class NovaAssemblyProfileCatalogDirectTests
    {
        private const string AlignmentCatalogPath = "Assets/Data/Garage/NovaGenerated/NovaPartAlignmentCatalog.asset";

        [Test]
        public void AlignmentCatalog_PromotesHumanoidDirectionOnlyWeaponsAsReviewShellProfiles()
        {
            var catalog = LoadCatalog();
            var spitfire = FindEntry(catalog, "nova_fire_arm32_sppoo");
            var hammerShock = FindEntry(catalog, "nova_fire_arm39_hmsk");

            AssertHumanoidShellReview(spitfire);
            AssertHumanoidShellReview(hammerShock);
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
            for (var i = 0; i < catalog.Entries.Count; i++)
            {
                var entry = catalog.Entries[i];
                if (entry != null && entry.PartId == partId)
                    return entry;
            }

            Assert.Fail("Missing alignment entry: " + partId);
            return null;
        }

        private static void AssertHumanoidShellReview(NovaPartAlignmentCatalog.Entry entry)
        {
            Assert.That(entry.AssemblyAnchorMode, Is.EqualTo("HumanoidShellBoundsCenter"));
            Assert.That(entry.AssemblySlotMode, Is.EqualTo("shell"));
            Assert.That(entry.AssemblyConfidence, Is.EqualTo("review"));
            Assert.That(entry.AssemblyReviewResult, Is.EqualTo("pending"));
            Assert.That(entry.AssemblyLocalOffset.sqrMagnitude, Is.EqualTo(0f).Within(0.000001f));
            StringAssert.Contains("nova_part_catalog.csv", entry.AssemblyEvidencePath);
        }
    }
}
