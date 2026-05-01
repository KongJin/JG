using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Features.Garage.Infrastructure;
using Features.Unit.Infrastructure;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectSD.EditorTools
{
    public static class Nova1492PlayablePartGenerationTool
    {
        private const string CatalogCsvPath = "artifacts/nova1492/nova_part_catalog.csv";
        private const string PipelineStatePath = "artifacts/nova1492/gx_pipeline_state.csv";
        private const string PreviewRootPath = "Assets/Prefabs/Features/Garage/PreviewModels/Generated";
        private const string AssemblyRootPath = "Assets/Prefabs/Features/Garage/AssemblyModels/Generated";
        private const string DataRootPath = "Assets/Data/Garage/NovaGenerated";
        private const string ModuleCatalogPath = "Assets/Data/Garage/ModuleCatalog.asset";
        private const string VisualCatalogPath = "Assets/Data/Garage/NovaGenerated/NovaPartVisualCatalog.asset";
        private const string AlignmentCatalogPath = "Assets/Data/Garage/NovaGenerated/NovaPartAlignmentCatalog.asset";
        private const string PreviewReportPath = "artifacts/nova1492/nova_part_preview_prefab_report.md";
        private const string AssemblyReportPath = "artifacts/nova1492/nova_part_assembly_prefab_report.md";
        private const string PlayableReportPath = "artifacts/nova1492/nova_part_playable_asset_report.md";
        private const string RoadRunnerPartId = "nova_mob_legs1_rdrn";
        private const float PreviewNormalizedMaxDimension = 0.9f;
        private const float AssemblyModelScale = 0.3353419f;

        [MenuItem("Tools/Nova1492/Create Full Part Preview Prefabs")]
        public static void CreateFullPartPreviewPrefabs()
        {
            AssetDatabase.Refresh();
            var rows = ReadCatalogRows();
            EnsureAssetFolder(PreviewRootPath);
            var deleted = DeleteStaleGeneratedAssets(rows, PreviewRootPath, ".prefab", GetPreviewPrefabPath);
            CreatePreviewPrefabs(rows, out var created, out var updated, out var missingModels, out var scaleByPartId);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WritePreviewReport(rows, created, updated, missingModels, scaleByPartId);
            Debug.Log("[Nova1492] Preview prefab generation complete. rows=" + rows.Count + " deletedStale=" + deleted + " report=" + PreviewReportPath);
        }

        [MenuItem("Tools/Nova1492/Create Full Part Assembly Prefabs")]
        public static void CreateFullPartAssemblyPrefabs()
        {
            AssetDatabase.Refresh();
            var rows = ReadCatalogRows();
            EnsureAssetFolder(AssemblyRootPath);
            var deleted = DeleteStaleGeneratedAssets(rows, AssemblyRootPath, ".prefab", GetAssemblyPrefabPath);
            CreateAssemblyPrefabs(rows, out var created, out var updated, out var missingModels, out var scaleByPartId);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WriteAssemblyReport(rows, created, updated, missingModels, scaleByPartId);
            Debug.Log("[Nova1492] Assembly prefab generation complete. rows=" + rows.Count + " deletedStale=" + deleted + " report=" + AssemblyReportPath);
        }

        [MenuItem("Tools/Nova1492/Create Changed Part Prefabs From Pipeline")]
        public static void CreateChangedPartPrefabsFromPipeline()
        {
            AssetDatabase.Refresh();
            var changedPartIds = ReadChangedPipelinePartIds();
            if (changedPartIds.Count == 0)
            {
                Debug.Log("[Nova1492] No changed pipeline rows found in " + PipelineStatePath);
                return;
            }

            var rows = ReadCatalogRows().FindAll(row => changedPartIds.Contains(row.PartId));
            EnsureAssetFolder(PreviewRootPath);
            EnsureAssetFolder(AssemblyRootPath);
            CreatePreviewPrefabs(rows, out var previewCreated, out var previewUpdated, out var previewMissingModels, out var previewScaleByPartId);
            CreateAssemblyPrefabs(rows, out var assemblyCreated, out var assemblyUpdated, out var assemblyMissingModels, out var assemblyScaleByPartId);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WritePreviewReport(rows, previewCreated, previewUpdated, previewMissingModels, previewScaleByPartId);
            WriteAssemblyReport(rows, assemblyCreated, assemblyUpdated, assemblyMissingModels, assemblyScaleByPartId);
            Debug.Log("[Nova1492] Changed prefab generation complete. rows=" + rows.Count +
                      " previewUpdated=" + previewUpdated +
                      " assemblyUpdated=" + assemblyUpdated);
        }

        [MenuItem("Tools/Nova1492/Create Road Runner Preview Prefab")]
        public static void CreateRoadRunnerPreviewPrefab()
        {
            AssetDatabase.Refresh();
            var rows = ReadCatalogRows().FindAll(row => row.PartId == RoadRunnerPartId);
            if (rows.Count != 1)
            {
                throw new InvalidOperationException("Expected exactly one Road Runner catalog row, found " + rows.Count);
            }

            EnsureAssetFolder(PreviewRootPath);
            CreatePreviewPrefabs(rows, out var created, out var updated, out var missingModels, out var scaleByPartId);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WritePreviewReport(rows, created, updated, missingModels, scaleByPartId);
            Debug.Log("[Nova1492] Road Runner preview prefab generation complete. created=" + created + " updated=" + updated + " report=" + PreviewReportPath);
        }

        private static void CreatePreviewPrefabs(
            IReadOnlyList<PartRow> rows,
            out int created,
            out int updated,
            out List<string> missingModels,
            out Dictionary<string, float> scaleByPartId)
        {
            created = 0;
            updated = 0;
            missingModels = new List<string>();
            scaleByPartId = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                EnsureAssetFolder(GetPreviewFolder(row.Slot));
                var prefabPath = GetPreviewPrefabPath(row);
                var hadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(row.ModelPath);
                var root = new GameObject(row.PartId);
                if (model == null)
                {
                    missingModels.Add(row.PartId + " -> " + row.ModelPath);
                }
                else
                {
                    var child = PrefabUtility.InstantiatePrefab(model) as GameObject;
                    if (child != null)
                    {
                        child.name = model.name;
                        child.transform.SetParent(root.transform, false);
                        scaleByPartId[row.PartId] = NormalizePreviewChild(root, child);
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Object.DestroyImmediate(root);
                if (hadPrefab)
                {
                    updated++;
                }
                else
                {
                    created++;
                }
            }
        }

        private static void CreateAssemblyPrefabs(
            IReadOnlyList<PartRow> rows,
            out int created,
            out int updated,
            out List<string> missingModels,
            out Dictionary<string, float> scaleByPartId)
        {
            created = 0;
            updated = 0;
            missingModels = new List<string>();
            scaleByPartId = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                EnsureAssetFolder(GetAssemblyFolder(row.Slot));
                var prefabPath = GetAssemblyPrefabPath(row);
                var hadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(row.ModelPath);
                var root = new GameObject(row.PartId);
                if (model == null)
                {
                    missingModels.Add(row.PartId + " -> " + row.ModelPath);
                }
                else
                {
                    var child = PrefabUtility.InstantiatePrefab(model) as GameObject;
                    if (child != null)
                    {
                        child.name = model.name;
                        child.transform.SetParent(root.transform, false);
                        scaleByPartId[row.PartId] = NormalizeAssemblyChild(root, child);
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Object.DestroyImmediate(root);
                if (hadPrefab)
                {
                    updated++;
                }
                else
                {
                    created++;
                }
            }
        }

        [MenuItem("Tools/Nova1492/Generate Playable Part Assets")]
        public static void GeneratePlayablePartAssets()
        {
            AssetDatabase.Refresh();
            var rows = ReadCatalogRows();
            EnsureAssetFolder(DataRootPath);
            EnsureAssetFolder(DataRootPath + "/Frames");
            EnsureAssetFolder(DataRootPath + "/Firepower");
            EnsureAssetFolder(DataRootPath + "/Mobility");
            var deleted = DeleteStaleGeneratedAssets(rows, DataRootPath + "/Frames", ".asset", GetPartAssetPath) +
                          DeleteStaleGeneratedAssets(rows, DataRootPath + "/Firepower", ".asset", GetPartAssetPath) +
                          DeleteStaleGeneratedAssets(rows, DataRootPath + "/Mobility", ".asset", GetPartAssetPath);

            var frames = new List<UnitFrameData>();
            var firepower = new List<FirepowerModuleData>();
            var mobility = new List<MobilityModuleData>();
            var missingPrefabs = new List<string>();

            foreach (var row in rows)
            {
                var previewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GetPreviewPrefabPath(row));
                if (previewPrefab == null)
                {
                    missingPrefabs.Add(row.PartId);
                }

                if (row.Slot == "Frame")
                {
                    frames.Add(CreateOrUpdateFrame(row, previewPrefab));
                }
                else if (row.Slot == "Firepower")
                {
                    firepower.Add(CreateOrUpdateFirepower(row, previewPrefab));
                }
                else if (row.Slot == "Mobility")
                {
                    mobility.Add(CreateOrUpdateMobility(row, previewPrefab));
                }
                else
                {
                    throw new InvalidOperationException("Unknown Nova part slot: " + row.Slot);
                }
            }

            UpdateModuleCatalog(frames, firepower, mobility);
            UpdateVisualCatalog(rows);
            FilterAlignmentCatalog(rows);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WritePlayableReport(rows, frames.Count, firepower.Count, mobility.Count, missingPrefabs);
            Debug.Log("[Nova1492] Playable part asset generation complete. rows=" + rows.Count + " deletedStale=" + deleted + " report=" + PlayableReportPath);
        }

        private static UnitFrameData CreateOrUpdateFrame(PartRow row, GameObject previewPrefab)
        {
            var asset = LoadOrCreate<UnitFrameData>(GetPartAssetPath(row));
            var serialized = new SerializedObject(asset);
            SetString(serialized, "frameId", row.PartId);
            SetString(serialized, "displayName", row.DisplayName);
            SetEnum(serialized, "assemblyForm", AssemblyFormToIndex(row.AssemblyForm));
            SetFloat(serialized, "baseHp", row.BaseHp, 120f);
            SetFloat(serialized, "baseMoveRange", row.BaseMoveRange, 3f);
            SetFloat(serialized, "baseAttackSpeed", row.BaseAttackSpeed, 1f);
            SetObject(serialized, "previewPrefab", previewPrefab);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static FirepowerModuleData CreateOrUpdateFirepower(PartRow row, GameObject previewPrefab)
        {
            var asset = LoadOrCreate<FirepowerModuleData>(GetPartAssetPath(row));
            var serialized = new SerializedObject(asset);
            SetString(serialized, "moduleId", row.PartId);
            SetString(serialized, "displayName", row.DisplayName);
            SetEnum(serialized, "assemblyForm", AssemblyFormToIndex(row.AssemblyForm));
            SetFloat(serialized, "attackDamage", row.AttackDamage, 24f);
            SetFloat(serialized, "attackSpeed", row.AttackSpeed, 1f);
            SetFloat(serialized, "range", row.Range, 4.5f);
            SetString(serialized, "description", row.SourceRelativePath);
            SetObject(serialized, "previewPrefab", previewPrefab);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static MobilityModuleData CreateOrUpdateMobility(PartRow row, GameObject previewPrefab)
        {
            var asset = LoadOrCreate<MobilityModuleData>(GetPartAssetPath(row));
            var serialized = new SerializedObject(asset);
            SetString(serialized, "moduleId", row.PartId);
            SetString(serialized, "displayName", row.DisplayName);
            SetEnum(serialized, "mobilitySurface", MobilitySurfaceToIndex(row.MobilitySurface));
            SetFloat(serialized, "hpBonus", row.HpBonus, 25f);
            SetFloat(serialized, "moveRange", row.MoveRange, 3f);
            SetFloat(serialized, "anchorRange", row.AnchorRange, 4f);
            SetString(serialized, "description", row.SourceRelativePath);
            SetObject(serialized, "previewPrefab", previewPrefab);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void UpdateModuleCatalog(
            IReadOnlyList<UnitFrameData> frames,
            IReadOnlyList<FirepowerModuleData> firepower,
            IReadOnlyList<MobilityModuleData> mobility)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ModuleCatalog>(ModuleCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ModuleCatalog>();
                EnsureAssetFolder(Path.GetDirectoryName(ModuleCatalogPath).Replace('\\', '/'));
                AssetDatabase.CreateAsset(catalog, ModuleCatalogPath);
            }

            var serialized = new SerializedObject(catalog);
            ReplaceReferences(
                serialized.FindProperty("unitFrames"),
                ToObjectList(frames));
            ReplaceReferences(
                serialized.FindProperty("firepowerModules"),
                ToObjectList(firepower));
            ReplaceReferences(
                serialized.FindProperty("mobilityModules"),
                ToObjectList(mobility));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void UpdateVisualCatalog(IReadOnlyList<PartRow> rows)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<NovaPartVisualCatalog>(VisualCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<NovaPartVisualCatalog>();
                EnsureAssetFolder(Path.GetDirectoryName(VisualCatalogPath).Replace('\\', '/'));
                AssetDatabase.CreateAsset(catalog, VisualCatalogPath);
            }

            var serialized = new SerializedObject(catalog);
            var entries = serialized.FindProperty("entries");
            entries.arraySize = rows.Count;
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var entry = entries.GetArrayElementAtIndex(i);
                SetString(entry, "partId", row.PartId);
                SetEnum(entry, "slot", SlotToIndex(row.Slot));
                SetString(entry, "displayName", row.DisplayName);
                SetString(entry, "sourceRelativePath", row.SourceRelativePath);
                SetString(entry, "modelPath", row.ModelPath);
                SetInt(entry, "tier", row.Tier);
                SetBool(entry, "needsNameReview", row.NeedsNameReview);
                SetObject(entry, "previewPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(GetPreviewPrefabPath(row)));
                SetObject(entry, "assemblyPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(GetAssemblyPrefabPath(row)));
                SetObject(entry, "partAsset", AssetDatabase.LoadAssetAtPath<ScriptableObject>(GetPartAssetPath(row)));
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void FilterAlignmentCatalog(IReadOnlyList<PartRow> rows)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<NovaPartAlignmentCatalog>(AlignmentCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<NovaPartAlignmentCatalog>();
                EnsureAssetFolder(Path.GetDirectoryName(AlignmentCatalogPath).Replace('\\', '/'));
                AssetDatabase.CreateAsset(catalog, AlignmentCatalogPath);
            }

            var serialized = new SerializedObject(catalog);
            var entries = serialized.FindProperty("entries");
            var snapshots = new Dictionary<string, AlignmentSnapshot>(StringComparer.Ordinal);
            for (var i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var partIdProperty = entry.FindPropertyRelative("partId");
                if (partIdProperty != null && !string.IsNullOrWhiteSpace(partIdProperty.stringValue))
                {
                    snapshots[partIdProperty.stringValue] = AlignmentSnapshot.From(entry);
                }
            }

            entries.arraySize = rows.Count;
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var entry = entries.GetArrayElementAtIndex(i);
                AlignmentSnapshot snapshot;
                if (!snapshots.TryGetValue(row.PartId, out snapshot))
                {
                    snapshot = AlignmentSnapshot.CreateDefault(row);
                }

                snapshot.Slot = SlotToIndex(row.Slot);
                snapshot.Apply(entry);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void ReplaceReferences(
            SerializedProperty property,
            IReadOnlyList<Object> generatedAssets)
        {
            property.arraySize = generatedAssets.Count;
            for (var i = 0; i < generatedAssets.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = generatedAssets[i];
            }
        }

        private static List<Object> ToObjectList<T>(IReadOnlyList<T> values) where T : Object
        {
            var results = new List<Object>(values.Count);
            for (var i = 0; i < values.Count; i++)
            {
                results.Add(values[i]);
            }

            return results;
        }

        private static string GetFrameId(Object value)
        {
            var frame = value as UnitFrameData;
            return frame == null ? null : frame.FrameId;
        }

        private static string GetModuleId(Object value)
        {
            var firepower = value as FirepowerModuleData;
            if (firepower != null)
            {
                return firepower.ModuleId;
            }

            var mobility = value as MobilityModuleData;
            return mobility == null ? null : mobility.ModuleId;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            EnsureAssetFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static int DeleteStaleGeneratedAssets(
            IReadOnlyList<PartRow> rows,
            string folder,
            string extension,
            Func<PartRow, string> getExpectedPath)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return 0;
            }

            var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < rows.Count; i++)
            {
                expected.Add(getExpectedPath(rows[i]));
            }

            var deleted = 0;
            var guids = AssetDatabase.FindAssets(string.Empty, new[] { folder });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(path) ||
                    !path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase) ||
                    !path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ||
                    expected.Contains(path))
                {
                    continue;
                }

                if (AssetDatabase.DeleteAsset(path))
                {
                    deleted++;
                }
            }

            return deleted;
        }

        private static List<PartRow> ReadCatalogRows()
        {
            if (!File.Exists(CatalogCsvPath))
            {
                throw new FileNotFoundException("Nova1492 part catalog CSV was not found.", CatalogCsvPath);
            }

            var lines = File.ReadAllLines(CatalogCsvPath, Encoding.UTF8);
            if (lines.Length < 2)
            {
                return new List<PartRow>();
            }

            var headers = ParseCsvLine(lines[0]);
            var headerIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < headers.Count; i++)
            {
                headerIndex[headers[i]] = i;
            }

            var rows = new List<PartRow>();
            for (var i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                var values = ParseCsvLine(lines[i]);
                var row = new PartRow
                {
                    PartId = Get(values, headerIndex, "partId"),
                    Slot = Get(values, headerIndex, "slot"),
                    Category = Get(values, headerIndex, "category"),
                    SourceRelativePath = Get(values, headerIndex, "source_relative_path"),
                    OriginalCode = Get(values, headerIndex, "originalCode"),
                    OriginalName = Get(values, headerIndex, "originalName"),
                    ModelPath = Get(values, headerIndex, "model_path"),
                    Tier = ParseInt(Get(values, headerIndex, "tier"), 1),
                    DisplayName = Get(values, headerIndex, "displayName"),
                    NeedsNameReview = ParseBool(Get(values, headerIndex, "needsNameReview")),
                    AssemblyForm = Get(values, headerIndex, "assemblyForm"),
                    MobilitySurface = Get(values, headerIndex, "mobilitySurface"),
                    BaseHp = ParseFloat(Get(values, headerIndex, "baseHp"), 0f),
                    BaseAttackSpeed = ParseFloat(Get(values, headerIndex, "baseAttackSpeed"), 0f),
                    BaseMoveRange = ParseFloat(Get(values, headerIndex, "baseMoveRange"), 0f),
                    AttackDamage = ParseFloat(Get(values, headerIndex, "attackDamage"), 0f),
                    AttackSpeed = ParseFloat(Get(values, headerIndex, "attackSpeed"), 0f),
                    Range = ParseFloat(Get(values, headerIndex, "range"), 0f),
                    HpBonus = ParseFloat(Get(values, headerIndex, "hpBonus"), 0f),
                    MoveRange = ParseFloat(Get(values, headerIndex, "moveRange"), 0f),
                    AnchorRange = ParseFloat(Get(values, headerIndex, "anchorRange"), 0f)
                };
                rows.Add(row);
            }

            return rows;
        }

        private static HashSet<string> ReadChangedPipelinePartIds()
        {
            var partIds = new HashSet<string>(StringComparer.Ordinal);
            if (!File.Exists(PipelineStatePath))
            {
                return partIds;
            }

            var lines = File.ReadAllLines(PipelineStatePath, Encoding.UTF8);
            if (lines.Length < 2)
            {
                return partIds;
            }

            var headers = ParseCsvLine(lines[0]);
            var headerIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < headers.Count; i++)
            {
                headerIndex[headers[i]] = i;
            }

            for (var i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                var values = ParseCsvLine(lines[i]);
                var status = Get(values, headerIndex, "status");
                if (status == "skipped" || status == "failed" || status == "analyzed")
                {
                    continue;
                }

                var partId = Get(values, headerIndex, "part_id");
                if (!string.IsNullOrWhiteSpace(partId))
                {
                    partIds.Add(partId);
                }
            }

            return partIds;
        }

        private static float NormalizePreviewChild(GameObject root, GameObject child)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return 1f;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var maxDimension = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (maxDimension <= 0.0001f)
            {
                return 1f;
            }

            var scale = PreviewNormalizedMaxDimension / maxDimension;
            child.transform.localScale = Vector3.one * scale;
            child.transform.localPosition = -bounds.center * scale;
            return scale;
        }

        private static float NormalizeAssemblyChild(GameObject root, GameObject child)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return 1f;
            }

            child.transform.localScale = Vector3.one * AssemblyModelScale;
            child.transform.localPosition = Vector3.zero;
            return AssemblyModelScale;
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
                    builder.Length = 0;
                }
                else
                {
                    builder.Append(c);
                }
            }

            values.Add(builder.ToString());
            return values;
        }

        private static string Get(IReadOnlyList<string> values, IReadOnlyDictionary<string, int> headerIndex, string header)
        {
            int index;
            if (!headerIndex.TryGetValue(header, out index) || index < 0 || index >= values.Count)
            {
                return string.Empty;
            }

            return values[index];
        }

        private static string GetPreviewFolder(string slot)
        {
            return PreviewRootPath + "/" + slot;
        }

        private static string GetPreviewPrefabPath(PartRow row)
        {
            return GetPreviewFolder(row.Slot) + "/" + row.PartId + ".prefab";
        }

        private static string GetAssemblyFolder(string slot)
        {
            return AssemblyRootPath + "/" + slot;
        }

        private static string GetAssemblyPrefabPath(PartRow row)
        {
            return GetAssemblyFolder(row.Slot) + "/" + row.PartId + ".prefab";
        }

        private static string GetPartAssetPath(PartRow row)
        {
            if (row.Slot == "Frame")
            {
                return DataRootPath + "/Frames/" + row.PartId + ".asset";
            }

            if (row.Slot == "Firepower")
            {
                return DataRootPath + "/Firepower/" + row.PartId + ".asset";
            }

            return DataRootPath + "/Mobility/" + row.PartId + ".asset";
        }

        private static void EnsureAssetFolder(string path)
        {
            var normalized = path.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            var parts = normalized.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void SetString(SerializedObject serialized, string name, string value)
        {
            var property = serialized.FindProperty(name);
            if (property != null)
            {
                property.stringValue = value ?? string.Empty;
            }
        }

        private static void SetFloat(SerializedObject serialized, string name, float value, float fallback)
        {
            var property = serialized.FindProperty(name);
            if (property != null)
            {
                property.floatValue = value > 0f ? value : fallback;
            }
        }

        private static void SetEnum(SerializedObject serialized, string name, int value)
        {
            var property = serialized.FindProperty(name);
            if (property != null)
            {
                property.enumValueIndex = value;
            }
        }

        private static void SetObject(SerializedObject serialized, string name, Object value)
        {
            var property = serialized.FindProperty(name);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetString(SerializedProperty parent, string name, string value)
        {
            var property = parent.FindPropertyRelative(name);
            if (property != null)
            {
                property.stringValue = value ?? string.Empty;
            }
        }

        private static void SetEnum(SerializedProperty parent, string name, int value)
        {
            var property = parent.FindPropertyRelative(name);
            if (property != null)
            {
                property.enumValueIndex = value;
            }
        }

        private static void SetInt(SerializedProperty parent, string name, int value)
        {
            var property = parent.FindPropertyRelative(name);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetBool(SerializedProperty parent, string name, bool value)
        {
            var property = parent.FindPropertyRelative(name);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetObject(SerializedProperty parent, string name, Object value)
        {
            var property = parent.FindPropertyRelative(name);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static int SlotToIndex(string slot)
        {
            if (slot == "Frame")
            {
                return 0;
            }

            if (slot == "Firepower")
            {
                return 1;
            }

            if (slot == "Mobility")
            {
                return 2;
            }

            throw new InvalidOperationException("Unknown Nova part slot: " + slot);
        }

        private static int AssemblyFormToIndex(string value)
        {
            return Enum.TryParse<AssemblyForm>(value, ignoreCase: true, out var parsed)
                ? (int)parsed
                : (int)AssemblyForm.Unspecified;
        }

        private static int MobilitySurfaceToIndex(string value)
        {
            return Enum.TryParse<MobilitySurface>(value, ignoreCase: true, out var parsed)
                ? (int)parsed
                : (int)MobilitySurface.Unspecified;
        }

        private static int ParseInt(string value, int fallback)
        {
            int parsed;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) ? parsed : fallback;
        }

        private static float ParseFloat(string value, float fallback)
        {
            float parsed;
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) ? parsed : fallback;
        }

        private static bool ParseBool(string value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static void WritePreviewReport(
            IReadOnlyList<PartRow> rows,
            int created,
            int updated,
            IReadOnlyList<string> missingModels,
            IReadOnlyDictionary<string, float> scaleByPartId)
        {
            var builder = NewReportBuilder("# Nova1492 Part Preview Prefab Report");
            builder.AppendLine($"- catalog rows: {rows.Count}");
            builder.AppendLine($"- created prefabs: {created}");
            builder.AppendLine($"- updated prefabs: {updated}");
            builder.AppendLine($"- missing source models: {missingModels.Count}");
            AppendCounts(builder, rows);
            AppendScaleTable(builder, rows, scaleByPartId, "Preview Scale", GetPreviewPrefabPath);
            AppendList(builder, "Missing Source Models", missingModels);
            WriteReport(PreviewReportPath, builder);
        }

        private static void WriteAssemblyReport(
            IReadOnlyList<PartRow> rows,
            int created,
            int updated,
            IReadOnlyList<string> missingModels,
            IReadOnlyDictionary<string, float> scaleByPartId)
        {
            var builder = NewReportBuilder("# Nova1492 Part Assembly Prefab Report");
            builder.AppendLine($"- catalog rows: {rows.Count}");
            builder.AppendLine($"- created prefabs: {created}");
            builder.AppendLine($"- updated prefabs: {updated}");
            builder.AppendLine($"- missing source models: {missingModels.Count}");
            AppendCounts(builder, rows);
            AppendScaleTable(builder, rows, scaleByPartId, "Assembly Scale", GetAssemblyPrefabPath);
            AppendList(builder, "Missing Source Models", missingModels);
            WriteReport(AssemblyReportPath, builder);
        }

        private static void WritePlayableReport(
            IReadOnlyList<PartRow> rows,
            int frameCount,
            int firepowerCount,
            int mobilityCount,
            IReadOnlyList<string> missingPrefabs)
        {
            var builder = NewReportBuilder("# Nova1492 Playable Part Asset Report");
            builder.AppendLine($"- module catalog: `{ModuleCatalogPath}`");
            builder.AppendLine($"- visual catalog: `{VisualCatalogPath}`");
            builder.AppendLine($"- alignment catalog: `{AlignmentCatalogPath}`");
            builder.AppendLine($"- visual entries: {rows.Count}");
            builder.AppendLine($"- generated counts: Frame {frameCount}, Firepower {firepowerCount}, Mobility {mobilityCount}");
            builder.AppendLine($"- missing preview prefabs: {missingPrefabs.Count}");
            AppendCounts(builder, rows);
            AppendList(builder, "Missing Preview Prefabs", missingPrefabs);
            WriteReport(PlayableReportPath, builder);
        }

        private static StringBuilder NewReportBuilder(string title)
        {
            var builder = new StringBuilder();
            builder.AppendLine(title);
            builder.AppendLine();
            builder.AppendLine($"> generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine();
            builder.AppendLine("## Summary");
            builder.AppendLine();
            return builder;
        }

        private static void AppendCounts(StringBuilder builder, IReadOnlyList<PartRow> rows)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < rows.Count; i++)
            {
                var slot = rows[i].Slot;
                counts[slot] = counts.ContainsKey(slot) ? counts[slot] + 1 : 1;
            }

            builder.AppendLine();
            builder.AppendLine("## Slot Counts");
            builder.AppendLine();
            foreach (var pair in counts)
            {
                builder.AppendLine($"- {pair.Key}: {pair.Value}");
            }
        }

        private static void AppendList(StringBuilder builder, string title, IReadOnlyList<string> values)
        {
            builder.AppendLine();
            builder.AppendLine("## " + title);
            builder.AppendLine();
            if (values.Count == 0)
            {
                builder.AppendLine("- none");
                return;
            }

            for (var i = 0; i < values.Count; i++)
            {
                builder.AppendLine("- " + values[i]);
            }
        }

        private static void AppendScaleTable(
            StringBuilder builder,
            IReadOnlyList<PartRow> rows,
            IReadOnlyDictionary<string, float> scaleByPartId,
            string title,
            Func<PartRow, string> getPrefabPath)
        {
            builder.AppendLine();
            builder.AppendLine("## " + title);
            builder.AppendLine();
            builder.AppendLine("| slot | id | prefab | scale |");
            builder.AppendLine("|---|---|---|---:|");
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                float scale;
                if (!scaleByPartId.TryGetValue(row.PartId, out scale))
                {
                    scale = 1f;
                }

                builder.AppendLine(
                    $"| {row.Slot} | `{row.PartId}` | `{getPrefabPath(row)}` | {scale.ToString("0.######", CultureInfo.InvariantCulture)} |");
            }
        }

        private static void WriteReport(string path, StringBuilder builder)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
        }

        private sealed class PartRow
        {
            public string PartId;
            public string Slot;
            public string Category;
            public string SourceRelativePath;
            public string OriginalCode;
            public string OriginalName;
            public string ModelPath;
            public int Tier;
            public string DisplayName;
            public bool NeedsNameReview;
            public string AssemblyForm;
            public string MobilitySurface;
            public float BaseHp;
            public float BaseAttackSpeed;
            public float BaseMoveRange;
            public float AttackDamage;
            public float AttackSpeed;
            public float Range;
            public float HpBonus;
            public float MoveRange;
            public float AnchorRange;
        }

        private sealed class AlignmentSnapshot
        {
            public string PartId;
            public int Slot;
            public float NormalizedScale;
            public Vector3 PivotOffset;
            public Vector3 SocketOffset;
            public Vector3 SocketEuler;
            public bool HasGxTreeSocket;
            public Vector3 GxTreeSocketOffset;
            public string GxTreeSocketName;
            public bool HasXfiMetadata;
            public string XfiPath;
            public string XfiHeader;
            public string XfiHeaderKind;
            public string XfiAttachSlot;
            public string XfiAttachVariant;
            public int XfiTransformCount;
            public string XfiTransformTranslations;
            public int XfiDirectionRangeCount;
            public string XfiDirectionRanges;
            public bool HasXfiAttachSocket;
            public Vector3 XfiAttachSocketOffset;
            public bool HasFrameTopSocket;
            public Vector3 FrameTopSocketOffset;
            public string XfiSocketQuality;
            public string XfiSocketName;
            public string QualityFlag;
            public string ReviewReason;

            public static AlignmentSnapshot From(SerializedProperty entry)
            {
                return new AlignmentSnapshot
                {
                    PartId = GetString(entry, "partId"),
                    Slot = GetEnum(entry, "slot"),
                    NormalizedScale = GetFloat(entry, "normalizedScale", 1f),
                    PivotOffset = GetVector3(entry, "pivotOffset"),
                    SocketOffset = GetVector3(entry, "socketOffset"),
                    SocketEuler = GetVector3(entry, "socketEuler"),
                    HasGxTreeSocket = GetBool(entry, "hasGxTreeSocket"),
                    GxTreeSocketOffset = GetVector3(entry, "gxTreeSocketOffset"),
                    GxTreeSocketName = GetString(entry, "gxTreeSocketName"),
                    HasXfiMetadata = GetBool(entry, "hasXfiMetadata"),
                    XfiPath = GetString(entry, "xfiPath"),
                    XfiHeader = GetString(entry, "xfiHeader"),
                    XfiHeaderKind = GetString(entry, "xfiHeaderKind"),
                    XfiAttachSlot = GetString(entry, "xfiAttachSlot"),
                    XfiAttachVariant = GetString(entry, "xfiAttachVariant"),
                    XfiTransformCount = GetInt(entry, "xfiTransformCount"),
                    XfiTransformTranslations = GetString(entry, "xfiTransformTranslations"),
                    XfiDirectionRangeCount = GetInt(entry, "xfiDirectionRangeCount"),
                    XfiDirectionRanges = GetString(entry, "xfiDirectionRanges"),
                    HasXfiAttachSocket = GetBool(entry, "hasXfiAttachSocket"),
                    XfiAttachSocketOffset = GetVector3(entry, "xfiAttachSocketOffset"),
                    HasFrameTopSocket = GetBool(entry, "hasFrameTopSocket"),
                    FrameTopSocketOffset = GetVector3(entry, "frameTopSocketOffset"),
                    XfiSocketQuality = GetString(entry, "xfiSocketQuality"),
                    XfiSocketName = GetString(entry, "xfiSocketName"),
                    QualityFlag = GetString(entry, "qualityFlag"),
                    ReviewReason = GetString(entry, "reviewReason")
                };
            }

            public static AlignmentSnapshot CreateDefault(PartRow row)
            {
                return new AlignmentSnapshot
                {
                    PartId = row.PartId,
                    Slot = SlotToIndex(row.Slot),
                    NormalizedScale = AssemblyModelScale,
                    QualityFlag = "needs_review",
                    ReviewReason = "Created by playable part cleanup generation."
                };
            }

            public void Apply(SerializedProperty entry)
            {
                Set(entry, "partId", PartId);
                SetEnumValue(entry, "slot", Slot);
                Set(entry, "normalizedScale", NormalizedScale);
                Set(entry, "pivotOffset", PivotOffset);
                Set(entry, "socketOffset", SocketOffset);
                Set(entry, "socketEuler", SocketEuler);
                Set(entry, "hasGxTreeSocket", HasGxTreeSocket);
                Set(entry, "gxTreeSocketOffset", GxTreeSocketOffset);
                Set(entry, "gxTreeSocketName", GxTreeSocketName);
                Set(entry, "hasXfiMetadata", HasXfiMetadata);
                Set(entry, "xfiPath", XfiPath);
                Set(entry, "xfiHeader", XfiHeader);
                Set(entry, "xfiHeaderKind", XfiHeaderKind);
                Set(entry, "xfiAttachSlot", XfiAttachSlot);
                Set(entry, "xfiAttachVariant", XfiAttachVariant);
                Set(entry, "xfiTransformCount", XfiTransformCount);
                Set(entry, "xfiTransformTranslations", XfiTransformTranslations);
                Set(entry, "xfiDirectionRangeCount", XfiDirectionRangeCount);
                Set(entry, "xfiDirectionRanges", XfiDirectionRanges);
                Set(entry, "hasXfiAttachSocket", HasXfiAttachSocket);
                Set(entry, "xfiAttachSocketOffset", XfiAttachSocketOffset);
                Set(entry, "hasFrameTopSocket", HasFrameTopSocket);
                Set(entry, "frameTopSocketOffset", FrameTopSocketOffset);
                Set(entry, "xfiSocketQuality", XfiSocketQuality);
                Set(entry, "xfiSocketName", XfiSocketName);
                Set(entry, "qualityFlag", QualityFlag);
                Set(entry, "reviewReason", ReviewReason);
            }

            private static string GetString(SerializedProperty parent, string name)
            {
                var property = parent.FindPropertyRelative(name);
                return property == null ? string.Empty : property.stringValue;
            }

            private static int GetEnum(SerializedProperty parent, string name)
            {
                var property = parent.FindPropertyRelative(name);
                return property == null ? 0 : property.enumValueIndex;
            }

            private static int GetInt(SerializedProperty parent, string name)
            {
                var property = parent.FindPropertyRelative(name);
                return property == null ? 0 : property.intValue;
            }

            private static bool GetBool(SerializedProperty parent, string name)
            {
                var property = parent.FindPropertyRelative(name);
                return property != null && property.boolValue;
            }

            private static float GetFloat(SerializedProperty parent, string name, float fallback)
            {
                var property = parent.FindPropertyRelative(name);
                return property == null ? fallback : property.floatValue;
            }

            private static Vector3 GetVector3(SerializedProperty parent, string name)
            {
                var property = parent.FindPropertyRelative(name);
                return property == null ? Vector3.zero : property.vector3Value;
            }

            private static void Set(SerializedProperty parent, string name, string value)
            {
                var property = parent.FindPropertyRelative(name);
                if (property != null)
                {
                    property.stringValue = value ?? string.Empty;
                }
            }

            private static void Set(SerializedProperty parent, string name, int value)
            {
                var property = parent.FindPropertyRelative(name);
                if (property != null)
                {
                    property.intValue = value;
                }
            }

            private static void Set(SerializedProperty parent, string name, float value)
            {
                var property = parent.FindPropertyRelative(name);
                if (property != null)
                {
                    property.floatValue = value;
                }
            }

            private static void Set(SerializedProperty parent, string name, bool value)
            {
                var property = parent.FindPropertyRelative(name);
                if (property != null)
                {
                    property.boolValue = value;
                }
            }

            private static void Set(SerializedProperty parent, string name, Vector3 value)
            {
                var property = parent.FindPropertyRelative(name);
                if (property != null)
                {
                    property.vector3Value = value;
                }
            }

            private static void SetEnumValue(SerializedProperty parent, string name, int value)
            {
                var property = parent.FindPropertyRelative(name);
                if (property != null)
                {
                    property.enumValueIndex = value;
                }
            }
        }
    }
}
