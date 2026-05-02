using Features.Garage.Presentation;
using NUnit.Framework;

namespace Tests.Editor
{
    public sealed class GarageUnitIdentityFormatterDirectTests
    {
        [Test]
        public void BuildCallsign_IsDeterministicBySlot()
        {
            Assert.AreEqual("A-01", GarageUnitIdentityFormatter.BuildCallsign(0));
            Assert.AreEqual("A-06", GarageUnitIdentityFormatter.BuildCallsign(5));
            Assert.AreEqual("A-01", GarageUnitIdentityFormatter.BuildCallsign(-1));
        }

        [Test]
        public void BuildRoleLabel_UsesAnchorAndRange()
        {
            var longRange = new GaragePanelCatalog.FirepowerOption
            {
                DisplayName = "광유탄",
                Range = 6f,
            };
            var shortAnchor = new GaragePanelCatalog.MobilityOption
            {
                DisplayName = "고정포대",
                MoveRange = 2f,
            };
            var wideAnchor = new GaragePanelCatalog.MobilityOption
            {
                DisplayName = "경량",
                MoveRange = 7f,
            };

            Assert.AreEqual("고정 화력", GarageUnitIdentityFormatter.BuildRoleLabel(longRange, shortAnchor));
            Assert.AreEqual("침투 추적", GarageUnitIdentityFormatter.BuildRoleLabel(longRange, wideAnchor));
        }

        [Test]
        public void BuildServiceTagText_FormatsRememberedUnitTags()
        {
            Assert.AreEqual(
                "최장 전선 유지 42초",
                GarageUnitIdentityFormatter.BuildServiceTagText(GarageUnitServiceTag.LongestFrontlineHold(42)));
            Assert.AreEqual(
                "코어 근접 차단 31회",
                GarageUnitIdentityFormatter.BuildServiceTagText(GarageUnitServiceTag.CoreNearBlocks(31)));
            Assert.AreEqual(
                "최다 재출격 기체",
                GarageUnitIdentityFormatter.BuildServiceTagText(GarageUnitServiceTag.MostRedeployed()));
            Assert.AreEqual(
                "위기 순간 생존",
                GarageUnitIdentityFormatter.BuildServiceTagText(GarageUnitServiceTag.CrisisSurvivor()));
            Assert.AreEqual(
                "최근 주요 기여 기체",
                GarageUnitIdentityFormatter.BuildServiceTagText(new GarageUnitServiceTag(
                    GarageUnitServiceTagKind.RecentOperationContributor,
                    0)));
        }

    }
}
