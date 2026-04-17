using System;
using System.Reflection;
using NUnit.Framework;

namespace Tests.Editor
{
    /// <summary>
    /// Assembly-CSharp를 직접 참조하지 않고 reflection으로 핵심 GarageRoster 동작을 검증한다.
    /// 기존 프로젝트가 asmdef-less 구조라 Test Runner 연결선만 먼저 복구한다.
    /// </summary>
    public sealed class GarageRosterReflectionTests
    {
        private static readonly Type RosterType = Type.GetType("Features.Garage.Domain.GarageRoster, Assembly-CSharp");
        private static readonly Type UnitLoadoutType = Type.GetType("Features.Garage.Domain.GarageRoster+UnitLoadout, Assembly-CSharp");

        [Test]
        public void GarageRosterType_IsAvailableFromAssemblyCSharp()
        {
            Assert.NotNull(RosterType);
            Assert.NotNull(UnitLoadoutType);
        }

        [Test]
        public void ThreeCompleteSlots_AreValid()
        {
            object roster = Activator.CreateInstance(RosterType);

            Invoke(roster, "SetSlot", 0, CreateLoadout("frame1", "fire1", "mob1"));
            Invoke(roster, "SetSlot", 1, CreateLoadout("frame2", "fire2", "mob2"));
            Invoke(roster, "SetSlot", 2, CreateLoadout("frame3", "fire3", "mob3"));

            Assert.AreEqual(3, GetProperty<int>(roster, "Count"));
            Assert.IsTrue(GetProperty<bool>(roster, "IsValid"));
        }

        [Test]
        public void ClearSlot_RemovesCommittedUnit()
        {
            object roster = Activator.CreateInstance(RosterType);

            Invoke(roster, "SetSlot", 0, CreateLoadout("frame1", "fire1", "mob1"));
            Invoke(roster, "SetSlot", 1, CreateLoadout("frame2", "fire2", "mob2"));
            Invoke(roster, "SetSlot", 2, CreateLoadout("frame3", "fire3", "mob3"));
            Invoke(roster, "ClearSlot", 1);

            Assert.AreEqual(2, GetProperty<int>(roster, "Count"));
            Assert.IsFalse(GetProperty<bool>(roster, "IsValid"));
        }

        private static object CreateLoadout(string frameId, string firepowerId, string mobilityId)
        {
            return Activator.CreateInstance(UnitLoadoutType, frameId, firepowerId, mobilityId);
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            return (T)RosterType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(target);
        }

        private static void Invoke(object target, string methodName, params object[] args)
        {
            RosterType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)?.Invoke(target, args);
        }
    }
}
