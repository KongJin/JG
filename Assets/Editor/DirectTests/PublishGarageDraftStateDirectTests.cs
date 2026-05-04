using Features.Garage.Application;
using Features.Garage.Domain;
using NUnit.Framework;

namespace Tests.Editor
{
    /// <summary>
    /// <see cref="PublishGarageDraftStateUseCase"/>가 <see cref="GarageRoster.MinReadySlots"/>와
    /// 어긋나지 않도록 고정 숫자를 쓰지 않는지 검증한다 (결과 패널·이벤트 스냅샷 정합).
    /// </summary>
    public sealed class PublishGarageDraftStateDirectTests
    {
        [Test]
        public void IncompleteRoster_BlockReasonUsesMinReadySlotsGap()
        {
            var roster = new GarageRoster();
            roster.SetSlot(0, new GarageRoster.UnitLoadout("f0", "fp0", "m0"));
            roster.SetSlot(1, new GarageRoster.UnitLoadout("f1", "fp1", "m1"));

            var sut = new PublishGarageDraftStateUseCase();
            var snap = sut.Build(roster, hasUnsavedChanges: false);

            Assert.IsFalse(snap.ReadyEligible);
            int expectedMissing = GarageRoster.MinReadySlots - roster.Count;
            Assert.Greater(expectedMissing, 0);
            StringAssert.Contains($"{expectedMissing}", snap.BlockReason);
        }

        [Test]
        public void ReadyRoster_NoUnsavedChanges_IsReadyEligible()
        {
            var roster = new GarageRoster();
            for (int i = 0; i < GarageRoster.MinReadySlots; i++)
                roster.SetSlot(i, new GarageRoster.UnitLoadout($"f{i}", $"fp{i}", $"m{i}"));

            var sut = new PublishGarageDraftStateUseCase();
            var snap = sut.Build(roster, hasUnsavedChanges: false);

            Assert.IsTrue(roster.IsValid);
            Assert.IsTrue(snap.ReadyEligible);
            Assert.IsFalse(snap.HasUnsavedChanges);
        }
    }
}
