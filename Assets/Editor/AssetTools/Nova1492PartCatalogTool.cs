using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Features.Garage.Infrastructure;
using Features.Unit.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace ProjectSD.EditorTools
{
    public static class Nova1492PartCatalogTool
    {
        private const string CatalogCsvPath = "artifacts/nova1492/nova_part_catalog.csv";
        private const string PrefabRoot = "Assets/Prefabs/Features/Garage/PreviewModels/Generated";
        private const string DataRoot = "Assets/Data/Garage/NovaGenerated";
        private const string VisualCatalogPath = "Assets/Data/Garage/NovaGenerated/NovaPartVisualCatalog.asset";
        private const string ModuleCatalogPath = "Assets/Data/Garage/ModuleCatalog.asset";
        private const string PrefabReportPath = "artifacts/nova1492/nova_part_preview_prefab_report.md";
        private const string AssetReportPath = "artifacts/nova1492/nova_part_playable_asset_report.md";

        [MenuItem("Tools/Nova1492/Create Full Part Preview Prefabs")]
        public static void CreateFullPartPreviewPrefabs()
        {
            AssetDatabase.Refresh();
            var rows = ReadCatalog();
            Directory.CreateDirectory(PrefabRoot);

            var results = new List<PrefabResult>();
            foreach (var row in rows)
            {
                var prefabPath = BuildPreviewPrefabPath(row);
                var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(row.ModelPath);
                if (modelAsset == null)
                {
                    results.Add(PrefabResult.Failed(row, prefabPath, "model asset not loaded"));
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
                var root = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));

                try
                {
                    var instance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
                    if (instance == null)
                    {
                        throw new InvalidOperationException("PrefabUtility.InstantiatePrefab returned null.");
                    }

                    instance.name = "Model";
                    instance.transform.SetParent(root.transform, false);
                    NormalizeModel(instance.transform, row.Slot, out var boundsSize, out var appliedScale);

                    var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    if (prefab == null)
                    {
                        throw new InvalidOperationException("SaveAsPrefabAsset returned null.");
                    }

                    results.Add(PrefabResult.Created(row, prefabPath, boundsSize, appliedScale, CountMissingMaterials(instance)));
                }
                catch (Exception ex)
                {
                    results.Add(PrefabResult.Failed(row, prefabPath, ex.Message));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WritePrefabReport(results);
            Debug.Log($"[Nova1492] Generated part preview prefabs. total={results.Count}, report={PrefabReportPath}");
        }

        [MenuItem("Tools/Nova1492/Generate Playable Part Assets")]
        public static void GeneratePlayablePartAssets()
        {
            AssetDatabase.Refresh();
            var rows = ReadCatalog();
            var moduleCatalog = AssetDatabase.LoadAssetAtPath<ModuleCatalog>(ModuleCatalogPath);
            if (moduleCatalog == null)
            {
                throw new FileNotFoundException("ModuleCatalog asset was not found.", ModuleCatalogPath);
            }

            var results = new List<AssetResult>();
            var visualEntries = new List<VisualEntry>();

            foreach (var row in rows)
            {
                var previewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildPreviewPrefabPath(row));
                if (previewPrefab == null)
                {
                    results.Add(AssetResult.Failed(row, string.Empty, "preview prefab not found"));
                    continue;
                }

                try
                {
                    ScriptableObject partAsset;
                    var assetPath = BuildPartAssetPath(row);
                    Directory.CreateDirectory(Path.GetDirectoryName(assetPath));

                    switch (row.Slot)
                    {
                        case NovaPartSlot.Frame:
                            partAsset = CreateOrUpdateFrame(row, assetPath, previewPrefab);
                            break;
                        case NovaPartSlot.Firepower:
                            partAsset = CreateOrUpdateFirepower(row, assetPath, previewPrefab);
                            break;
                        case NovaPartSlot.Mobility:
                            partAsset = CreateOrUpdateMobility(row, assetPath, previewPrefab);
                            break;
                        default:
                            throw new InvalidOperationException($"Unsupported slot: {row.Slot}");
                    }

                    visualEntries.Add(new VisualEntry(row, previewPrefab, partAsset));
                    results.Add(AssetResult.Created(row, assetPath));
                }
                catch (Exception ex)
                {
                    results.Add(AssetResult.Failed(row, string.Empty, ex.Message));
                }
            }

            AppendToModuleCatalog(moduleCatalog, results);
            CreateOrUpdateVisualCatalog(visualEntries);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WriteAssetReport(results, visualEntries.Count);
            Debug.Log($"[Nova1492] Generated playable part assets. total={results.Count}, visualEntries={visualEntries.Count}, report={AssetReportPath}");
        }

        private static UnitFrameData CreateOrUpdateFrame(CatalogRow row, string assetPath, GameObject previewPrefab)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnitFrameData>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<UnitFrameData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            var so = new SerializedObject(asset);
            so.FindProperty("frameId").stringValue = row.PartId;
            so.FindProperty("displayName").stringValue = row.DisplayName;
            so.FindProperty("baseHp").floatValue = row.BaseHp;
            so.FindProperty("baseMoveRange").floatValue = row.BaseMoveRange;
            so.FindProperty("baseAttackSpeed").floatValue = row.BaseAttackSpeed;
            so.FindProperty("passiveTrait").objectReferenceValue = null;
            so.FindProperty("unitPrefab").objectReferenceValue = null;
            so.FindProperty("previewPrefab").objectReferenceValue = previewPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static FirepowerModuleData CreateOrUpdateFirepower(CatalogRow row, string assetPath, GameObject previewPrefab)
        {
            var asset = AssetDatabase.LoadAssetAtPath<FirepowerModuleData>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<FirepowerModuleData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            var so = new SerializedObject(asset);
            so.FindProperty("moduleId").stringValue = row.PartId;
            so.FindProperty("displayName").stringValue = row.DisplayName;
            so.FindProperty("attackDamage").floatValue = row.AttackDamage;
            so.FindProperty("attackSpeed").floatValue = row.AttackSpeed;
            so.FindProperty("range").floatValue = row.Range;
            so.FindProperty("description").stringValue = BuildGeneratedDescription(row);
            so.FindProperty("previewPrefab").objectReferenceValue = previewPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static MobilityModuleData CreateOrUpdateMobility(CatalogRow row, string assetPath, GameObject previewPrefab)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MobilityModuleData>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<MobilityModuleData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            var so = new SerializedObject(asset);
            so.FindProperty("moduleId").stringValue = row.PartId;
            so.FindProperty("displayName").stringValue = row.DisplayName;
            so.FindProperty("hpBonus").floatValue = row.HpBonus;
            so.FindProperty("moveRange").floatValue = row.MoveRange;
            so.FindProperty("anchorRange").floatValue = row.AnchorRange;
            so.FindProperty("description").stringValue = BuildGeneratedDescription(row);
            so.FindProperty("previewPrefab").objectReferenceValue = previewPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void AppendToModuleCatalog(ModuleCatalog moduleCatalog, IReadOnlyList<AssetResult> results)
        {
            var so = new SerializedObject(moduleCatalog);
            var frameProperty = so.FindProperty("unitFrames");
            var firepowerProperty = so.FindProperty("firepowerModules");
            var mobilityProperty = so.FindProperty("mobilityModules");

            var frameIds = CollectFrameIds(frameProperty);
            var firepowerIds = CollectModuleIds<FirepowerModuleData>(firepowerProperty, data => data.ModuleId);
            var mobilityIds = CollectModuleIds<MobilityModuleData>(mobilityProperty, data => data.ModuleId);

            foreach (var result in results)
            {
                if (!result.Success)
                {
                    continue;
                }

                switch (result.Row.Slot)
                {
                    case NovaPartSlot.Frame:
                        var frame = AssetDatabase.LoadAssetAtPath<UnitFrameData>(result.AssetPath);
                        if (frame != null && frameIds.Add(frame.FrameId))
                        {
                            AppendObjectReference(frameProperty, frame);
                        }
                        break;
                    case NovaPartSlot.Firepower:
                        var firepower = AssetDatabase.LoadAssetAtPath<FirepowerModuleData>(result.AssetPath);
                        if (firepower != null && firepowerIds.Add(firepower.ModuleId))
                        {
                            AppendObjectReference(firepowerProperty, firepower);
                        }
                        break;
                    case NovaPartSlot.Mobility:
                        var mobility = AssetDatabase.LoadAssetAtPath<MobilityModuleData>(result.AssetPath);
                        if (mobility != null && mobilityIds.Add(mobility.ModuleId))
                        {
                            AppendObjectReference(mobilityProperty, mobility);
                        }
                        break;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(moduleCatalog);
        }

        private static HashSet<string> CollectFrameIds(SerializedProperty property)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < property.arraySize; i++)
            {
                if (property.GetArrayElementAtIndex(i).objectReferenceValue is UnitFrameData frame)
                {
                    ids.Add(frame.FrameId);
                }
            }

            return ids;
        }

        private static HashSet<string> CollectModuleIds<T>(SerializedProperty property, Func<T, string> getId)
            where T : UnityEngine.Object
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < property.arraySize; i++)
            {
                if (property.GetArrayElementAtIndex(i).objectReferenceValue is T item)
                {
                    ids.Add(getId(item));
                }
            }

            return ids;
        }

        private static void AppendObjectReference(SerializedProperty property, UnityEngine.Object value)
        {
            var index = property.arraySize;
            property.InsertArrayElementAtIndex(index);
            property.GetArrayElementAtIndex(index).objectReferenceValue = value;
        }

        private static void CreateOrUpdateVisualCatalog(IReadOnlyList<VisualEntry> entries)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(VisualCatalogPath));
            var catalog = AssetDatabase.LoadAssetAtPath<NovaPartVisualCatalog>(VisualCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<NovaPartVisualCatalog>();
                AssetDatabase.CreateAsset(catalog, VisualCatalogPath);
            }

            var so = new SerializedObject(catalog);
            var entriesProperty = so.FindProperty("entries");
            entriesProperty.arraySize = entries.Count;

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var property = entriesProperty.GetArrayElementAtIndex(i);
                property.FindPropertyRelative("partId").stringValue = entry.Row.PartId;
                property.FindPropertyRelative("slot").enumValueIndex = (int)entry.Row.Slot;
                property.FindPropertyRelative("displayName").stringValue = entry.Row.DisplayName;
                property.FindPropertyRelative("sourceRelativePath").stringValue = entry.Row.SourceRelativePath;
                property.FindPropertyRelative("modelPath").stringValue = entry.Row.ModelPath;
                property.FindPropertyRelative("tier").intValue = entry.Row.Tier;
                property.FindPropertyRelative("needsNameReview").boolValue = entry.Row.NeedsNameReview;
                property.FindPropertyRelative("previewPrefab").objectReferenceValue = entry.PreviewPrefab;
                property.FindPropertyRelative("partAsset").objectReferenceValue = entry.PartAsset;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
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

        private static string BuildPartAssetPath(CatalogRow row)
        {
            return $"{DataRoot}/{GetFolderName(row.Slot)}/{row.PartId}.asset";
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

        private static string BuildGeneratedDescription(CatalogRow row)
        {
            return $"Generated Nova1492 baseline. Source: {row.SourceRelativePath}. Tier: {row.Tier}.";
        }

        private static void NormalizeModel(Transform modelRoot, NovaPartSlot slot, out Vector3 boundsSize, out float appliedScale)
        {
            var renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                boundsSize = Vector3.zero;
                appliedScale = 1f;
                return;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            boundsSize = bounds.size;
            var maxDimension = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            var targetMaxDimension = slot switch
            {
                NovaPartSlot.Frame => 0.95f,
                NovaPartSlot.Firepower => 0.82f,
                NovaPartSlot.Mobility => 0.9f,
                _ => 0.9f
            };

            appliedScale = maxDimension > 0.0001f ? targetMaxDimension / maxDimension : 1f;
            modelRoot.localScale *= appliedScale;
            modelRoot.localPosition -= bounds.center * appliedScale;
        }

        private static int CountMissingMaterials(GameObject root)
        {
            var missing = 0;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        missing++;
                    }
                }
            }

            return missing;
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

        private static void WritePrefabReport(IReadOnlyList<PrefabResult> results)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Nova1492 Part Preview Prefab Report");
            builder.AppendLine();
            builder.AppendLine($"> generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine();
            AppendResultSummary(builder, results, r => r.Success, r => r.Row.Slot);
            builder.AppendLine();
            builder.AppendLine("| slot | id | prefab | scale | missing materials | status |");
            builder.AppendLine("|---|---|---|---:|---:|---|");
            foreach (var result in results)
            {
                builder.AppendLine($"| {result.Row.Slot} | `{result.Row.PartId}` | `{result.PrefabPath}` | {result.AppliedScale.ToString("0.######", CultureInfo.InvariantCulture)} | {result.MissingMaterials} | {result.Status} |");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabReportPath));
            File.WriteAllText(PrefabReportPath, builder.ToString(), Encoding.UTF8);
        }

        private static void WriteAssetReport(IReadOnlyList<AssetResult> results, int visualEntryCount)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Nova1492 Playable Part Asset Report");
            builder.AppendLine();
            builder.AppendLine($"> generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine();
            builder.AppendLine($"- visual catalog: `{VisualCatalogPath}`");
            builder.AppendLine($"- visual entries: {visualEntryCount}");
            builder.AppendLine($"- module catalog: `{ModuleCatalogPath}`");
            builder.AppendLine();
            AppendResultSummary(builder, results, r => r.Success, r => r.Row.Slot);
            builder.AppendLine();
            builder.AppendLine("| slot | id | asset | status |");
            builder.AppendLine("|---|---|---|---|");
            foreach (var result in results)
            {
                builder.AppendLine($"| {result.Row.Slot} | `{result.Row.PartId}` | `{result.AssetPath}` | {result.Status} |");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(AssetReportPath));
            File.WriteAllText(AssetReportPath, builder.ToString(), Encoding.UTF8);
        }

        private static void AppendResultSummary<T>(
            StringBuilder builder,
            IReadOnlyList<T> results,
            Func<T, bool> isSuccess,
            Func<T, NovaPartSlot> getSlot)
        {
            builder.AppendLine("## Summary");
            builder.AppendLine();
            builder.AppendLine("| slot | total | success | failed |");
            builder.AppendLine("|---|---:|---:|---:|");

            foreach (NovaPartSlot slot in Enum.GetValues(typeof(NovaPartSlot)))
            {
                var total = 0;
                var success = 0;
                for (var i = 0; i < results.Count; i++)
                {
                    if (getSlot(results[i]) != slot)
                    {
                        continue;
                    }

                    total++;
                    if (isSuccess(results[i]))
                    {
                        success++;
                    }
                }

                builder.AppendLine($"| {slot} | {total} | {success} | {total - success} |");
            }
        }

        private readonly struct CatalogRow
        {
            private CatalogRow(Dictionary<string, string> row)
            {
                PartId = row["partId"];
                Slot = ParseSlot(row["slot"]);
                Category = row["category"];
                SourceRelativePath = row["source_relative_path"];
                ModelPath = row["model_path"];
                Tier = ParseInt(row["tier"]);
                DisplayName = row["displayName"];
                NeedsNameReview = ParseBool(row["needsNameReview"]);
                BaseHp = ParseFloat(row["baseHp"]);
                BaseAttackSpeed = ParseFloat(row["baseAttackSpeed"]);
                BaseMoveRange = ParseFloat(row["baseMoveRange"]);
                AttackDamage = ParseFloat(row["attackDamage"]);
                AttackSpeed = ParseFloat(row["attackSpeed"]);
                Range = ParseFloat(row["range"]);
                HpBonus = ParseFloat(row["hpBonus"]);
                MoveRange = ParseFloat(row["moveRange"]);
                AnchorRange = ParseFloat(row["anchorRange"]);
            }

            public string PartId { get; }
            public NovaPartSlot Slot { get; }
            public string Category { get; }
            public string SourceRelativePath { get; }
            public string ModelPath { get; }
            public int Tier { get; }
            public string DisplayName { get; }
            public bool NeedsNameReview { get; }
            public float BaseHp { get; }
            public float BaseAttackSpeed { get; }
            public float BaseMoveRange { get; }
            public float AttackDamage { get; }
            public float AttackSpeed { get; }
            public float Range { get; }
            public float HpBonus { get; }
            public float MoveRange { get; }
            public float AnchorRange { get; }

            public static CatalogRow From(Dictionary<string, string> row) => new CatalogRow(row);

            private static NovaPartSlot ParseSlot(string value)
            {
                if (Enum.TryParse(value, out NovaPartSlot slot))
                {
                    return slot;
                }

                throw new InvalidOperationException($"Unknown Nova part slot: {value}");
            }

            private static bool ParseBool(string value)
            {
                return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }

            private static int ParseInt(string value)
            {
                return int.Parse(value, CultureInfo.InvariantCulture);
            }

            private static float ParseFloat(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return 0f;
                }

                return float.Parse(value, CultureInfo.InvariantCulture);
            }
        }

        private readonly struct VisualEntry
        {
            public VisualEntry(CatalogRow row, GameObject previewPrefab, ScriptableObject partAsset)
            {
                Row = row;
                PreviewPrefab = previewPrefab;
                PartAsset = partAsset;
            }

            public CatalogRow Row { get; }
            public GameObject PreviewPrefab { get; }
            public ScriptableObject PartAsset { get; }
        }

        private readonly struct PrefabResult
        {
            private PrefabResult(CatalogRow row, string prefabPath, float appliedScale, int missingMaterials, bool success, string status)
            {
                Row = row;
                PrefabPath = prefabPath ?? string.Empty;
                AppliedScale = appliedScale;
                MissingMaterials = missingMaterials;
                Success = success;
                Status = status;
            }

            public CatalogRow Row { get; }
            public string PrefabPath { get; }
            public float AppliedScale { get; }
            public int MissingMaterials { get; }
            public bool Success { get; }
            public string Status { get; }

            public static PrefabResult Created(CatalogRow row, string prefabPath, Vector3 boundsSize, float appliedScale, int missingMaterials)
            {
                return new PrefabResult(row, prefabPath, appliedScale, missingMaterials, true, "created");
            }

            public static PrefabResult Failed(CatalogRow row, string prefabPath, string reason)
            {
                return new PrefabResult(row, prefabPath, 1f, 0, false, $"failed: {reason}");
            }
        }

        private readonly struct AssetResult
        {
            private AssetResult(CatalogRow row, string assetPath, bool success, string status)
            {
                Row = row;
                AssetPath = assetPath ?? string.Empty;
                Success = success;
                Status = status;
            }

            public CatalogRow Row { get; }
            public string AssetPath { get; }
            public bool Success { get; }
            public string Status { get; }

            public static AssetResult Created(CatalogRow row, string assetPath)
            {
                return new AssetResult(row, assetPath, true, "created");
            }

            public static AssetResult Failed(CatalogRow row, string assetPath, string reason)
            {
                return new AssetResult(row, assetPath, false, $"failed: {reason}");
            }
        }
    }
}
