using NUnit.Framework;
using Features.Garage.Domain;
using System.Collections.Generic;

namespace Tests.Garage.Domain
{
    /// <summary>
    /// GarageRoster 테스트.
    /// 편성 데이터 구조 + 유효성 검증.
    /// </summary>
    public class GarageRosterTests
    {
        [Test]
        public void IsValid_3기_유효()
        {
            var roster = new GarageRoster();
            roster.AddUnit(new GarageRoster.UnitLoadout("frame1", "fire1", "mob1"));
            roster.AddUnit(new GarageRoster.UnitLoadout("frame2", "fire2", "mob2"));
            roster.AddUnit(new GarageRoster.UnitLoadout("frame3", "fire3", "mob3"));

            Assert.IsTrue(roster.IsValid);
            Assert.AreEqual(3, roster.Count);
        }

        [Test]
        public void IsValid_5기_유효()
        {
            var roster = new GarageRoster();
            for (int i = 0; i < 5; i++)
                roster.AddUnit(new GarageRoster.UnitLoadout($"frame{i}", "fire1", "mob1"));

            Assert.IsTrue(roster.IsValid);
            Assert.AreEqual(5, roster.Count);
        }

        [Test]
        public void IsValid_2기_무효()
        {
            var roster = new GarageRoster();
            roster.AddUnit(new GarageRoster.UnitLoadout("frame1", "fire1", "mob1"));
            roster.AddUnit(new GarageRoster.UnitLoadout("frame2", "fire2", "mob2"));

            Assert.IsFalse(roster.IsValid);
            Assert.AreEqual(2, roster.Count);
        }

        [Test]
        public void IsValid_6기_무효()
        {
            var roster = new GarageRoster();
            for (int i = 0; i < 6; i++)
                roster.AddUnit(new GarageRoster.UnitLoadout($"frame{i}", "fire1", "mob1"));

            Assert.IsFalse(roster.IsValid);
            Assert.AreEqual(6, roster.Count);
        }

        [Test]
        public void IsValid_빈편성_무효()
        {
            var roster = new GarageRoster();
            Assert.IsFalse(roster.IsValid);
            Assert.AreEqual(0, roster.Count);
        }

        [Test]
        public void RemoveUnitAt_정상동작()
        {
            var roster = new GarageRoster();
            roster.AddUnit(new GarageRoster.UnitLoadout("frame1", "fire1", "mob1"));
            roster.AddUnit(new GarageRoster.UnitLoadout("frame2", "fire2", "mob2"));
            roster.AddUnit(new GarageRoster.UnitLoadout("frame3", "fire3", "mob3"));

            roster.RemoveUnitAt(1);

            Assert.AreEqual(2, roster.Count);
            Assert.IsFalse(roster.IsValid);
        }

        [Test]
        public void UpdateUnit_정상동작()
        {
            var roster = new GarageRoster();
            roster.AddUnit(new GarageRoster.UnitLoadout("frame1", "fire_single", "mob_armor"));

            roster.UpdateUnit(0, new GarageRoster.UnitLoadout("frame1", "fire_aoe", "mob_light"));

            Assert.AreEqual("fire_aoe", roster.loadout[0].firepowerModuleId);
            Assert.AreEqual("mob_light", roster.loadout[0].mobilityModuleId);
        }

        [Test]
        public void Clear_편성초기화()
        {
            var roster = new GarageRoster();
            for (int i = 0; i < 3; i++)
                roster.AddUnit(new GarageRoster.UnitLoadout($"frame{i}", "fire1", "mob1"));

            roster.Clear();

            Assert.AreEqual(0, roster.Count);
            Assert.IsFalse(roster.IsValid);
        }
    }
}
