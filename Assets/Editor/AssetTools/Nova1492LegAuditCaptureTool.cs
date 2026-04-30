using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectSD.EditorTools
{
    public static class Nova1492LegAuditCaptureTool
    {
        private const string AuditManifestPath = "artifacts/nova1492/gx_leg_audit_manifest.csv";
        private const string CaptureRootPath = "artifacts/nova1492/leg-captures";
        private const string ManualReviewPath = "artifacts/nova1492/gx_leg_manual_review.csv";
        private const int CaptureSize = 384;
        private const int SheetCellSize = 256;

        [MenuItem("Tools/Nova1492/Capture Leg Audit Contact Sheet")]
        public static void CaptureLegAuditContactSheet()
        {
            AssetDatabase.Refresh();
            if (!File.Exists(AuditManifestPath))
            {
                throw new FileNotFoundException("Leg audit manifest was not found. Run GxObjConverter --stage audit first.", AuditManifestPath);
            }

            Directory.CreateDirectory(CaptureRootPath);
            var rows = ReadAuditRows(AuditManifestPath)
                .Where(row => !string.IsNullOrWhiteSpace(row.PartId))
                .OrderBy(row => row.PartId, StringComparer.Ordinal)
                .ToList();
            if (rows.Count == 0)
            {
                throw new InvalidOperationException("Leg audit manifest contains no rows to capture.");
            }

            var context = CreateRenderContext();
            var sheetRows = new List<Texture2D[]>();
            var failures = new List<string>();
            try
            {
                foreach (var row in rows)
                {
                    var perView = new List<Texture2D>();
                    foreach (var view in CaptureView.Views)
                    {
                        var capture = CaptureRow(context, row, view, failures);
                        if (capture != null)
                        {
                            perView.Add(capture);
                        }
                    }

                    if (perView.Count == CaptureView.Views.Length)
                    {
                        sheetRows.Add(perView.Select(texture => ResizeNearest(texture, SheetCellSize, SheetCellSize)).ToArray());
                    }
                }
            }
            finally
            {
                DestroyRenderContext(context);
            }

            WriteContactSheet(sheetRows);
            WriteManualReviewTemplate(rows);
            WriteCaptureReport(rows, failures);
            AssetDatabase.Refresh();
            Debug.Log("[Nova1492] Leg audit captures complete. rows=" + rows.Count + " failures=" + failures.Count + " root=" + CaptureRootPath);
        }

        private static Texture2D CaptureRow(RenderContext context, AuditRow row, CaptureView view, List<string> failures)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(row.ObjPath);
            if (model == null)
            {
                failures.Add(row.PartId + " missing model: " + row.ObjPath);
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null)
            {
                failures.Add(row.PartId + " instantiate failed: " + row.ObjPath);
                return null;
            }

            instance.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                instance.transform.SetParent(context.Root.transform, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.Euler(view.Euler);
                instance.transform.localScale = Vector3.one;
                SetHideFlags(instance);
                FitCamera(context.Camera, instance);
                SetOverlay(context, row, view);

                context.Camera.Render();
                RenderTexture.active = context.RenderTexture;
                var texture = new Texture2D(CaptureSize, CaptureSize, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, CaptureSize, CaptureSize), 0, 0);
                texture.Apply();
                RenderTexture.active = null;

                var path = Path.Combine(CaptureRootPath, row.PartId + "-" + view.Name + ".png");
                File.WriteAllBytes(path, texture.EncodeToPNG());
                return texture;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static RenderContext CreateRenderContext()
        {
            var root = new GameObject("Nova1492LegAuditCaptureRoot")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            var cameraObject = new GameObject("CaptureCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            cameraObject.transform.SetParent(root.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.36f, 0.35f, 0.32f, 1f);
            camera.orthographic = true;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.enabled = false;
            camera.transform.position = new Vector3(0f, 0f, -8f);
            camera.transform.rotation = Quaternion.identity;
            var renderTexture = new RenderTexture(CaptureSize, CaptureSize, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = renderTexture;

            var lightObject = new GameObject("CaptureKeyLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            lightObject.transform.SetParent(root.transform, false);
            lightObject.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;

            var overlayObject = new GameObject("CaptureOverlay")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            overlayObject.transform.SetParent(camera.transform, false);
            var overlay = overlayObject.AddComponent<TextMesh>();
            overlay.anchor = TextAnchor.LowerLeft;
            overlay.alignment = TextAlignment.Left;
            overlay.fontSize = 64;
            overlay.characterSize = 0.02f;
            overlay.color = Color.white;

            return new RenderContext(root, camera, renderTexture, overlay);
        }

        private static void DestroyRenderContext(RenderContext context)
        {
            if (context.Camera != null)
            {
                context.Camera.targetTexture = null;
            }

            if (context.RenderTexture != null)
            {
                context.RenderTexture.Release();
                Object.DestroyImmediate(context.RenderTexture);
            }

            if (context.Root != null)
            {
                Object.DestroyImmediate(context.Root);
            }
        }

        private static void FitCamera(Camera camera, GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                camera.orthographicSize = 1f;
                camera.transform.position = new Vector3(0f, 0f, -8f);
                return;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var max = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            camera.orthographicSize = Mathf.Max(0.35f, max * 0.62f);
            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z - 8f);
            camera.transform.rotation = Quaternion.identity;
        }

        private static void SetOverlay(RenderContext context, AuditRow row, CaptureView view)
        {
            var aspect = 1f;
            var y = -context.Camera.orthographicSize + 0.04f;
            var x = -context.Camera.orthographicSize * aspect + 0.04f;
            context.Overlay.transform.localPosition = new Vector3(x, y, 2f);
            context.Overlay.transform.localRotation = Quaternion.identity;
            context.Overlay.text =
                row.PartId + " / " + view.Name + "\n" +
                row.DisplayName + "\n" +
                row.Vertices + "/" + row.Triangles + " " + row.Verdict + " " + row.Flags;
        }

        private static void SetHideFlags(GameObject root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                transform.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        private static Texture2D ResizeNearest(Texture2D source, int width, int height)
        {
            var output = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (var y = 0; y < height; y++)
            {
                var sourceY = Mathf.Clamp(y * source.height / height, 0, source.height - 1);
                for (var x = 0; x < width; x++)
                {
                    var sourceX = Mathf.Clamp(x * source.width / width, 0, source.width - 1);
                    output.SetPixel(x, y, source.GetPixel(sourceX, sourceY));
                }
            }

            output.Apply();
            return output;
        }

        private static void WriteContactSheet(IReadOnlyList<Texture2D[]> rows)
        {
            if (rows.Count == 0)
            {
                return;
            }

            var sheet = new Texture2D(SheetCellSize * 3, SheetCellSize * rows.Count, TextureFormat.RGBA32, false);
            for (var y = 0; y < sheet.height; y++)
            {
                for (var x = 0; x < sheet.width; x++)
                {
                    sheet.SetPixel(x, y, Color.black);
                }
            }

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var yOffset = sheet.height - (rowIndex + 1) * SheetCellSize;
                for (var col = 0; col < rows[rowIndex].Length; col++)
                {
                    var texture = rows[rowIndex][col];
                    for (var y = 0; y < SheetCellSize; y++)
                    {
                        for (var x = 0; x < SheetCellSize; x++)
                        {
                            sheet.SetPixel(col * SheetCellSize + x, yOffset + y, texture.GetPixel(x, y));
                        }
                    }
                }
            }

            sheet.Apply();
            File.WriteAllBytes(Path.Combine(CaptureRootPath, "gx-leg-contact-sheet.png"), sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
        }

        private static void WriteManualReviewTemplate(IReadOnlyList<AuditRow> rows)
        {
            using var writer = new StreamWriter(ManualReviewPath, false, new UTF8Encoding(false));
            writer.WriteLine("part_id,display_name,source_relative_path,audit_verdict,audit_flags,review_result,notes");
            foreach (var row in rows)
            {
                writer.WriteLine(string.Join(",", new[]
                {
                    Csv(row.PartId),
                    Csv(row.DisplayName),
                    Csv(row.SourceRelativePath),
                    Csv(row.Verdict),
                    Csv(row.Flags),
                    Csv(""),
                    Csv("")
                }));
            }
        }

        private static void WriteCaptureReport(IReadOnlyList<AuditRow> rows, IReadOnlyList<string> failures)
        {
            var path = Path.Combine(CaptureRootPath, "gx-leg-capture-report.md");
            using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
            writer.WriteLine("# Nova1492 Leg Capture Report");
            writer.WriteLine();
            writer.WriteLine($"> generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine();
            writer.WriteLine($"- audit rows: {rows.Count}");
            writer.WriteLine($"- capture failures: {failures.Count}");
            writer.WriteLine($"- contact sheet: `{Path.Combine(CaptureRootPath, "gx-leg-contact-sheet.png").Replace('\\', '/')}`");
            writer.WriteLine($"- manual review: `{ManualReviewPath}`");
            if (failures.Count == 0)
            {
                return;
            }

            writer.WriteLine();
            writer.WriteLine("## Failures");
            writer.WriteLine();
            foreach (var failure in failures)
            {
                writer.WriteLine("- " + failure);
            }
        }

        private static List<AuditRow> ReadAuditRows(string path)
        {
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length < 2)
            {
                return new List<AuditRow>();
            }

            var headers = ParseCsvLine(lines[0]);
            var headerIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < headers.Count; i++)
            {
                headerIndex[headers[i]] = i;
            }

            var rows = new List<AuditRow>();
            for (var i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                var values = ParseCsvLine(lines[i]);
                rows.Add(new AuditRow
                {
                    PartId = Get(values, headerIndex, "part_id"),
                    DisplayName = Get(values, headerIndex, "display_name"),
                    SourceRelativePath = Get(values, headerIndex, "source_relative_path"),
                    Verdict = Get(values, headerIndex, "audit_verdict"),
                    Flags = Get(values, headerIndex, "audit_flags"),
                    Vertices = Get(values, headerIndex, "vertices"),
                    Triangles = Get(values, headerIndex, "triangles"),
                    ObjPath = Get(values, headerIndex, "obj_path")
                });
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
            return headerIndex.TryGetValue(header, out var index) && index >= 0 && index < values.Count
                ? values[index]
                : "";
        }

        private static string Csv(string value)
        {
            return "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
        }

        private sealed class AuditRow
        {
            public string PartId;
            public string DisplayName;
            public string SourceRelativePath;
            public string Verdict;
            public string Flags;
            public string Vertices;
            public string Triangles;
            public string ObjPath;
        }

        private sealed class RenderContext
        {
            public RenderContext(GameObject root, Camera camera, RenderTexture renderTexture, TextMesh overlay)
            {
                Root = root;
                Camera = camera;
                RenderTexture = renderTexture;
                Overlay = overlay;
            }

            public GameObject Root { get; }
            public Camera Camera { get; }
            public RenderTexture RenderTexture { get; }
            public TextMesh Overlay { get; }
        }

        private sealed class CaptureView
        {
            public CaptureView(string name, Vector3 euler)
            {
                Name = name;
                Euler = euler;
            }

            public string Name { get; }
            public Vector3 Euler { get; }

            public static readonly CaptureView[] Views =
            {
                new("iso", new Vector3(22f, -35f, 0f)),
                new("front", Vector3.zero),
                new("side", new Vector3(0f, 90f, 0f))
            };
        }
    }
}
