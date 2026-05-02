using NUnit.Framework;
using ProjectSD.EditorTools;

namespace Tests.Editor
{
    public sealed class Nova1492PlayablePartGenerationToolDirectTests
    {
        [Test]
        public void IsGeneratedEffectRendererNameForTest_FiltersKnownBazookaBeamOnly()
        {
            Assert.IsTrue(Nova1492PlayablePartGenerationTool.IsGeneratedEffectRendererNameForTest(
                "nova_fire_arm24_bzk",
                "datan_common_arm24_bzk_2d7ab4fb_mesh06",
                string.Empty));

            Assert.IsFalse(Nova1492PlayablePartGenerationTool.IsGeneratedEffectRendererNameForTest(
                "nova_fire_arm24_bzk",
                "datan_common_arm24_bzk_2d7ab4fb_mesh05",
                string.Empty));
        }

        [Test]
        public void IsGeneratedEffectRendererNameForTest_FiltersKnownCruiserBeamOnly()
        {
            Assert.IsTrue(Nova1492PlayablePartGenerationTool.IsGeneratedEffectRendererNameForTest(
                "nova_mob_n_legs40_krz",
                string.Empty,
                "datan_common_n_legs40_krz_cc420116_mesh01"));

            Assert.IsFalse(Nova1492PlayablePartGenerationTool.IsGeneratedEffectRendererNameForTest(
                "nova_mob_n_legs40_krz",
                string.Empty,
                "datan_common_n_legs40_krz_cc420116_mesh02"));
        }
    }
}
