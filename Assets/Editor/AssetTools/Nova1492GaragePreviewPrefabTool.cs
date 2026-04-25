using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Features.Garage.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectSD.EditorTools
{
    public static class Nova1492GaragePreviewPrefabTool
    {
        private const string MenuPath = "Tools/Nova1492/Create Garage Preview Model Prefabs";
        private const string WireMenuPath = "Tools/Nova1492/Wire Garage Preview Model Prefabs";
        private const string ShortlistPath = "artifacts/nova1492/lobby_model_shortlist.csv";
        private const string PrefabRoot = "Assets/Prefabs/Features/Garage/PreviewModels";
        private const string ReportPath = "artifacts/nova1492/lobby_preview_prefab_pack_report.md";
        private const string MappingReportPath = "artifacts/nova1492/lobby_preview_mapping_report.md";

        [MenuItem(MenuPath)]
        public static void CreateGaragePreviewModelPrefabs()
        {
            if (!File.Exists(ShortlistPath))
            {
                throw new FileNotFoundException("Shortlist CSV not found.", ShortlistPath);
            }

            AssetDatabase.Refresh();
            Directory.CreateDirectory(PrefabRoot);

            var rows = ReadCsv(ShortlistPath);
            var results = new List<PrefabResult>();

            foreach (var row in rows)
            {
                if (!row.TryGetValue("slot", out var slot) || slot == "ambient_prop")
                {
                    continue;
                }

                if (!row.TryGetValue("model_path", out var modelPath) || string.IsNullOrWhiteSpace(modelPath))
                {
                    results.Add(PrefabResult.Failed(slot, "(missing)", string.Empty, "missing model_path"));
                    continue;
                }

                if (!row.TryGetValue("priority", out var priority))
                {
                    priority = "0";
                }

                row.TryGetValue("source_relative_path", out var sourcePath);

                var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (modelAsset == null)
                {
                    results.Add(PrefabResult.Failed(slot, sourcePath, modelPath, "model asset not loaded"));
                    continue;
                }

                var prefabName = BuildPrefabName(slot, priority, sourcePath, modelPath);
                var prefabPath = $"{PrefabRoot}/{prefabName}.prefab";
                var root = new GameObject(prefabName);

                try
                {
                    var instance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
                    if (instance == null)
                    {
                        throw new InvalidOperationException("PrefabUtility.InstantiatePrefab returned null.");
                    }

                    instance.name = "Model";
                    instance.transform.SetParent(root.transform, false);
                    NormalizeModel(instance.transform, slot, out var boundsSize, out var appliedScale);

                    var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    if (prefab == null)
                    {
                        throw new InvalidOperationException("SaveAsPrefabAsset returned null.");
                    }

                    results.Add(PrefabResult.Created(slot, sourcePath, modelPath, prefabPath, boundsSize, appliedScale, CountMissingMaterials(instance)));
                }
                catch (Exception ex)
                {
                    results.Add(PrefabResult.Failed(slot, sourcePath, modelPath, ex.Message));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WriteReport(results);
            Debug.Log($"[Nova1492] Garage preview prefab pack generated. count={results.Count}, report={ReportPath}");
        }

        [MenuItem(WireMenuPath)]
        public static void WireGaragePreviewModelPrefabs()
        {
            var view = FindScenePreviewView();
            if (view == null)
            {
                throw new InvalidOperationException("GarageUnitPreviewView was not found in the active scene.");
            }

            var frameEntries = new[]
            {
                new MappingEntry("frame_striker", $"{PrefabRoot}/GaragePreview_frame_body_1_body23_ms.prefab"),
                new MappingEntry("frame_bastion", $"{PrefabRoot}/GaragePreview_frame_body_2_body25_bosro.prefab"),
                new MappingEntry("frame_relay", $"{PrefabRoot}/GaragePreview_frame_body_3_body37_ktn.prefab")
            };
            var firepowerEntries = new[]
            {
                new MappingEntry("fire_scatter", $"{PrefabRoot}/GaragePreview_firepower_1_arm43_przso.prefab"),
                new MappingEntry("fire_pulse", $"{PrefabRoot}/GaragePreview_firepower_2_arm20_rkto.prefab"),
                new MappingEntry("fire_rail", $"{PrefabRoot}/GaragePreview_firepower_4_arm31_skokr.prefab")
            };
            var mobilityEntries = new[]
            {
                new MappingEntry("mob_treads", $"{PrefabRoot}/GaragePreview_mobility_1_legs24_sts.prefab"),
                new MappingEntry("mob_vector", $"{PrefabRoot}/GaragePreview_mobility_2_legs20_spod.prefab"),
                new MappingEntry("mob_burst", $"{PrefabRoot}/GaragePreview_mobility_3_legs7_hb.prefab")
            };

            var so = new SerializedObject(view);
            SetMappingArray(so, "_frameModelPrefabs", frameEntries);
            SetMappingArray(so, "_firepowerModelPrefabs", firepowerEntries);
            SetMappingArray(so, "_mobilityModelPrefabs", mobilityEntries);
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            WriteMappingReport(view, frameEntries, firepowerEntries, mobilityEntries);
            Debug.Log($"[Nova1492] Wired GarageUnitPreviewView preview mappings. report={MappingReportPath}");
        }

        private static void NormalizeModel(Transform modelRoot, string slot, out Vector3 boundsSize, out float appliedScale)
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
                "frame_body" => 0.95f,
                "firepower" => 0.82f,
                "mobility" => 0.9f,
                _ => 0.9f
            };

            appliedScale = maxDimension > 0.0001f ? targetMaxDimension / maxDimension : 1f;
            modelRoot.localScale *= appliedScale;
            modelRoot.localPosition -= bounds.center * appliedScale;
        }

        private static GarageUnitPreviewView FindScenePreviewView()
        {
            var activeScene = SceneManager.GetActiveScene();
            var views = Resources.FindObjectsOfTypeAll<GarageUnitPreviewView>();
            for (var i = 0; i < views.Length; i++)
            {
                var view = views[i];
                if (view != null && view.gameObject.scene == activeScene && !EditorUtility.IsPersistent(view))
                {
                    return view;
                }
            }

            return null;
        }

        private static void SetMappingArray(SerializedObject so, string propertyName, IReadOnlyList<MappingEntry> entries)
        {
            var property = so.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                throw new InvalidOperationException($"Serialized mapping array was not found: {propertyName}");
            }

            property.arraySize = entries.Count;
            for (var i = 0; i < entries.Count; i++)
            {
                var entryProperty = property.GetArrayElementAtIndex(i);
                var idProperty = entryProperty.FindPropertyRelative("_id");
                var prefabProperty = entryProperty.FindPropertyRelative("_prefab");
                if (idProperty == null || prefabProperty == null)
                {
                    throw new InvalidOperationException($"Mapping entry fields were not found: {propertyName}[{i}]");
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entries[i].PrefabPath);
                if (prefab == null)
                {
                    throw new FileNotFoundException("Preview prefab was not found.", entries[i].PrefabPath);
                }

                idProperty.stringValue = entries[i].Id;
                prefabProperty.objectReferenceValue = prefab;
            }
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

        private static string BuildPrefabName(string slot, string priority, string sourcePath, string modelPath)
        {
            var sourceName = string.IsNullOrWhiteSpace(sourcePath)
                ? Path.GetFileNameWithoutExtension(modelPath)
                : Path.GetFileNameWithoutExtension(sourcePath.Replace('\\', '/'));

            return $"GaragePreview_{Sanitize(slot)}_{Sanitize(priority)}_{Sanitize(sourceName)}";
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unknown";
            }

            var builder = new StringBuilder(value.Length);
            foreach (var c in value)
            {
                builder.Append(char.IsLetterOrDigit(c) ? c : '_');
            }

            var result = builder.ToString().Trim('_');
            return string.IsNullOrWhiteSpace(result) ? "unknown" : result;
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

        private static void WriteReport(IReadOnlyList<PrefabResult> results)
        {
            var created = 0;
            var failed = 0;
            foreach (var result in results)
            {
                if (result.Success) created++;
                else failed++;
            }

            var builder = new StringBuilder();
            builder.AppendLine("# LobbyScene Nova1492 Preview Prefab Pack Report");
            builder.AppendLine();
            builder.AppendLine($"> generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine();
            builder.AppendLine($"- shortlist: `{ShortlistPath}`");
            builder.AppendLine($"- prefab root: `{PrefabRoot}`");
            builder.AppendLine($"- created: {created}");
            builder.AppendLine($"- failed: {failed}");
            builder.AppendLine();
            builder.AppendLine("| slot | source | prefab | original bounds | scale | missing materials | status |");
            builder.AppendLine("|---|---|---|---|---:|---:|---|");

            foreach (var result in results)
            {
                var bounds = $"{result.BoundsSize.x.ToString("0.###", CultureInfo.InvariantCulture)}x{result.BoundsSize.y.ToString("0.###", CultureInfo.InvariantCulture)}x{result.BoundsSize.z.ToString("0.###", CultureInfo.InvariantCulture)}";
                var scale = result.AppliedScale.ToString("0.######", CultureInfo.InvariantCulture);
                var prefab = string.IsNullOrWhiteSpace(result.PrefabPath) ? "" : $"`{result.PrefabPath}`";
                builder.AppendLine($"| {result.Slot} | `{result.SourcePath}` | {prefab} | {bounds} | {scale} | {result.MissingMaterials} | {result.Status} |");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, builder.ToString(), Encoding.UTF8);
        }

        private static void WriteMappingReport(
            GarageUnitPreviewView view,
            IReadOnlyList<MappingEntry> frameEntries,
            IReadOnlyList<MappingEntry> firepowerEntries,
            IReadOnlyList<MappingEntry> mobilityEntries)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# LobbyScene Nova1492 Preview Mapping Report");
            builder.AppendLine();
            builder.AppendLine($"> generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine();
            builder.AppendLine($"- scene: `{view.gameObject.scene.path}`");
            builder.AppendLine($"- view: `{GetHierarchyPath(view.transform)}`");
            builder.AppendLine($"- frame mappings: {frameEntries.Count}");
            builder.AppendLine($"- firepower mappings: {firepowerEntries.Count}");
            builder.AppendLine($"- mobility mappings: {mobilityEntries.Count}");
            builder.AppendLine();
            builder.AppendLine("| slot | id | prefab |");
            builder.AppendLine("|---|---|---|");
            AppendMappingRows(builder, "frame_body", frameEntries);
            AppendMappingRows(builder, "firepower", firepowerEntries);
            AppendMappingRows(builder, "mobility", mobilityEntries);

            Directory.CreateDirectory(Path.GetDirectoryName(MappingReportPath));
            File.WriteAllText(MappingReportPath, builder.ToString(), Encoding.UTF8);
        }

        private static void AppendMappingRows(StringBuilder builder, string slot, IReadOnlyList<MappingEntry> entries)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                builder.AppendLine($"| {slot} | `{entries[i].Id}` | `{entries[i].PrefabPath}` |");
            }
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var path = transform.name;
            var current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return "/" + path;
        }

        private readonly struct PrefabResult
        {
            private PrefabResult(string slot, string sourcePath, string modelPath, string prefabPath, Vector3 boundsSize, float appliedScale, int missingMaterials, bool success, string status)
            {
                Slot = slot;
                SourcePath = sourcePath ?? string.Empty;
                ModelPath = modelPath ?? string.Empty;
                PrefabPath = prefabPath ?? string.Empty;
                BoundsSize = boundsSize;
                AppliedScale = appliedScale;
                MissingMaterials = missingMaterials;
                Success = success;
                Status = status;
            }

            public string Slot { get; }
            public string SourcePath { get; }
            public string ModelPath { get; }
            public string PrefabPath { get; }
            public Vector3 BoundsSize { get; }
            public float AppliedScale { get; }
            public int MissingMaterials { get; }
            public bool Success { get; }
            public string Status { get; }

            public static PrefabResult Created(string slot, string sourcePath, string modelPath, string prefabPath, Vector3 boundsSize, float appliedScale, int missingMaterials)
            {
                return new PrefabResult(slot, sourcePath, modelPath, prefabPath, boundsSize, appliedScale, missingMaterials, true, "created");
            }

            public static PrefabResult Failed(string slot, string sourcePath, string modelPath, string reason)
            {
                return new PrefabResult(slot, sourcePath, modelPath, string.Empty, Vector3.zero, 1f, 0, false, $"failed: {reason}");
            }
        }

        private readonly struct MappingEntry
        {
            public MappingEntry(string id, string prefabPath)
            {
                Id = id;
                PrefabPath = prefabPath;
            }

            public string Id { get; }
            public string PrefabPath { get; }
        }
    }
}
