using Features.Garage.Domain;
using Features.Lobby.Presentation;
using NUnit.Framework;

namespace Tests.Editor
{
    public sealed class LobbyReadyPolicyDirectTests
    {
        [Test]
        public void ComputeReadyEligible_RequiresValidSavedRoster()
        {
            var policy = new LobbyReadyPolicyController();
            var roster = CreateValidRoster();

            Assert.IsTrue(policy.ComputeReadyEligible(roster, false));
            Assert.IsFalse(policy.ComputeReadyEligible(roster, true));
            Assert.IsFalse(policy.ComputeReadyEligible(new GarageRoster(), false));
        }

        [Test]
        public void ShouldForceRelock_WhenSavedRosterBecomesIneligible()
        {
            var policy = new LobbyReadyPolicyController();

            Assert.IsTrue(policy.ShouldForceRelock(false, true, true));
            Assert.IsFalse(policy.ShouldForceRelock(true, true, true));
            Assert.IsFalse(policy.ShouldForceRelock(false, false, true));
            Assert.IsFalse(policy.ShouldForceRelock(false, true, false));
        }

        [Test]
        public void BuildReadyButtonLabel_ReflectsDraftAndRosterState()
        {
            var policy = new LobbyReadyPolicyController();

            Assert.AreEqual("Save Garage Draft", policy.BuildReadyButtonLabel(true, "ignored"));
            Assert.AreEqual("Need 3 Saved Units", policy.BuildReadyButtonLabel(false, string.Empty));
            Assert.AreEqual("Unsaved Garage changes", policy.BuildDraftBlockReason(string.Empty, true, false));
        }

        private static GarageRoster CreateValidRoster()
        {
            var roster = new GarageRoster();
            for (int i = 0; i < 3; i++)
            {
                roster.SetSlot(i, new GarageRoster.UnitLoadout(
                    $"frame-{i}",
                    $"fire-{i}",
                    $"mob-{i}"));
            }

            return roster;
        }
    }
}
