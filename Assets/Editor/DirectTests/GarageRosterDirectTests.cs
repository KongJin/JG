using Features.Garage.Domain;
using NUnit.Framework;
using System.Collections.Generic;

namespace Tests.Editor
{
    /// <summary>
    /// Direct editor tests live outside asmdef-backed test assemblies so they can reference Assembly-CSharp directly.
    /// </summary>
    public sealed class GarageRosterDirectTests
    {
        [Test]
        public void SetSlot_ThreeCompleteUnits_AreValid()
        {
            var roster = new GarageRoster();
            roster.SetSlot(0, new GarageRoster.UnitLoadout("frame1", "fire1", "mob1"));
            roster.SetSlot(1, new GarageRoster.UnitLoadout("frame2", "fire2", "mob2"));
            roster.SetSlot(2, new GarageRoster.UnitLoadout("frame3", "fire3", "mob3"));

            Assert.AreEqual(3, roster.Count);
            Assert.IsTrue(roster.IsValid);
        }

        [Test]
        public void SetSlot_SixCompleteUnits_AreValid()
        {
            var roster = new GarageRoster();
            for (int i = 0; i < GarageRoster.MaxSlots; i++)
                roster.SetSlot(i, new GarageRoster.UnitLoadout($"frame{i}", $"fire{i}", $"mob{i}"));

            Assert.AreEqual(GarageRoster.MaxSlots, roster.Count);
            Assert.IsTrue(roster.IsValid);
        }

        [Test]
        public void Normalize_TrimsOverMaxSlots()
        {
            var loadouts = new List<GarageRoster.UnitLoadout>();
            for (int i = 0; i < GarageRoster.MaxSlots + 2; i++)
                loadouts.Add(new GarageRoster.UnitLoadout($"frame{i}", $"fire{i}", $"mob{i}"));

            var roster = new GarageRoster(loadouts);

            Assert.AreEqual(GarageRoster.MaxSlots, roster.loadout.Count);
            Assert.AreEqual(GarageRoster.MaxSlots, roster.Count);
        }

        [Test]
        public void ClearSlot_RemovesOnlySelectedSlot()
        {
            var roster = new GarageRoster();
            roster.SetSlot(0, new GarageRoster.UnitLoadout("frame1", "fire1", "mob1"));
            roster.SetSlot(1, new GarageRoster.UnitLoadout("frame2", "fire2", "mob2"));
            roster.SetSlot(2, new GarageRoster.UnitLoadout("frame3", "fire3", "mob3"));

            roster.ClearSlot(1);

            Assert.AreEqual(2, roster.Count);
            Assert.IsFalse(roster.IsValid);
            Assert.IsFalse(roster.GetSlot(1).HasAnySelection);
            Assert.IsTrue(roster.GetSlot(0).IsComplete);
            Assert.IsTrue(roster.GetSlot(2).IsComplete);
        }

        [Test]
        public void Clone_CreatesIndependentRoster()
        {
            var original = new GarageRoster();
            original.SetSlot(0, new GarageRoster.UnitLoadout("frame1", "fire1", "mob1"));

            var clone = original.Clone();
            clone.SetSlot(0, new GarageRoster.UnitLoadout("frameX", "fireX", "mobX"));

            Assert.AreEqual("frame1", original.GetSlot(0).frameId);
            Assert.AreEqual("frameX", clone.GetSlot(0).frameId);
        }

        [Test]
        public void GetFilledLoadouts_ReturnsOnlyCompleteLoadoutsInOrder()
        {
            var roster = new GarageRoster();
            roster.SetSlot(0, new GarageRoster.UnitLoadout("frame1", "fire1", "mob1"));
            roster.SetSlot(1, new GarageRoster.UnitLoadout("frame2", null, "mob2"));
            roster.SetSlot(2, new GarageRoster.UnitLoadout("frame3", "fire3", "mob3"));

            var filled = roster.GetFilledLoadouts();

            Assert.AreEqual(2, filled.Length);
            Assert.AreEqual("frame1", filled[0].frameId);
            Assert.AreEqual("frame3", filled[1].frameId);
        }
    }
}
