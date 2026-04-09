using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [InitializeOnLoad]
    public static class LayerDependencyValidator
    {
        private static readonly Regex UsingRegex = new Regex(
            @"^\s*using\s+(?:static\s+)?(?:\w+\s*=\s*)?([\w.]+)\s*;"
        );

        private static readonly Regex ForbiddenLayerRegex = new Regex(@"^Features\.\w+\.(\w+)");
        private static readonly Regex FeatureReferenceRegex = new Regex(
            @"\b(Features\.(\w+)(?:\.[A-Za-z_]\w*)+)"
        );
        private const string DependencyReportRelativePath = "Temp/LayerDependencyValidator/feature-dependencies.json";

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
            if (!Directory.Exists(scriptsRoot))
                return;

            var repoRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(repoRoot))
                return;

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

                CollectFeatureDependencies(relativePath, normalizedPath, rawLines, sanitizedLines, features, edgeMap);
            }

            var report = BuildFeatureDependencyReport(features, edgeMap);
            WriteFeatureDependencyReport(repoRoot, report);

            LogLayerViolations(layerViolations);
            LogFeatureCycles(report.cycles);

            if (!silent)
            {
                if (layerViolations.Count == 0)
                    Debug.Log("[Layer Rule] No violations found.");

                if (report.hasCycles)
                {
                    Debug.LogError(
                        $"[Feature Dependency Rule] {report.cycles.Length} cycle(s) found. JSON: {DependencyReportRelativePath}"
                    );
                }
                else
                {
                    Debug.Log(
                        $"[Feature Dependency Rule] Graph is acyclic. Features={report.featureCount}, Edges={report.edgeCount}. JSON: {DependencyReportRelativePath}"
                    );
                }
            }
        }

        private static string DetectLayer(string path)
        {
            if (Contains(path, "/Shared/"))
                return "Shared";
            if (Contains(path, "/Bootstrap/"))
                return "Bootstrap";
            if (Contains(path, "/Infrastructure/"))
                return "Infrastructure";
            if (Contains(path, "/Presentation/"))
                return "Presentation";
            if (Contains(path, "/Application/"))
                return "Application";
            if (Contains(path, "/Domain/"))
                return "Domain";
            return null;
        }

        private static string Check(string ns, string layer)
        {
            // 레이어 방향 규칙은 유지한다.
            // 피처 간 참조 자체는 허용하지만, cycle 검사는 별도 그래프 단계에서 수행한다.
            switch (layer)
            {
                case "Domain":
                    if (ns.StartsWith("UnityEngine"))
                        return "Domain → UnityEngine 금지";
                    if (ns.StartsWith("UnityEditor"))
                        return "Domain → UnityEditor 금지";
                    if (ns.StartsWith("Photon"))
                        return "Domain → Photon 금지";
                    if (ns.StartsWith("System.IO"))
                        return "Domain → System.IO 금지";
                    if (CheckForbiddenLayer(ns, "Application"))
                        return "Domain → Application 참조 금지";
                    if (CheckForbiddenLayer(ns, "Presentation"))
                        return "Domain → Presentation 참조 금지";
                    if (CheckForbiddenLayer(ns, "Infrastructure"))
                        return "Domain → Infrastructure 참조 금지";
                    if (CheckForbiddenLayer(ns, "Bootstrap"))
                        return "Domain → Bootstrap 참조 금지";
                    break;

                case "Application":
                    if (ns.StartsWith("UnityEngine"))
                        return "Application → UnityEngine 금지";
                    if (ns.StartsWith("UnityEditor"))
                        return "Application → UnityEditor 금지";
                    if (ns.StartsWith("Photon"))
                        return "Application → Photon 금지";
                    if (CheckForbiddenLayer(ns, "Presentation"))
                        return "Application → Presentation 참조 금지";
                    if (CheckForbiddenLayer(ns, "Infrastructure"))
                        return "Application → Infrastructure 참조 금지";
                    if (CheckForbiddenLayer(ns, "Bootstrap"))
                        return "Application → Bootstrap 참조 금지";
                    break;

                case "Presentation":
                    if (ns.StartsWith("Photon"))
                        return "Presentation → Photon 금지";
                    if (CheckForbiddenLayer(ns, "Infrastructure"))
                        return "Presentation → Infrastructure 참조 금지";
                    if (CheckForbiddenLayer(ns, "Bootstrap"))
                        return "Presentation → Bootstrap 참조 금지";
                    break;

                case "Infrastructure":
                    if (CheckForbiddenLayer(ns, "Presentation"))
                        return "Infrastructure → Presentation 참조 금지";
                    if (CheckForbiddenLayer(ns, "Bootstrap"))
                        return "Infrastructure → Bootstrap 참조 금지";
                    break;

                case "Bootstrap":
                    break;

                case "Shared":
                    if (ns.StartsWith("Features."))
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
            string[] rawLines,
            string[] sanitizedLines,
            string[] features,
            Dictionary<string, FeatureEdgeAccumulator> edgeMap)
        {
            var currentFeature = DetectFeatureName(normalizedPath);
            if (string.IsNullOrWhiteSpace(currentFeature))
                return;

            for (var i = 0; i < rawLines.Length; i++)
            {
                var usingMatch = UsingRegex.Match(rawLines[i]);
                if (usingMatch.Success)
                {
                    if (TryResolveFeatureEdge(
                        currentFeature,
                        usingMatch.Groups[1].Value,
                        features,
                        out var fromFeature,
                        out var toFeature))
                    {
                        AddEdge(edgeMap, fromFeature, toFeature, relativePath, i + 1);
                    }
                }

                if (i >= sanitizedLines.Length)
                    continue;

                foreach (Match featureMatch in FeatureReferenceRegex.Matches(sanitizedLines[i]))
                {
                    if (!TryResolveFeatureEdge(
                        currentFeature,
                        featureMatch.Groups[1].Value,
                        features,
                        out var fromFeature,
                        out var toFeature))
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
                var prefix = $"Features.{feature}.";
                if (namespaceOrType.StartsWith(prefix, StringComparison.Ordinal))
                    return feature;

                if (string.Equals(namespaceOrType, $"Features.{feature}", StringComparison.Ordinal))
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

            // Consumer-owned Application/Ports references represent the consumer asking the
            // current feature for a capability, so flip the edge to keep the semantic graph.
            if (IsConsumerOwnedPortReference(namespaceOrType, referencedFeature))
            {
                fromFeature = referencedFeature;
                toFeature = currentFeature;
                return true;
            }

            fromFeature = currentFeature;
            toFeature = referencedFeature;
            return true;
        }

        private static bool IsConsumerOwnedPortReference(string namespaceOrType, string feature)
        {
            var prefix = $"Features.{feature}.Application.Ports";
            return string.Equals(namespaceOrType, prefix, StringComparison.Ordinal) ||
                namespaceOrType.StartsWith($"{prefix}.", StringComparison.Ordinal);
        }

        private static void AddEdge(
            Dictionary<string, FeatureEdgeAccumulator> edgeMap,
            string fromFeature,
            string toFeature,
            string relativePath,
            int line)
        {
            var key = GetEdgeKey(fromFeature, toFeature);
            if (!edgeMap.TryGetValue(key, out var edge))
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
                if (!adjacency.TryGetValue(edge.from, out var neighbors))
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

            if (adjacency.TryGetValue(feature, out var neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    if (!state.TryGetValue(neighbor, out var neighborState))
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
            for (var i = 0; i < cyclePath.Count; i++)
            {
                var from = cyclePath[i];
                var to = cyclePath[(i + 1) % cyclePath.Count];
                if (!edgeMap.TryGetValue(GetEdgeKey(from, to), out var edge) || edge.evidence.Count == 0)
                    continue;

                evidence.Add(edge.evidence[0]);
            }

            cycles.Add(signature, new FeatureDependencyCycle
            {
                features = cyclePath.ToArray(),
                evidence = evidence.ToArray()
            });
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

        private static void WriteFeatureDependencyReport(string repoRoot, FeatureDependencyReport report)
        {
            var outputPath = Path.Combine(repoRoot, DependencyReportRelativePath);
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var json = JsonUtility.ToJson(report, prettyPrint: true);
            File.WriteAllText(outputPath, json);
        }

        private static void LogLayerViolations(List<LayerViolation> layerViolations)
        {
            foreach (var violation in layerViolations)
            {
                Debug.LogError(
                    $"[Layer Rule] {violation.path}:{violation.line} — {violation.message}\n  using {violation.usingNamespace};"
                );
            }

            if (layerViolations.Count > 0)
            {
                Debug.LogError($"[Layer Rule] {layerViolations.Count} violation(s) found.");
            }
        }

        private static void LogFeatureCycles(FeatureDependencyCycle[] cycles)
        {
            foreach (var cycle in cycles)
            {
                var cyclePath = string.Join(" -> ", cycle.features);
                var closedCycle = $"{cyclePath} -> {cycle.features[0]}";
                var evidenceSummary = cycle.evidence != null && cycle.evidence.Length > 0
                    ? string.Join(", ", cycle.evidence.Select(evidence => $"{evidence.path}:{evidence.line}"))
                    : "no evidence";

                Debug.LogError(
                    $"[Feature Dependency Rule] Cycle detected: {closedCycle}\n  Evidence: {evidenceSummary}"
                );
            }
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
            return $"{fromFeature}->{toFeature}";
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
            if (!ns.StartsWith("Features."))
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

        [Serializable]
        private sealed class FeatureDependencyReport
        {
            public string generatedAtUtc;
            public int featureCount;
            public int edgeCount;
            public bool hasCycles;
            public FeatureDependencyEdge[] edges;
            public FeatureDependencyCycle[] cycles;
        }

        [Serializable]
        private sealed class FeatureDependencyEdge
        {
            public string from;
            public string to;
            public FeatureDependencyEvidence[] evidence;
        }

        [Serializable]
        private sealed class FeatureDependencyCycle
        {
            public string[] features;
            public FeatureDependencyEvidence[] evidence;
        }

        [Serializable]
        private sealed class FeatureDependencyEvidence
        {
            public string path;
            public int line;
        }

        private sealed class FeatureEdgeAccumulator
        {
            public string from;
            public string to;
            public readonly List<FeatureDependencyEvidence> evidence = new List<FeatureDependencyEvidence>();
            readonly HashSet<string> _evidenceKeys = new HashSet<string>(StringComparer.Ordinal);

            public void AddEvidence(string path, int line)
            {
                var key = $"{path}:{line}";
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

        private sealed class LayerViolation
        {
            public string path;
            public int line;
            public string message;
            public string usingNamespace;
        }
    }
}
