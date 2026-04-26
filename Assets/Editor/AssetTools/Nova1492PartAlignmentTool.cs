using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Features.Garage.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace ProjectSD.EditorTools
{
    public static class Nova1492PartAlignmentTool
    {
        private const string CatalogCsvPath = "artifacts/nova1492/nova_part_catalog.csv";
        private const string PrefabRoot = "Assets/Prefabs/Features/Garage/PreviewModels/Generated";
        private const string VisualCatalogPath = "Assets/Data/Garage/NovaGenerated/NovaPartVisualCatalog.asset";
        private const string AlignmentCatalogPath = "Assets/Data/Garage/NovaGenerated/NovaPartAlignmentCatalog.asset";
        private const string AlignmentReportPath = "artifacts/nova1492/nova_part_alignment_report.md";
        private const string AlignmentCsvPath = "artifacts/nova1492/nova_part_alignment.csv";

        [MenuItem("Tools/Nova1492/Generate Part Alignment Data")]
        public static void GeneratePartAlignmentData()
        {
            AssetDatabase.Refresh();
            var rows = ReadCatalog();
            var visualCatalog = AssetDatabase.LoadAssetAtPath<NovaPartVisualCatalog>(VisualCatalogPath);
            var visualEntries = BuildVisualEntryMap(visualCatalog);
            var results = new List<AlignmentResult>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var row in rows)
            {
                if (!seenIds.Add(row.PartId))
                {
                    results.Add(AlignmentResult.Duplicate(row));
                    continue;
                }

                var previewPrefab = ResolvePreviewPrefab(row, visualEntries);
                if (previewPrefab == null)
                {
                    results.Add(AlignmentResult.MissingPrefab(row));
                    continue;
                }

                results.Add(Analyze(row, previewPrefab));
            }

            CreateOrUpdateAlignmentCatalog(results);
            WriteAlignmentCsv(results);
            WriteAlignmentReport(results, visualCatalog != null);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Nova1492] Generated part alignment data. total={results.Count}, report={AlignmentReportPath}");
        }

        private static AlignmentResult Analyze(CatalogRow row, GameObject previewPrefab)
        {
            var renderers = previewPrefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return AlignmentResult.MissingRenderer(row);
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var maxDimension = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            var minDimension = Mathf.Min(bounds.size.x, Mathf.Min(bounds.size.y, bounds.size.z));
            var normalizedScale = maxDimension > 0.0001f ? GetTargetMaxDimension(row.Slot) / maxDimension : 1f;
            var pivotOffset = -bounds.center;
            var socketOffset = GetSocketOffset(row.Slot, bounds);
            var socketEuler = GetSocketEuler(row.Slot);
            var quality = ClassifyQuality(bounds.size, normalizedScale, out var reviewReason);

            return AlignmentResult.Analyzed(
                row,
                bounds.size,
                bounds.center,
                normalizedScale,
                pivotOffset,
                socketOffset,
                socketEuler,
                quality,
                reviewReason);
        }

        private static float GetTargetMaxDimension(NovaPartSlot slot)
        {
            return slot switch
            {
                NovaPartSlot.Frame => 0.95f,
                NovaPartSlot.Firepower => 0.82f,
                NovaPartSlot.Mobility => 0.9f,
                _ => 0.9f
            };
        }

        private static Vector3 GetSocketOffset(NovaPartSlot slot, Bounds bounds)
        {
            return slot switch
            {
                NovaPartSlot.Frame => new Vector3(bounds.center.x, bounds.max.y, bounds.center.z),
                NovaPartSlot.Firepower => new Vector3(bounds.center.x, bounds.min.y, bounds.center.z),
                NovaPartSlot.Mobility => new Vector3(bounds.center.x, bounds.max.y, bounds.center.z),
                _ => bounds.center
            };
        }

        private static Vector3 GetSocketEuler(NovaPartSlot slot)
        {
            return slot == NovaPartSlot.Firepower ? new Vector3(0f, 0f, 90f) : Vector3.zero;
        }

        private static string ClassifyQuality(Vector3 boundsSize, float normalizedScale, out string reviewReason)
        {
            var maxDimension = Mathf.Max(boundsSize.x, Mathf.Max(boundsSize.y, boundsSize.z));
            var minDimension = Mathf.Min(boundsSize.x, Mathf.Min(boundsSize.y, boundsSize.z));
            if (maxDimension <= 0.0001f)
            {
                reviewReason = "bounds max dimension is zero";
                return "missing_renderer";
            }

            if (maxDimension < 0.08f)
            {
                reviewReason = $"max dimension {Format(maxDimension)} is below 0.08";
                return "needs_review_tiny";
            }

            var flatRatio = minDimension / maxDimension;
            if (flatRatio < 0.04f)
            {
                reviewReason = $"min/max ratio {Format(flatRatio)} is below 0.04";
                return "needs_review_flat";
            }

            if (normalizedScale < 0.5f || normalizedScale > 2f)
            {
                reviewReason = $"normalized scale {Format(normalizedScale)} is outside 0.5..2.0";
                return "needs_review_large";
            }

            reviewReason = string.Empty;
            return "auto_ok";
        }

        private static GameObject ResolvePreviewPrefab(
            CatalogRow row,
            IReadOnlyDictionary<string, NovaPartVisualCatalog.Entry> visualEntries)
        {
            if (visualEntries != null &&
                visualEntries.TryGetValue(row.PartId, out var visualEntry) &&
                visualEntry?.PreviewPrefab != null)
            {
                return visualEntry.PreviewPrefab;
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(BuildPreviewPrefabPath(row));
        }

        private static Dictionary<string, NovaPartVisualCatalog.Entry> BuildVisualEntryMap(NovaPartVisualCatalog catalog)
        {
            var map = new Dictionary<string, NovaPartVisualCatalog.Entry>(StringComparer.Ordinal);
            if (catalog == null)
            {
                return map;
            }

            for (var i = 0; i < catalog.Entries.Count; i++)
            {
                var entry = catalog.Entries[i];
                if (entry != null && !string.IsNullOrWhiteSpace(entry.PartId))
                {
                    map[entry.PartId] = entry;
                }
            }

            return map;
        }

        private static void CreateOrUpdateAlignmentCatalog(IReadOnlyList<AlignmentResult> results)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AlignmentCatalogPath));
            var catalog = AssetDatabase.LoadAssetAtPath<NovaPartAlignmentCatalog>(AlignmentCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<NovaPartAlignmentCatalog>();
                AssetDatabase.CreateAsset(catalog, AlignmentCatalogPath);
            }

            var so = new SerializedObject(catalog);
            var entriesProperty = so.FindProperty("entries");
            entriesProperty.arraySize = results.Count;

            for (var i = 0; i < results.Count; i++)
            {
                var result = results[i];
                var property = entriesProperty.GetArrayElementAtIndex(i);
                property.FindPropertyRelative("partId").stringValue = result.Row.PartId;
                property.FindPropertyRelative("slot").enumValueIndex = (int)result.Row.Slot;
                property.FindPropertyRelative("boundsSize").vector3Value = result.BoundsSize;
                property.FindPropertyRelative("boundsCenter").vector3Value = result.BoundsCenter;
                property.FindPropertyRelative("normalizedScale").floatValue = result.NormalizedScale;
                property.FindPropertyRelative("pivotOffset").vector3Value = result.PivotOffset;
                property.FindPropertyRelative("socketOffset").vector3Value = result.SocketOffset;
                property.FindPropertyRelative("socketEuler").vector3Value = result.SocketEuler;
                property.FindPropertyRelative("qualityFlag").stringValue = result.QualityFlag;
                property.FindPropertyRelative("reviewReason").stringValue = result.ReviewReason;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void WriteAlignmentCsv(IReadOnlyList<AlignmentResult> results)
        {
            var builder = new StringBuilder();
            builder.AppendLine("partId,slot,boundsSize,boundsCenter,normalizedScale,pivotOffset,socketOffset,socketEuler,qualityFlag,reviewReason");
            foreach (var result in results)
            {
                builder.Append(EscapeCsv(result.Row.PartId)).Append(',');
                builder.Append(result.Row.Slot).Append(',');
                builder.Append(EscapeCsv(FormatVector(result.BoundsSize))).Append(',');
                builder.Append(EscapeCsv(FormatVector(result.BoundsCenter))).Append(',');
                builder.Append(Format(result.NormalizedScale)).Append(',');
                builder.Append(EscapeCsv(FormatVector(result.PivotOffset))).Append(',');
                builder.Append(EscapeCsv(FormatVector(result.SocketOffset))).Append(',');
                builder.Append(EscapeCsv(FormatVector(result.SocketEuler))).Append(',');
                builder.Append(EscapeCsv(result.QualityFlag)).Append(',');
                builder.AppendLine(EscapeCsv(result.ReviewReason));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(AlignmentCsvPath));
            File.WriteAllText(AlignmentCsvPath, builder.ToString(), Encoding.UTF8);
        }

        private static void WriteAlignmentReport(IReadOnlyList<AlignmentResult> results, bool hasVisualCatalog)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Nova1492 Part Alignment Report");
            builder.AppendLine();
            builder.AppendLine($"> generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine();
            builder.AppendLine("## Summary");
            builder.AppendLine();
            builder.AppendLine($"- alignment catalog: `{AlignmentCatalogPath}`");
            builder.AppendLine($"- alignment csv: `{AlignmentCsvPath}`");
            builder.AppendLine($"- visual catalog loaded: {hasVisualCatalog.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()}");
            builder.AppendLine($"- total entries: {results.Count}");
            builder.AppendLine($"- duplicate ids: {CountDuplicateIds(results)}");
            builder.AppendLine($"- missing prefab: {CountQuality(results, "missing_prefab")}");
            builder.AppendLine($"- missing renderer: {CountQuality(results, "missing_renderer")}");
            builder.AppendLine($"- static balance: {BuildStaticBalance(results)}");
            builder.AppendLine();
            AppendSlotSummary(builder, results);
            builder.AppendLine();
            AppendSampleRows(builder, results);
            builder.AppendLine();
            AppendReviewRows(builder, results);

            Directory.CreateDirectory(Path.GetDirectoryName(AlignmentReportPath));
            File.WriteAllText(AlignmentReportPath, builder.ToString(), Encoding.UTF8);
        }

        private static void AppendSlotSummary(StringBuilder builder, IReadOnlyList<AlignmentResult> results)
        {
            builder.AppendLine("## Slot Summary");
            builder.AppendLine();
            builder.AppendLine("| slot | total | auto_ok | needs_review | missing_prefab | missing_renderer |");
            builder.AppendLine("|---|---:|---:|---:|---:|---:|");
            foreach (NovaPartSlot slot in Enum.GetValues(typeof(NovaPartSlot)))
            {
                var total = 0;
                var autoOk = 0;
                var needsReview = 0;
                var missingPrefab = 0;
                var missingRenderer = 0;
                foreach (var result in results)
                {
                    if (result.Row.Slot != slot)
                    {
                        continue;
                    }

                    total++;
                    if (result.QualityFlag == "auto_ok")
                    {
                        autoOk++;
                    }
                    else if (result.QualityFlag == "missing_prefab")
                    {
                        missingPrefab++;
                    }
                    else if (result.QualityFlag == "missing_renderer")
                    {
                        missingRenderer++;
                    }
                    else
                    {
                        needsReview++;
                    }
                }

                builder.AppendLine($"| {slot} | {total} | {autoOk} | {needsReview} | {missingPrefab} | {missingRenderer} |");
            }
        }

        private static void AppendSampleRows(StringBuilder builder, IReadOnlyList<AlignmentResult> results)
        {
            builder.AppendLine("## Fixed Sample Combination");
            builder.AppendLine();
            builder.AppendLine("| slot | requested id | resolved id | quality | socket offset | socket euler |");
            builder.AppendLine("|---|---|---|---|---|---|");
            AppendSampleRow(builder, results, NovaPartSlot.Frame, "nova_frame_body23_ms", "Body23 Ms");
            AppendSampleRow(builder, results, NovaPartSlot.Firepower, "nova_fire_arm43_przso", "Arm43 Przso");
            AppendSampleRow(builder, results, NovaPartSlot.Mobility, "nova_mob_legs24_sts", "Legs24 Sts");
        }

        private static void AppendSampleRow(
            StringBuilder builder,
            IReadOnlyList<AlignmentResult> results,
            NovaPartSlot slot,
            string requestedId,
            string displayName)
        {
            var result = FindById(results, requestedId) ?? FindByDisplayName(results, slot, displayName);
            if (result == null)
            {
                builder.AppendLine($"| {slot} | `{requestedId}` | `sample_missing` | sample_missing | - | - |");
                return;
            }

            var resolved = result.Value;
            builder.AppendLine($"| {slot} | `{requestedId}` | `{resolved.Row.PartId}` | {resolved.QualityFlag} | `{FormatVector(resolved.SocketOffset)}` | `{FormatVector(resolved.SocketEuler)}` |");
        }

        private static void AppendReviewRows(StringBuilder builder, IReadOnlyList<AlignmentResult> results)
        {
            builder.AppendLine("## Review Candidates");
            builder.AppendLine();
            builder.AppendLine("| slot | id | quality | reason | bounds | normalized scale |");
            builder.AppendLine("|---|---|---|---|---|---:|");
            var count = 0;
            foreach (var result in results)
            {
                if (result.QualityFlag == "auto_ok")
                {
                    continue;
                }

                builder.AppendLine($"| {result.Row.Slot} | `{result.Row.PartId}` | {result.QualityFlag} | {EscapeMarkdown(result.ReviewReason)} | `{FormatVector(result.BoundsSize)}` | {Format(result.NormalizedScale)} |");
                count++;
            }

            if (count == 0)
            {
                builder.AppendLine("| - | - | none | - | - | - |");
            }
        }

        private static string BuildStaticBalance(IReadOnlyList<AlignmentResult> results)
        {
            var counted = 0;
            foreach (var result in results)
            {
                if (result.QualityFlag == "auto_ok" ||
                    result.QualityFlag == "needs_review_tiny" ||
                    result.QualityFlag == "needs_review_flat" ||
                    result.QualityFlag == "needs_review_large" ||
                    result.QualityFlag == "missing_renderer" ||
                    result.QualityFlag == "missing_prefab")
                {
                    counted++;
                }
            }

            return counted == results.Count ? "pass" : $"mismatch counted={counted}";
        }

        private static int CountQuality(IReadOnlyList<AlignmentResult> results, string qualityFlag)
        {
            var count = 0;
            foreach (var result in results)
            {
                if (result.QualityFlag == qualityFlag)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountDuplicateIds(IReadOnlyList<AlignmentResult> results)
        {
            var count = 0;
            foreach (var result in results)
            {
                if (string.Equals(result.ReviewReason, "duplicate part id", StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static AlignmentResult? FindById(IReadOnlyList<AlignmentResult> results, string partId)
        {
            foreach (var result in results)
            {
                if (string.Equals(result.Row.PartId, partId, StringComparison.Ordinal))
                {
                    return result;
                }
            }

            return null;
        }

        private static AlignmentResult? FindByDisplayName(IReadOnlyList<AlignmentResult> results, NovaPartSlot slot, string displayName)
        {
            foreach (var result in results)
            {
                if (result.Row.Slot == slot &&
                    string.Equals(result.Row.DisplayName, displayName, StringComparison.Ordinal))
                {
                    return result;
                }
            }

            return null;
        }

        private static List<CatalogRow> ReadCatalog()
        {
            if (!File.Exists(CatalogCsvPath))
            {
                throw new FileNotFoundException("Nova part catalog CSV was not found.", CatalogCsvPath);
            }

            var rows = new List<CatalogRow>();
            foreach (var row in ReadCsv(CatalogCsvPath))
            {
                rows.Add(CatalogRow.From(row));
            }

            if (rows.Count != 321)
            {
                throw new InvalidOperationException($"Expected 321 catalog rows, found {rows.Count}.");
            }

            return rows;
        }

        private static string BuildPreviewPrefabPath(CatalogRow row)
        {
            return $"{PrefabRoot}/{GetFolderName(row.Slot)}/{row.PartId}.prefab";
        }

        private static string GetFolderName(NovaPartSlot slot)
        {
            return slot switch
            {
                NovaPartSlot.Frame => "Frames",
                NovaPartSlot.Firepower => "Firepower",
                NovaPartSlot.Mobility => "Mobility",
                _ => throw new InvalidOperationException($"Unsupported slot: {slot}")
            };
        }

        private static List<Dictionary<string, string>> ReadCsv(string path)
        {
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length == 0)
            {
                return new List<Dictionary<string, string>>();
            }

            var headers = ParseCsvLine(lines[0]);
            var rows = new List<Dictionary<string, string>>();
            for (var i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                var values = ParseCsvLine(lines[i]);
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var column = 0; column < headers.Count; column++)
                {
                    row[headers[column]] = column < values.Count ? values[column] : string.Empty;
                }

                rows.Add(row);
            }

            return rows;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            var builder = new StringBuilder();
            var inQuotes = false;
            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        builder.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(builder.ToString());
                    builder.Clear();
                }
                else
                {
                    builder.Append(c);
                }
            }

            values.Add(builder.ToString());
            return values;
        }

        private static string FormatVector(Vector3 value)
        {
            return $"{Format(value.x)};{Format(value.y)};{Format(value.z)}";
        }

        private static string Format(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string EscapeCsv(string value)
        {
            value ??= string.Empty;
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }

        private static string EscapeMarkdown(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Replace("|", "\\|");
        }

        private readonly struct CatalogRow
        {
            private CatalogRow(Dictionary<string, string> row)
            {
                PartId = row["partId"];
                Slot = ParseSlot(row["slot"]);
                DisplayName = row["displayName"];
            }

            public string PartId { get; }
            public NovaPartSlot Slot { get; }
            public string DisplayName { get; }

            public static CatalogRow From(Dictionary<string, string> row) => new CatalogRow(row);

            private static NovaPartSlot ParseSlot(string value)
            {
                if (Enum.TryParse(value, out NovaPartSlot slot))
                {
                    return slot;
                }

                throw new InvalidOperationException($"Unknown Nova part slot: {value}");
            }
        }

        private readonly struct AlignmentResult
        {
            private AlignmentResult(
                CatalogRow row,
                Vector3 boundsSize,
                Vector3 boundsCenter,
                float normalizedScale,
                Vector3 pivotOffset,
                Vector3 socketOffset,
                Vector3 socketEuler,
                string qualityFlag,
                string reviewReason)
            {
                Row = row;
                BoundsSize = boundsSize;
                BoundsCenter = boundsCenter;
                NormalizedScale = normalizedScale;
                PivotOffset = pivotOffset;
                SocketOffset = socketOffset;
                SocketEuler = socketEuler;
                QualityFlag = qualityFlag;
                ReviewReason = reviewReason ?? string.Empty;
            }

            public CatalogRow Row { get; }
            public Vector3 BoundsSize { get; }
            public Vector3 BoundsCenter { get; }
            public float NormalizedScale { get; }
            public Vector3 PivotOffset { get; }
            public Vector3 SocketOffset { get; }
            public Vector3 SocketEuler { get; }
            public string QualityFlag { get; }
            public string ReviewReason { get; }

            public static AlignmentResult Analyzed(
                CatalogRow row,
                Vector3 boundsSize,
                Vector3 boundsCenter,
                float normalizedScale,
                Vector3 pivotOffset,
                Vector3 socketOffset,
                Vector3 socketEuler,
                string qualityFlag,
                string reviewReason)
            {
                return new AlignmentResult(
                    row,
                    boundsSize,
                    boundsCenter,
                    normalizedScale,
                    pivotOffset,
                    socketOffset,
                    socketEuler,
                    qualityFlag,
                    reviewReason);
            }

            public static AlignmentResult MissingPrefab(CatalogRow row)
            {
                return new AlignmentResult(row, Vector3.zero, Vector3.zero, 1f, Vector3.zero, Vector3.zero, Vector3.zero, "missing_prefab", "preview prefab not found");
            }

            public static AlignmentResult MissingRenderer(CatalogRow row)
            {
                return new AlignmentResult(row, Vector3.zero, Vector3.zero, 1f, Vector3.zero, Vector3.zero, Vector3.zero, "missing_renderer", "preview prefab has no renderer");
            }

            public static AlignmentResult Duplicate(CatalogRow row)
            {
                return new AlignmentResult(row, Vector3.zero, Vector3.zero, 1f, Vector3.zero, Vector3.zero, Vector3.zero, "needs_review_large", "duplicate part id");
            }
        }
    }
}
