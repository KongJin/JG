using System.IO;
using System.Linq;
using NUnit.Framework;
using ProjectSD.LayerValidation;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class LayerDependencyAnalyzerDirectTests
    {
        [Test]
        public void Analyze_CurrentScriptsRoot_HasNoLayerViolations_AndNoFeatureCycles()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");

            var analysis = LayerDependencyAnalyzer.Analyze(scriptsRoot);

            Assert.That(analysis.layerViolations, Is.Empty, "LayerDependencyAnalyzer found layer violations.");
            Assert.IsFalse(analysis.report.hasCycles, "LayerDependencyAnalyzer found feature dependency cycles.");
        }

        [Test]
        public void Analyze_FlagsPresentationInfrastructureReferences()
        {
            var scriptsRoot = Path.Combine(Path.GetTempPath(), "LayerDependencyAnalyzerDirectTests", System.Guid.NewGuid().ToString("N"));
            try
            {
                var presentationPath = Path.Combine(scriptsRoot, "Features", "Garage", "Presentation");
                Directory.CreateDirectory(presentationPath);
                File.WriteAllText(
                    Path.Combine(presentationPath, "GarageView.cs"),
                    "using Features.Unit.Infrastructure;\nnamespace Features.Garage.Presentation { public sealed class GarageView {} }\n");

                var analysis = LayerDependencyAnalyzer.Analyze(scriptsRoot);

                Assert.That(
                    analysis.layerViolations.Any(violation =>
                        violation.message == "Presentation → Infrastructure 참조 금지" &&
                        violation.usingNamespace == "Features.Unit.Infrastructure"),
                    Is.True);
            }
            finally
            {
                if (Directory.Exists(scriptsRoot))
                    Directory.Delete(scriptsRoot, recursive: true);
            }
        }

        [Test]
        public void Analyze_FlagsPresentationFullyQualifiedInfrastructureReferences()
        {
            var scriptsRoot = Path.Combine(Path.GetTempPath(), "LayerDependencyAnalyzerDirectTests", System.Guid.NewGuid().ToString("N"));
            try
            {
                var presentationPath = Path.Combine(scriptsRoot, "Features", "Garage", "Presentation");
                Directory.CreateDirectory(presentationPath);
                File.WriteAllText(
                    Path.Combine(presentationPath, "GarageView.cs"),
                    "namespace Features.Garage.Presentation { public sealed class GarageView { private Features.Unit.Infrastructure.UnitFrameData frame; } }\n");

                var analysis = LayerDependencyAnalyzer.Analyze(scriptsRoot);

                Assert.That(
                    analysis.layerViolations.Any(violation =>
                        violation.message == "Presentation → Infrastructure 참조 금지" &&
                        violation.usingNamespace == "Features.Unit.Infrastructure.UnitFrameData"),
                    Is.True);
            }
            finally
            {
                if (Directory.Exists(scriptsRoot))
                    Directory.Delete(scriptsRoot, recursive: true);
            }
        }
    }
}
