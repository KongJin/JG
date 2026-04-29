using System.IO;
using NUnit.Framework;
using Shared.Ui;
using UnityEngine.UIElements;

namespace Tests.Editor
{
    public sealed class UitkIconRegistryDirectTests
    {
        private const string IconsUssPath = "Assets/UI/UIToolkit/Shared/TacticalIcons.uss";

        [Test]
        public void RegisteredIconsHaveUssClassAndPngAsset()
        {
            var uss = File.ReadAllText(IconsUssPath);
            foreach (var iconId in UitkIconRegistry.IconIds)
            {
                var className = UitkIconRegistry.GetClassName(iconId);
                StringAssert.Contains($".{className}", uss, iconId);

                var pngPath = ExtractPngPath(uss, className);
                Assert.IsTrue(File.Exists(Path.Combine("Assets/UI/UIToolkit/Shared", pngPath)), pngPath);
            }
        }

        [Test]
        public void ApplyRejectsUnknownIconId()
        {
            var element = new VisualElement();
            Assert.Throws<System.InvalidOperationException>(() => UitkIconRegistry.Apply(element, "missing_icon"));
        }

        private static string ExtractPngPath(string uss, string className)
        {
            var classIndex = uss.IndexOf($".{className}", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(classIndex, 0, className);
            var urlIndex = uss.IndexOf("url(\"", classIndex, System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(urlIndex, 0, className);
            var start = urlIndex + "url(\"".Length;
            var end = uss.IndexOf("\")", start, System.StringComparison.Ordinal);
            Assert.Greater(end, start, className);
            return uss.Substring(start, end - start);
        }
    }
}
