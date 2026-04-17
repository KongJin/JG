using System;
using System.Collections;
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

        [Test]
        public void Normalize_TrimsOverMaxSlots()
        {
            var loadouts = CreateLoadoutList(8);
            object roster = Activator.CreateInstance(RosterType, loadouts);

            var normalizedLoadout = GetField<IList>(roster, "loadout");

            Assert.AreEqual(6, normalizedLoadout.Count);
            Assert.AreEqual(6, GetProperty<int>(roster, "Count"));
        }

        [Test]
        public void Clone_CreatesIndependentRoster()
        {
            object original = Activator.CreateInstance(RosterType);
            Invoke(original, "SetSlot", 0, CreateLoadout("frame1", "fire1", "mob1"));

            object clone = InvokeWithResult(original, "Clone");
            Invoke(clone, "SetSlot", 0, CreateLoadout("frameX", "fireX", "mobX"));

            object originalSlot = InvokeWithResult(original, "GetSlot", 0);
            object cloneSlot = InvokeWithResult(clone, "GetSlot", 0);

            Assert.AreEqual("frame1", GetField<string>(originalSlot, "frameId"));
            Assert.AreEqual("frameX", GetField<string>(cloneSlot, "frameId"));
        }

        [Test]
        public void GetFilledLoadouts_ReturnsOnlyCompleteLoadoutsInOrder()
        {
            object roster = Activator.CreateInstance(RosterType);

            Invoke(roster, "SetSlot", 0, CreateLoadout("frame1", "fire1", "mob1"));
            Invoke(roster, "SetSlot", 1, CreateLoadout("frame2", null, "mob2"));
            Invoke(roster, "SetSlot", 2, CreateLoadout("frame3", "fire3", "mob3"));

            var filled = (Array)InvokeWithResult(roster, "GetFilledLoadouts");

            Assert.AreEqual(2, filled.Length);
            Assert.AreEqual("frame1", GetField<string>(filled.GetValue(0), "frameId"));
            Assert.AreEqual("frame3", GetField<string>(filled.GetValue(1), "frameId"));
        }

        private static object CreateLoadout(string frameId, string firepowerId, string mobilityId)
        {
            return Activator.CreateInstance(UnitLoadoutType, frameId, firepowerId, mobilityId);
        }

        private static object CreateLoadoutList(int count)
        {
            Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(UnitLoadoutType);
            IList list = (IList)Activator.CreateInstance(listType);

            for (int i = 0; i < count; i++)
                list.Add(CreateLoadout($"frame{i}", $"fire{i}", $"mob{i}"));

            return list;
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            return (T)RosterType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(target);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            return (T)target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(target);
        }

        private static void Invoke(object target, string methodName, params object[] args)
        {
            RosterType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)?.Invoke(target, args);
        }

        private static object InvokeWithResult(object target, string methodName, params object[] args)
        {
            return target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)?.Invoke(target, args);
        }
    }
}
