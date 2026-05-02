using NUnit.Framework;
using Shared.Runtime;

namespace Tests.Editor
{
    public sealed class SampleOptionPickerDirectTests
    {
        [Test]
        public void TryPickFirst_ReturnsFirstMatchingOption()
        {
            var options = new[]
            {
                new SampleOption("no-preview", false),
                new SampleOption("preview-a", true),
                new SampleOption("preview-b", true)
            };

            Assert.IsTrue(SampleOptionPicker.TryPickFirst(
                options,
                option => option.HasPreview,
                out var selected));
            Assert.AreEqual("preview-a", selected.Id);
        }

        [Test]
        public void TryPickPreferredOrFirst_UsesFallbackWhenPreferredIsUnavailable()
        {
            var options = new[]
            {
                new SampleOption("fallback", true),
                new SampleOption("preferred-without-preview", false)
            };

            Assert.IsTrue(SampleOptionPicker.TryPickPreferredOrFirst(
                options,
                option => option.Id == "preferred" && option.HasPreview,
                option => option.HasPreview,
                out var selected));
            Assert.AreEqual("fallback", selected.Id);
        }

        private sealed class SampleOption
        {
            public SampleOption(string id, bool hasPreview)
            {
                Id = id;
                HasPreview = hasPreview;
            }

            public string Id { get; }
            public bool HasPreview { get; }
        }
    }
}
