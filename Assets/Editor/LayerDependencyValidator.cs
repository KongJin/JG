using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

#if !LAYER_VALIDATION_NO_UNITY
using UnityEditor;
using UnityEngine;
#endif

namespace ProjectSD.LayerValidation
{
    public static class LayerDependencyAnalyzer
    {
        private static readonly Regex UsingRegex = new Regex(
            @"^\s*using\s+(?:static\s+)?(?:\w+\s*=\s*)?([\w.]+)\s*;"
        );

        private static readonly Regex ForbiddenLayerRegex = new Regex(@"^Features\.\w+\.(\w+)");
        private static readonly Regex FeatureReferenceRegex = new Regex(
            @"\b(Features\.(\w+)(?:\.[A-Za-z_]\w*)+)"
        );

        public const string DependencyReportRelativePath = "Temp/LayerDependencyValidator/feature-dependencies.json";

        public static LayerDependencyAnalysisResult Analyze(string scriptsRoot)
        {
            if (string.IsNullOrWhiteSpace(scriptsRoot))
                throw new ArgumentException("scriptsRoot is required.", "scriptsRoot");

            if (!Directory.Exists(scriptsRoot))
            {
                return new LayerDependencyAnalysisResult
                {
                    layerViolations = Array.Empty<LayerViolation>(),
                    report = new FeatureDependencyReport
                    {
                        generatedAtUtc = DateTime.UtcNow.ToString("o"),
                        featureCount = 0,
                        edgeCount = 0,
                        hasCycles = false,
                        edges = Array.Empty<FeatureDependencyEdge>(),
                        cycles = Array.Empty<FeatureDependencyCycle>()
                    }
                };
            }

            var csFiles = Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories);
            Array.Sort(csFiles, StringComparer.OrdinalIgnoreCase);

            var features = DiscoverFeatures(scriptsRoot);
            var layerViolations = new List<LayerViolation>();
            var edgeMap = new Dictionary<string, FeatureEdgeAccumulator>(StringComparer.Ordinal);

            foreach (var file in csFiles)
            {
                var normalizedPath = file.Replace("\\", "/");
                var relativePath = ToAssetRelativePath(normalizedPath);
                var layer = DetectLayer(normalizedPath);
                var rawLines = File.ReadAllLines(file);
                var sanitizedText = StripCommentsAndStrings(File.ReadAllText(file));
                var sanitizedLines = NormalizeNewlines(sanitizedText).Split('\n');

                if (layer != null)
                {
                    CollectLayerViolations(relativePath, layer, rawLines, layerViolations);
                }

                CollectFeatureDependencies(relativePath, normalizedPath, layer, rawLines, sanitizedLines, features, edgeMap);
            }

            return new LayerDependencyAnalysisResult
            {
                layerViolations = layerViolations.ToArray(),
                report = BuildFeatureDependencyReport(features, edgeMap)
            };
        }

        private static string DetectLayer(string path)
        {
            if (Contains(path, "/Shared/"))
                return "Shared";
            if (Contains(path, "/Bootstrap/"))
                return "Bootstrap";
            if (Contains(path, "/Infrastructure/"))
                return "Infrastructure";
            if (Contains(path, "/Application/"))
                return "Application";
            if (Contains(path, "/Domain/"))
                return "Domain";
            return null;
        }

        private static string Check(string ns, string layer)
        {
            switch (layer)
            {
                case "Domain":
                    if (ns.StartsWith("UnityEngine", StringComparison.Ordinal))
                        return "Domain → UnityEngine 금지";
                    if (ns.StartsWith("UnityEditor", StringComparison.Ordinal))
                        return "Domain → UnityEditor 금지";
                    if (ns.StartsWith("Photon", StringComparison.Ordinal))
                        return "Domain → Photon 금지";
                    if (ns.StartsWith("System.IO", StringComparison.Ordinal))
                        return "Domain → System.IO 금지";
                    if (CheckForbiddenLayer(ns, "Application"))
                        return "Domain → Application 참조 금지";
                    if (CheckForbiddenLayer(ns, "Infrastructure"))
                        return "Domain → Infrastructure 참조 금지";
                    if (CheckForbiddenLayer(ns, "Bootstrap"))
                        return "Domain → Bootstrap 참조 금지";
                    break;

                case "Application":
                    if (ns.StartsWith("UnityEngine", StringComparison.Ordinal))
                        return "Application → UnityEngine 금지";
                    if (ns.StartsWith("UnityEditor", StringComparison.Ordinal))
                        return "Application → UnityEditor 금지";
                    if (ns.StartsWith("Photon", StringComparison.Ordinal))
                        return "Application → Photon 금지";
                    if (CheckForbiddenLayer(ns, "Infrastructure"))
                        return "Application → Infrastructure 참조 금지";
                    if (CheckForbiddenLayer(ns, "Bootstrap"))
                        return "Application → Bootstrap 참조 금지";
                    break;

                case "Infrastructure":
                    if (CheckForbiddenLayer(ns, "Bootstrap"))
                        return "Infrastructure → Bootstrap 참조 금지";
                    break;

                case "Shared":
                    if (ns.StartsWith("Features.", StringComparison.Ordinal))
                        return "Shared → Features 참조 금지";
                    break;
            }

            return null;
        }

        private static string[] DiscoverFeatures(string scriptsRoot)
        {
            var featuresRoot = Path.Combine(scriptsRoot, "Features");
            if (!Directory.Exists(featuresRoot))
                return Array.Empty<string>();

            return Directory
                .GetDirectories(featuresRoot)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
        }

        private static void CollectLayerViolations(
            string relativePath,
            string layer,
            string[] lines,
            List<LayerViolation> layerViolations)
        {
            for (var i = 0; i < lines.Length; i++)
            {
                var match = UsingRegex.Match(lines[i]);
                if (!match.Success)
                    continue;

                var ns = match.Groups[1].Value;
                var violation = Check(ns, layer);
                if (violation == null)
                    continue;

                layerViolations.Add(new LayerViolation
                {
                    path = relativePath,
                    line = i + 1,
                    message = violation,
                    usingNamespace = ns
                });
            }
        }

        private static void CollectFeatureDependencies(
            string relativePath,
            string normalizedPath,
            string layer,
            string[] rawLines,
            string[] sanitizedLines,
            string[] features,
            Dictionary<string, FeatureEdgeAccumulator> edgeMap)
        {
            if (string.IsNullOrWhiteSpace(layer))
                return;

            if (IsGraphIgnoredFile(normalizedPath))
                return;

            var currentFeature = DetectFeatureName(normalizedPath);
            if (string.IsNullOrWhiteSpace(currentFeature))
                return;

            for (var i = 0; i < rawLines.Length; i++)
            {
                var usingMatch = UsingRegex.Match(rawLines[i]);
                if (usingMatch.Success)
                {
                    string fromFeature;
                    string toFeature;
                    if (TryResolveFeatureEdge(
                        currentFeature,
                        usingMatch.Groups[1].Value,
                        features,
                        out fromFeature,
                        out toFeature))
                    {
                        AddEdge(edgeMap, fromFeature, toFeature, relativePath, i + 1);
                    }
                }

                if (i >= sanitizedLines.Length)
                    continue;

                foreach (Match featureMatch in FeatureReferenceRegex.Matches(sanitizedLines[i]))
                {
                    string fromFeature;
                    string toFeature;
                    if (!TryResolveFeatureEdge(
                        currentFeature,
                        featureMatch.Groups[1].Value,
                        features,
                        out fromFeature,
                        out toFeature))
                        continue;

                    AddEdge(edgeMap, fromFeature, toFeature, relativePath, i + 1);
                }
            }
        }

        private static string DetectFeatureName(string normalizedPath)
        {
            const string marker = "/Features/";
            var index = normalizedPath.IndexOf(marker, StringComparison.Ordinal);
            if (index < 0)
                return null;

            var start = index + marker.Length;
            var end = normalizedPath.IndexOf('/', start);
            if (end < 0 || end <= start)
                return null;

            return normalizedPath.Substring(start, end - start);
        }

        private static string ExtractFeatureNameFromNamespace(string namespaceOrType, string[] features)
        {
            if (string.IsNullOrWhiteSpace(namespaceOrType) || !namespaceOrType.StartsWith("Features.", StringComparison.Ordinal))
                return null;

            foreach (var feature in features)
            {
                var prefix = "Features." + feature + ".";
                if (namespaceOrType.StartsWith(prefix, StringComparison.Ordinal))
                    return feature;

                if (string.Equals(namespaceOrType, "Features." + feature, StringComparison.Ordinal))
                    return feature;
            }

            return null;
        }

        private static bool IsFeatureDependency(string currentFeature, string dependencyFeature)
        {
            return !string.IsNullOrWhiteSpace(dependencyFeature) &&
                !string.Equals(currentFeature, dependencyFeature, StringComparison.Ordinal) &&
                !string.Equals(dependencyFeature, "Shared", StringComparison.Ordinal) &&
                !string.Equals(dependencyFeature, "Editor", StringComparison.Ordinal);
        }

        private static bool TryResolveFeatureEdge(
            string currentFeature,
            string namespaceOrType,
            string[] features,
            out string fromFeature,
            out string toFeature)
        {
            fromFeature = null;
            toFeature = null;

            var referencedFeature = ExtractFeatureNameFromNamespace(namespaceOrType, features);
            if (!IsFeatureDependency(currentFeature, referencedFeature))
                return false;

            if (IsConsumerOwnedPortReference(namespaceOrType, referencedFeature))
                return false;

            fromFeature = currentFeature;
            toFeature = referencedFeature;
            return true;
        }

        private static bool IsConsumerOwnedPortReference(string namespaceOrType, string feature)
        {
            var prefix = "Features." + feature + ".Application.Ports";
            return string.Equals(namespaceOrType, prefix, StringComparison.Ordinal) ||
                namespaceOrType.StartsWith(prefix + ".", StringComparison.Ordinal);
        }

        private static bool IsGraphIgnoredFile(string normalizedPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(normalizedPath);
            return fileName.IndexOf("Analytics", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void AddEdge(
            Dictionary<string, FeatureEdgeAccumulator> edgeMap,
            string fromFeature,
            string toFeature,
            string relativePath,
            int line)
        {
            var key = GetEdgeKey(fromFeature, toFeature);
            FeatureEdgeAccumulator edge;
            if (!edgeMap.TryGetValue(key, out edge))
            {
                edge = new FeatureEdgeAccumulator
                {
                    from = fromFeature,
                    to = toFeature
                };
                edgeMap.Add(key, edge);
            }

            edge.AddEvidence(relativePath, line);
        }

        private static FeatureDependencyReport BuildFeatureDependencyReport(
            string[] features,
            Dictionary<string, FeatureEdgeAccumulator> edgeMap)
        {
            var orderedEdges = edgeMap.Values
                .OrderBy(edge => edge.from, StringComparer.Ordinal)
                .ThenBy(edge => edge.to, StringComparer.Ordinal)
                .Select(edge => edge.ToReportEdge())
                .ToArray();

            var cycles = DetectCycles(features, edgeMap);

            return new FeatureDependencyReport
            {
                generatedAtUtc = DateTime.UtcNow.ToString("o"),
                featureCount = features.Length,
                edgeCount = orderedEdges.Length,
                hasCycles = cycles.Length > 0,
                edges = orderedEdges,
                cycles = cycles
            };
        }

        private static FeatureDependencyCycle[] DetectCycles(
            string[] features,
            Dictionary<string, FeatureEdgeAccumulator> edgeMap)
        {
            var adjacency = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var feature in features)
            {
                adjacency[feature] = new List<string>();
            }

            foreach (var edge in edgeMap.Values)
            {
                List<string> neighbors;
                if (!adjacency.TryGetValue(edge.from, out neighbors))
                    continue;

                if (!neighbors.Contains(edge.to))
                    neighbors.Add(edge.to);
            }

            foreach (var neighbors in adjacency.Values)
            {
                neighbors.Sort(StringComparer.Ordinal);
            }

            var state = new Dictionary<string, int>(StringComparer.Ordinal);
            var stack = new List<string>();
            var cycles = new Dictionary<string, FeatureDependencyCycle>(StringComparer.Ordinal);

            foreach (var feature in features.OrderBy(name => name, StringComparer.Ordinal))
            {
                if (!state.ContainsKey(feature))
                {
                    VisitFeature(feature, adjacency, edgeMap, state, stack, cycles);
                }
            }

            return cycles.Values
                .OrderBy(cycle => string.Join("->", cycle.features), StringComparer.Ordinal)
                .ToArray();
        }

        private static void VisitFeature(
            string feature,
            Dictionary<string, List<string>> adjacency,
            Dictionary<string, FeatureEdgeAccumulator> edgeMap,
            Dictionary<string, int> state,
            List<string> stack,
            Dictionary<string, FeatureDependencyCycle> cycles)
        {
            state[feature] = 1;
            stack.Add(feature);

            List<string> neighbors;
            if (adjacency.TryGetValue(feature, out neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    int neighborState;
                    if (!state.TryGetValue(neighbor, out neighborState))
                    {
                        VisitFeature(neighbor, adjacency, edgeMap, state, stack, cycles);
                        continue;
                    }

                    if (neighborState != 1)
                        continue;

                    var cycleStart = stack.IndexOf(neighbor);
                    if (cycleStart < 0)
                        continue;

                    var cyclePath = stack.GetRange(cycleStart, stack.Count - cycleStart);
                    RegisterCycle(cyclePath, edgeMap, cycles);
                }
            }

            stack.RemoveAt(stack.Count - 1);
            state[feature] = 2;
        }

        private static void RegisterCycle(
            List<string> cyclePath,
            Dictionary<string, FeatureEdgeAccumulator> edgeMap,
            Dictionary<string, FeatureDependencyCycle> cycles)
        {
            if (cyclePath.Count == 0)
                return;

            var signature = GetCycleSignature(cyclePath);
            if (cycles.ContainsKey(signature))
                return;

            var evidence = new List<FeatureDependencyEvidence>();
            var cycleEdges = new List<FeatureDependencyEdge>();
            for (var i = 0; i < cyclePath.Count; i++)
            {
                var from = cyclePath[i];
                var to = cyclePath[(i + 1) % cyclePath.Count];
                FeatureEdgeAccumulator edge;
                if (!edgeMap.TryGetValue(GetEdgeKey(from, to), out edge) || edge.evidence.Count == 0)
                    continue;

                evidence.Add(edge.evidence[0]);
                cycleEdges.Add(edge.ToReportEdge());
            }

            cycles.Add(signature, new FeatureDependencyCycle
            {
                features = cyclePath.ToArray(),
                evidence = evidence.ToArray(),
                edges = cycleEdges.ToArray(),
                preferredBreakCandidates = GetPreferredBreakCandidates(cycleEdges).ToArray()
            });
        }

        private static IEnumerable<FeatureDependencyBreakCandidate> GetPreferredBreakCandidates(
            IEnumerable<FeatureDependencyEdge> cycleEdges)
        {
            foreach (var edge in cycleEdges)
            {
                var evidence = edge.evidence != null && edge.evidence.Length > 0 ? edge.evidence[0] : null;
                if (evidence == null || string.IsNullOrWhiteSpace(evidence.path))
                    continue;

                if (IsCompositionRootPath(evidence.path) || IsConsumerOwnedPortReferencePath(evidence.path))
                    continue;

                if (!(Contains(evidence.path, "/Application/") ||
                      Contains(evidence.path, "/Infrastructure/")))
                    continue;

                yield return new FeatureDependencyBreakCandidate
                {
                    recipe = "port_inversion",
                    from = edge.from,
                    to = edge.to,
                    reason = "Cross-feature concrete dependency from consumer code may be inverted through a consumer-owned Application/Ports seam.",
                    evidence = new[]
                    {
                        new FeatureDependencyEvidence
                        {
                            path = evidence.path,
                            line = evidence.line
                        }
                    }
                };
            }
        }

        private static string GetCycleSignature(List<string> cyclePath)
        {
            var rotations = new List<string>(cyclePath.Count);
            for (var start = 0; start < cyclePath.Count; start++)
            {
                var builder = new StringBuilder();
                for (var offset = 0; offset < cyclePath.Count; offset++)
                {
                    if (offset > 0)
                        builder.Append("->");

                    builder.Append(cyclePath[(start + offset) % cyclePath.Count]);
                }

                rotations.Add(builder.ToString());
            }

            rotations.Sort(StringComparer.Ordinal);
            return rotations[0];
        }

        private static string ToAssetRelativePath(string normalizedPath)
        {
            var assetsIndex = normalizedPath.IndexOf("/Assets/", StringComparison.Ordinal);
            if (assetsIndex >= 0)
                return normalizedPath.Substring(assetsIndex + 1);

            if (normalizedPath.EndsWith("/Assets", StringComparison.Ordinal))
                return "Assets";

            return normalizedPath;
        }

        private static string GetEdgeKey(string fromFeature, string toFeature)
        {
            return fromFeature + "->" + toFeature;
        }

        private static bool IsCompositionRootPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return false;

            var fileName = Path.GetFileNameWithoutExtension(relativePath);
            return fileName.EndsWith("Setup", StringComparison.Ordinal) ||
                fileName.EndsWith("Bootstrap", StringComparison.Ordinal);
        }

        private static bool IsConsumerOwnedPortReferencePath(string relativePath)
        {
            return !string.IsNullOrWhiteSpace(relativePath) && Contains(relativePath, "/Application/Ports/");
        }

        private static string NormalizeNewlines(string text)
        {
            return text.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        private static string StripCommentsAndStrings(string text)
        {
            var builder = new StringBuilder(text.Length);
            var state = SanitizerState.Code;

            for (var i = 0; i < text.Length; i++)
            {
                var current = text[i];
                var next = i + 1 < text.Length ? text[i + 1] : '\0';

                switch (state)
                {
                    case SanitizerState.Code:
                        if (current == '/' && next == '/')
                        {
                            builder.Append("  ");
                            state = SanitizerState.LineComment;
                            i++;
                            break;
                        }

                        if (current == '/' && next == '*')
                        {
                            builder.Append("  ");
                            state = SanitizerState.BlockComment;
                            i++;
                            break;
                        }

                        if (current == '@' && next == '"')
                        {
                            builder.Append("  ");
                            state = SanitizerState.VerbatimString;
                            i++;
                            break;
                        }

                        if (current == '"')
                        {
                            builder.Append(' ');
                            state = SanitizerState.StringLiteral;
                            break;
                        }

                        if (current == '\'')
                        {
                            builder.Append(' ');
                            state = SanitizerState.CharLiteral;
                            break;
                        }

                        builder.Append(current);
                        break;

                    case SanitizerState.LineComment:
                        if (current == '\r' || current == '\n')
                        {
                            builder.Append(current);
                            state = SanitizerState.Code;
                        }
                        else
                        {
                            builder.Append(' ');
                        }
                        break;

                    case SanitizerState.BlockComment:
                        if (current == '*' && next == '/')
                        {
                            builder.Append("  ");
                            state = SanitizerState.Code;
                            i++;
                        }
                        else
                        {
                            builder.Append(current == '\r' || current == '\n' ? current : ' ');
                        }
                        break;

                    case SanitizerState.StringLiteral:
                        if (current == '\\' && next != '\0')
                        {
                            builder.Append("  ");
                            i++;
                        }
                        else if (current == '"')
                        {
                            builder.Append(' ');
                            state = SanitizerState.Code;
                        }
                        else
                        {
                            builder.Append(current == '\r' || current == '\n' ? current : ' ');
                        }
                        break;

                    case SanitizerState.VerbatimString:
                        if (current == '"' && next == '"')
                        {
                            builder.Append("  ");
                            i++;
                        }
                        else if (current == '"')
                        {
                            builder.Append(' ');
                            state = SanitizerState.Code;
                        }
                        else
                        {
                            builder.Append(current == '\r' || current == '\n' ? current : ' ');
                        }
                        break;

                    case SanitizerState.CharLiteral:
                        if (current == '\\' && next != '\0')
                        {
                            builder.Append("  ");
                            i++;
                        }
                        else if (current == '\'')
                        {
                            builder.Append(' ');
                            state = SanitizerState.Code;
                        }
                        else
                        {
                            builder.Append(current == '\r' || current == '\n' ? current : ' ');
                        }
                        break;
                }
            }

            return builder.ToString();
        }

        private static bool CheckForbiddenLayer(string ns, string forbiddenLayer)
        {
            if (!ns.StartsWith("Features.", StringComparison.Ordinal))
                return false;

            var match = ForbiddenLayerRegex.Match(ns);
            return match.Success && match.Groups[1].Value == forbiddenLayer;
        }

        private static bool Contains(string path, string segment)
        {
            return path.IndexOf(segment, StringComparison.Ordinal) >= 0;
        }

        private enum SanitizerState
        {
            Code,
            LineComment,
            BlockComment,
            StringLiteral,
            VerbatimString,
            CharLiteral
        }

        private sealed class FeatureEdgeAccumulator
        {
            public string from;
            public string to;
            public readonly List<FeatureDependencyEvidence> evidence = new List<FeatureDependencyEvidence>();
            private readonly HashSet<string> _evidenceKeys = new HashSet<string>(StringComparer.Ordinal);

            public void AddEvidence(string path, int line)
            {
                var key = path + ":" + line;
                if (!_evidenceKeys.Add(key))
                    return;

                evidence.Add(new FeatureDependencyEvidence
                {
                    path = path,
                    line = line
                });

                evidence.Sort((left, right) =>
                {
                    var pathCompare = string.CompareOrdinal(left.path, right.path);
                    if (pathCompare != 0)
                        return pathCompare;

                    return left.line.CompareTo(right.line);
                });
            }

            public FeatureDependencyEdge ToReportEdge()
            {
                return new FeatureDependencyEdge
                {
                    from = from,
                    to = to,
                    evidence = evidence.ToArray()
                };
            }
        }
    }

    [Serializable]
    public sealed class LayerDependencyAnalysisResult
    {
        public LayerViolation[] layerViolations;
        public FeatureDependencyReport report;
    }

    [Serializable]
    public sealed class FeatureDependencyReport
    {
        public string generatedAtUtc;
        public int featureCount;
        public int edgeCount;
        public bool hasCycles;
        public FeatureDependencyEdge[] edges;
        public FeatureDependencyCycle[] cycles;
    }

    [Serializable]
    public sealed class FeatureDependencyEdge
    {
        public string from;
        public string to;
        public FeatureDependencyEvidence[] evidence;
    }

    [Serializable]
    public sealed class FeatureDependencyCycle
    {
        public string[] features;
        public FeatureDependencyEvidence[] evidence;
        public FeatureDependencyEdge[] edges;
        public FeatureDependencyBreakCandidate[] preferredBreakCandidates;
    }

    [Serializable]
    public sealed class FeatureDependencyBreakCandidate
    {
        public string recipe;
        public string from;
        public string to;
        public string reason;
        public FeatureDependencyEvidence[] evidence;
    }

    [Serializable]
    public sealed class FeatureDependencyEvidence
    {
        public string path;
        public int line;
    }

    [Serializable]
    public sealed class LayerViolation
    {
        public string path;
        public int line;
        public string message;
        public string usingNamespace;
    }
}

#if !LAYER_VALIDATION_NO_UNITY
namespace Editor
{
    [InitializeOnLoad]
    public static class LayerDependencyValidator
    {
        static LayerDependencyValidator()
        {
            Validate(silent: true);
        }

        [MenuItem("Tools/Validate Layer Dependencies")]
        public static void ValidateFromMenu()
        {
            Validate(silent: false);
        }

        private static void Validate(bool silent)
        {
            var scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            var repoRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(repoRoot) || !Directory.Exists(scriptsRoot))
                return;

            var analysis = ProjectSD.LayerValidation.LayerDependencyAnalyzer.Analyze(scriptsRoot);
            WriteFeatureDependencyReport(repoRoot, analysis.report);

            LogLayerViolations(analysis.layerViolations);
            LogFeatureCycles(analysis.report.cycles);

            if (!silent)
            {
                if (analysis.layerViolations.Length == 0)
                    Debug.Log("[Layer Rule] No violations found.");

                if (analysis.report.hasCycles)
                {
                    Debug.LogError(
                        $"[Feature Dependency Rule] {analysis.report.cycles.Length} cycle(s) found. JSON: {ProjectSD.LayerValidation.LayerDependencyAnalyzer.DependencyReportRelativePath}"
                    );
                }
                else
                {
                    Debug.Log(
                        $"[Feature Dependency Rule] Graph is acyclic. Features={analysis.report.featureCount}, Edges={analysis.report.edgeCount}. JSON: {ProjectSD.LayerValidation.LayerDependencyAnalyzer.DependencyReportRelativePath}"
                    );
                }
            }
        }

        private static void WriteFeatureDependencyReport(string repoRoot, ProjectSD.LayerValidation.FeatureDependencyReport report)
        {
            var outputPath = Path.Combine(repoRoot, ProjectSD.LayerValidation.LayerDependencyAnalyzer.DependencyReportRelativePath);
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var json = JsonUtility.ToJson(report, prettyPrint: true);
            File.WriteAllText(outputPath, json);
        }

        private static void LogLayerViolations(ProjectSD.LayerValidation.LayerViolation[] layerViolations)
        {
            foreach (var violation in layerViolations)
            {
                Debug.LogError(
                    $"[Layer Rule] {violation.path}:{violation.line} — {violation.message}\n  using {violation.usingNamespace};"
                );
            }

            if (layerViolations.Length > 0)
            {
                Debug.LogError($"[Layer Rule] {layerViolations.Length} violation(s) found.");
            }
        }

        private static void LogFeatureCycles(ProjectSD.LayerValidation.FeatureDependencyCycle[] cycles)
        {
            foreach (var cycle in cycles)
            {
                var cyclePath = string.Join(" -> ", cycle.features);
                var closedCycle = $"{cyclePath} -> {cycle.features[0]}";
                var evidenceSummary = cycle.evidence != null && cycle.evidence.Length > 0
                    ? string.Join(", ", Array.ConvertAll(cycle.evidence, evidence => $"{evidence.path}:{evidence.line}"))
                    : "no evidence";

                Debug.LogError(
                    $"[Feature Dependency Rule] Cycle detected: {closedCycle}\n  Evidence: {evidenceSummary}"
                );
            }
        }
    }
}
#endif
