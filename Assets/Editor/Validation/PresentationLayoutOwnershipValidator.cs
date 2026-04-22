#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ProjectSD.EditorTools.Validation
{
    internal static class PresentationLayoutOwnershipValidator
    {
        internal const string ReportRelativePath = "Temp/PresentationLayoutOwnershipValidator/presentation-layout-ownership.json";

        private const string FeaturesScriptsRelativePath = "Assets/Scripts/Features";

        private static readonly Regex NamespaceRegex = new Regex(
            @"^\s*namespace\s+([A-Za-z0-9_.]+)",
            RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex TypeRegex = new Regex(
            @"^\s*(?:public|internal|private|protected|sealed|abstract|static|partial|\s)*(?:class|struct|interface)\s+([A-Za-z_][A-Za-z0-9_]*)",
            RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex PresentationNamespaceRegex = new Regex(
            @"^Features\.[A-Za-z_][A-Za-z0-9_]*\.Presentation(?:\.|$)",
            RegexOptions.Compiled);

        private static readonly Regex GaragePresentationNamespaceRegex = new Regex(
            @"^Features\.Garage\.Presentation(?:\.|$)",
            RegexOptions.Compiled);

        private static readonly DetectionRule[] Rules =
        {
            new(
                "rect-transform-geometry-write",
                new Regex(@"\.\s*(anchoredPosition|sizeDelta|offsetMin|offsetMax|anchorMin|anchorMax|pivot)\s*=", RegexOptions.Compiled)),
            new(
                "layout-component-mutation",
                new Regex(@"\.\s*(minWidth|minHeight|preferredWidth|preferredHeight|flexibleWidth|flexibleHeight|spacing|padding|childAlignment|childControlWidth|childControlHeight|childForceExpandWidth|childForceExpandHeight|constraint|constraintCount|cellSize|startAxis)\s*=", RegexOptions.Compiled)),
            new(
                "layout-helper-wrapper",
                new Regex(@"\b(LayoutElementState|VerticalLayoutGroupState|HorizontalLayoutGroupState)\.Apply\s*\(", RegexOptions.Compiled)),
            new(
                "runtime-layout-repair-set-parent",
                new Regex(@"\.\s*SetParent\s*\(", RegexOptions.Compiled)),
            new(
                "runtime-layout-repair-set-sibling-index",
                new Regex(@"\.\s*SetSiblingIndex\s*\(", RegexOptions.Compiled)),
            new(
                "transform-spatial-write",
                new Regex(@"\.\s*(localScale|localPosition|localEulerAngles|localRotation|position|rotation)\s*=", RegexOptions.Compiled)),
            new(
                "renderer-material-write",
                new Regex(@"\.\s*(material|sharedMaterial)\s*=", RegexOptions.Compiled)),
            new(
                "renderer-material-color-write",
                new Regex(@"\.\s*(material|sharedMaterial)\s*\.\s*color\s*=", RegexOptions.Compiled)),
            new(
                "garage-presentation-typography-write",
                new Regex(@"\.\s*(fontSize|enableAutoSizing|fontSizeMin|fontSizeMax|alignment|textWrappingMode|overflowMode)\s*=", RegexOptions.Compiled),
                GaragePresentationNamespaceRegex),
        };

        [MenuItem("Tools/Validate Presentation Layout Ownership")]
        private static void ValidateFromMenu()
        {
            var report = VerifyProject();
            if (report.ok)
            {
                Debug.Log($"[PresentationLayoutOwnershipValidator] OK. scanned={report.presentationFileCount}, violations=0, report={ReportRelativePath}");
                return;
            }

            Debug.LogError(
                $"[PresentationLayoutOwnershipValidator] FAILED. violations={report.violationCount}, scanned={report.presentationFileCount}, report={ReportRelativePath}");
        }

        internal static PresentationLayoutOwnershipReport VerifyProject()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
            var scriptsRoot = Path.Combine(projectRoot, FeaturesScriptsRelativePath.Replace('/', Path.DirectorySeparatorChar));

            var report = Analyze(projectRoot, scriptsRoot);
            WriteReport(projectRoot, report);
            return report;
        }

        private static PresentationLayoutOwnershipReport Analyze(string projectRoot, string scriptsRoot)
        {
            if (!Directory.Exists(scriptsRoot))
            {
                return BuildReport(
                    scannedFileCount: 0,
                    presentationFileCount: 0,
                    violations: Array.Empty<PresentationLayoutOwnershipViolation>());
            }

            var files = Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var violations = new List<PresentationLayoutOwnershipViolation>();
            var presentationFileCount = 0;

            foreach (var file in files)
            {
                var rawText = File.ReadAllText(file);
                var namespaceMatch = NamespaceRegex.Match(rawText);
                if (!namespaceMatch.Success)
                    continue;

                var namespaceName = namespaceMatch.Groups[1].Value;
                if (!PresentationNamespaceRegex.IsMatch(namespaceName))
                    continue;

                presentationFileCount++;
                var lines = File.ReadAllLines(file);
                var typeName = ResolveTypeName(rawText, file);
                var relativePath = ToRepoRelativePath(projectRoot, file);

                for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                {
                    var line = lines[lineIndex];
                    var trimmed = line.TrimStart();
                    if (IsCommentOnlyLine(trimmed))
                        continue;

                    foreach (var rule in Rules)
                    {
                        if (rule.NamespaceFilter != null && !rule.NamespaceFilter.IsMatch(namespaceName))
                            continue;

                        var match = rule.Pattern.Match(line);
                        if (!match.Success)
                            continue;

                        violations.Add(new PresentationLayoutOwnershipViolation
                        {
                            typeName = typeName,
                            path = relativePath,
                            line = lineIndex + 1,
                            rule = rule.Name,
                            matchedText = match.Value,
                        });
                    }
                }
            }

            return BuildReport(files.Length, presentationFileCount, violations.ToArray());
        }

        private static PresentationLayoutOwnershipReport BuildReport(
            int scannedFileCount,
            int presentationFileCount,
            PresentationLayoutOwnershipViolation[] violations)
        {
            violations ??= Array.Empty<PresentationLayoutOwnershipViolation>();

            return new PresentationLayoutOwnershipReport
            {
                ok = violations.Length == 0,
                generatedAtUtc = DateTime.UtcNow.ToString("o"),
                scannedFileCount = scannedFileCount,
                presentationFileCount = presentationFileCount,
                violationCount = violations.Length,
                violations = violations,
                summary = violations.Length == 0
                    ? "Presentation layout ownership verified."
                    : $"Presentation layout ownership failed with {violations.Length} violation(s).",
            };
        }

        private static void WriteReport(string projectRoot, PresentationLayoutOwnershipReport report)
        {
            var absolutePath = Path.Combine(projectRoot, ReportRelativePath.Replace('/', Path.DirectorySeparatorChar));
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(absolutePath, JsonUtility.ToJson(report, true));
        }

        private static string ResolveTypeName(string rawText, string filePath)
        {
            var typeMatch = TypeRegex.Match(rawText);
            if (typeMatch.Success)
                return typeMatch.Groups[1].Value;

            return Path.GetFileNameWithoutExtension(filePath);
        }

        private static string ToRepoRelativePath(string projectRoot, string filePath)
        {
            var normalizedRoot = projectRoot.Replace('\\', '/').TrimEnd('/');
            var normalizedPath = filePath.Replace('\\', '/');
            if (normalizedPath.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase))
                return normalizedPath.Substring(normalizedRoot.Length + 1);

            return normalizedPath;
        }

        private static bool IsCommentOnlyLine(string trimmedLine)
        {
            if (string.IsNullOrWhiteSpace(trimmedLine))
                return true;

            return trimmedLine.StartsWith("//", StringComparison.Ordinal)
                || trimmedLine.StartsWith("/*", StringComparison.Ordinal)
                || trimmedLine.StartsWith("*", StringComparison.Ordinal)
                || trimmedLine.StartsWith("///", StringComparison.Ordinal);
        }

        private readonly struct DetectionRule
        {
            internal DetectionRule(string name, Regex pattern, Regex namespaceFilter = null)
            {
                Name = name;
                Pattern = pattern;
                NamespaceFilter = namespaceFilter;
            }

            internal string Name { get; }
            internal Regex Pattern { get; }
            internal Regex NamespaceFilter { get; }
        }
    }

    [Serializable]
    internal sealed class PresentationLayoutOwnershipReport
    {
        public bool ok;
        public string generatedAtUtc;
        public int scannedFileCount;
        public int presentationFileCount;
        public int violationCount;
        public PresentationLayoutOwnershipViolation[] violations;
        public string summary;
    }

    [Serializable]
    internal sealed class PresentationLayoutOwnershipViolation
    {
        public string typeName;
        public string path;
        public int line;
        public string rule;
        public string matchedText;
    }
}
#endif
