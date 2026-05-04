using Features.Garage.Domain;
using Features.Garage.Infrastructure;
using Features.Unit.Domain;
using Features.Unit.Infrastructure;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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

        [Test]
        public void ValidationProvider_AllowsMatchingFrameAndFirepowerAssemblyForm()
        {
            var fixture = CreateValidationFixture(AssemblyForm.Tower, AssemblyForm.Tower);
            try
            {
                var provider = new RosterValidationProvider(fixture.Catalog);

                Assert.IsTrue(provider.TryValidateComposition("frame", "fire", "mob", out var errorMessage), errorMessage);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void ValidationProvider_BlocksMismatchedFrameAndFirepowerAssemblyForm()
        {
            var fixture = CreateValidationFixture(AssemblyForm.Tower, AssemblyForm.Shoulder);
            try
            {
                var provider = new RosterValidationProvider(fixture.Catalog);

                Assert.IsFalse(provider.TryValidateComposition("frame", "fire", "mob", out var errorMessage));
                StringAssert.Contains("조립 형태", errorMessage);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        private static ValidationFixture CreateValidationFixture(AssemblyForm frameForm, AssemblyForm firepowerForm)
        {
            var frame = ScriptableObject.CreateInstance<UnitFrameData>();
            var firepower = ScriptableObject.CreateInstance<FirepowerModuleData>();
            var mobility = ScriptableObject.CreateInstance<MobilityModuleData>();
            var catalog = ScriptableObject.CreateInstance<ModuleCatalog>();

            SetString(frame, "frameId", "frame");
            SetEnum(frame, "assemblyForm", (int)frameForm);
            SetString(firepower, "moduleId", "fire");
            SetEnum(firepower, "assemblyForm", (int)firepowerForm);
            SetFloat(firepower, "range", 6f);
            SetString(mobility, "moduleId", "mob");
            SetFloat(mobility, "moveRange", 4f);
            SetCatalog(catalog, frame, firepower, mobility);

            return new ValidationFixture(catalog, frame, firepower, mobility);
        }

        private static void SetCatalog(
            ModuleCatalog catalog,
            UnitFrameData frame,
            FirepowerModuleData firepower,
            MobilityModuleData mobility)
        {
            var serialized = new SerializedObject(catalog);
            SetObjectArray(serialized.FindProperty("unitFrames"), frame);
            SetObjectArray(serialized.FindProperty("firepowerModules"), firepower);
            SetObjectArray(serialized.FindProperty("mobilityModules"), mobility);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(Object target, string propertyName, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(SerializedProperty property, Object value)
        {
            property.arraySize = 1;
            property.GetArrayElementAtIndex(0).objectReferenceValue = value;
        }

        private readonly struct ValidationFixture
        {
            public ValidationFixture(
                ModuleCatalog catalog,
                UnitFrameData frame,
                FirepowerModuleData firepower,
                MobilityModuleData mobility)
            {
                Catalog = catalog;
                Frame = frame;
                Firepower = firepower;
                Mobility = mobility;
            }

            public ModuleCatalog Catalog { get; }
            private UnitFrameData Frame { get; }
            private FirepowerModuleData Firepower { get; }
            private MobilityModuleData Mobility { get; }

            public void Destroy()
            {
                Object.DestroyImmediate(Catalog);
                Object.DestroyImmediate(Frame);
                Object.DestroyImmediate(Firepower);
                Object.DestroyImmediate(Mobility);
            }
        }
    }
}
