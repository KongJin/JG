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
        public void AlignmentCatalog_KeepsHumanoidDirectionOnlyWeaponsDisabledUntilOriginalProfileExists()
        {
            var catalog = LoadCatalog();
            var handcannon = FindEntry(catalog, "nova_fire_arm15_hdkn");
            var spitfire = FindEntry(catalog, "nova_fire_arm32_sppoo");
            var hammerShock = FindEntry(catalog, "nova_fire_arm39_hmsk");

            AssertDisabledHumanoidProfile(handcannon, "pending");
            AssertDisabledHumanoidProfile(spitfire, "pending");
            AssertDisabledHumanoidProfile(hammerShock, "pending");
        }

        [TestCase("nova_fire_arm29_sdbt", "mismatch")]
        public void AlignmentCatalog_KeepsReviewedHumanoidFrameWeaponMobilityEvidenceDisabled(
            string firepowerId,
            string reviewResult)
        {
            var catalog = LoadCatalog();
            var firepower = FindEntry(catalog, firepowerId);

            AssertDisabledHumanoidProfile(firepower, reviewResult);
            Assert.That(firepower.AssemblyLocalOffset.sqrMagnitude, Is.EqualTo(0f).Within(0.000001f));
            StringAssert.Contains("original-oracle/oracle-lab-assembled-current-20260502.png", firepower.AssemblyEvidencePath);
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

        private static void AssertDisabledHumanoidProfile(NovaPartAlignmentCatalog.Entry entry, string reviewResult)
        {
            Assert.That(entry.AssemblyAnchorMode, Is.EqualTo("Disabled"));
            Assert.That(entry.AssemblySlotMode, Is.EqualTo("disabled"));
            Assert.That(entry.AssemblyConfidence, Is.EqualTo("blocked"));
            Assert.That(entry.AssemblyReviewResult, Is.EqualTo(reviewResult));
            Assert.That(entry.AssemblyLocalOffset.sqrMagnitude, Is.EqualTo(0f).Within(0.000001f));
            StringAssert.Contains("nova_part_catalog.csv", entry.AssemblyEvidencePath);
        }
    }
}
