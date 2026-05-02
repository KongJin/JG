using NUnit.Framework;
using Shared.Gameplay;
using Shared.Math;

namespace Editor.DirectTests
{
    public sealed class SharedCommonDirectTests
    {
        [Test]
        public void LoadoutKey_BuildsTrimmedStableKey()
        {
            Assert.AreEqual("guardian|single|heavy", LoadoutKey.Build(" guardian ", "single", "heavy"));
            Assert.AreEqual("-|single|-", LoadoutKey.Build(null, "single", " "));
        }

        [Test]
        public void DifficultyPreset_DefinesSharedRoomContract()
        {
            Assert.AreEqual("difficultyPreset", DifficultyPreset.RoomPropertyKey);
            Assert.IsTrue(DifficultyPreset.IsDefined(DifficultyPreset.Normal));
            Assert.IsTrue(DifficultyPreset.IsDefined(DifficultyPreset.Easy));
            Assert.IsTrue(DifficultyPreset.IsDefined(DifficultyPreset.Hard));
            Assert.IsFalse(DifficultyPreset.IsDefined(3));
            Assert.AreEqual("Hard", DifficultyPreset.ToShortLabel(DifficultyPreset.Hard));
        }

        [Test]
        public void FloatValidation_RejectsNonFiniteValues()
        {
            Assert.IsTrue(FloatValidation.IsFinite(new Float3(1f, 2f, 3f)));
            Assert.IsFalse(FloatValidation.IsFinite(float.NaN));
            Assert.IsFalse(FloatValidation.IsFinite(new Float3(1f, float.PositiveInfinity, 3f)));
        }
    }
}
