using System.IO;
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
    }
}
