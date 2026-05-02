using System;
using System.Collections.Generic;
using System.IO;
using Features.Garage;
using Features.Garage.Infrastructure;
using Features.Garage.Presentation;
using Features.Unit.Infrastructure;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectSD.EditorTools
{
    public static class GarageHumanoidWeaponAssemblyCaptureTool
    {
        private const string ModuleCatalogPath = "Assets/Data/Garage/ModuleCatalog.asset";
        private const string VisualCatalogPath = "Assets/Data/Garage/NovaGenerated/NovaPartVisualCatalog.asset";
        private const string AlignmentCatalogPath = "Assets/Data/Garage/NovaGenerated/NovaPartAlignmentCatalog.asset";
        private const string OutputRoot = "artifacts/unity/garage-humanoid-weapon-current";
        private const int CaptureSize = 512;

        [MenuItem("Tools/Garage/Capture Humanoid Weapon Assembly Samples")]
        public static void CaptureHumanoidWeaponAssemblySamples()
        {
            Directory.CreateDirectory(OutputRoot);

            var moduleCatalog = AssetDatabase.LoadAssetAtPath<ModuleCatalog>(ModuleCatalogPath);
            var visualCatalog = AssetDatabase.LoadAssetAtPath<NovaPartVisualCatalog>(VisualCatalogPath);
            var alignmentCatalog = AssetDatabase.LoadAssetAtPath<NovaPartAlignmentCatalog>(AlignmentCatalogPath);
            if (moduleCatalog == null || visualCatalog == null || alignmentCatalog == null)
                throw new InvalidOperationException("Garage humanoid capture requires ModuleCatalog, NovaPartVisualCatalog, and NovaPartAlignmentCatalog assets.");

            var catalog = new GaragePanelCatalogFactory().Build(moduleCatalog, visualCatalog, alignmentCatalog);
            var samples = new[]
            {
                new CaptureSample("hammershock-starship-squadron", "nova_mob_legs24_sts", "nova_frame_body10_skdr", "nova_fire_arm39_hmsk"),
                new CaptureSample("spitfire-cassowary-kingpin", "nova_mob_legs21_ksor", "nova_frame_body26_kp", "nova_fire_arm32_sppoo"),
                new CaptureSample("bazooka-cassowary-kingpin", "nova_mob_legs21_ksor", "nova_frame_body26_kp", "nova_fire_arm24_bzk"),
                new CaptureSample("thunderbolt-cassowary-kingpin", "nova_mob_legs21_ksor", "nova_frame_body26_kp", "nova_fire_arm29_sdbt"),
                new CaptureSample("bazooka-s-cassowary-kingpin", "nova_mob_legs21_ksor", "nova_frame_body26_kp", "nova_fire_s_arm52_bzk")
            };

            var reports = new List<SampleReport>();
            var cameraObject = CreateCamera();
            var camera = cameraObject.GetComponent<Camera>();
            var renderTexture = CreateRenderTexture();
            camera.targetTexture = renderTexture;

            var previousActive = RenderTexture.active;
            try
            {
                foreach (var sample in samples)
                    reports.Add(CaptureSampleViews(catalog, sample, camera, renderTexture));
            }
            finally
            {
                RenderTexture.active = previousActive;
                camera.targetTexture = null;
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
                Object.DestroyImmediate(cameraObject);
            }

            var report = new CaptureReport
            {
                generatedAtLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"),
                generatedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"),
                outputRoot = OutputRoot,
                samples = reports.ToArray()
            };

            var reportPath = Path.Combine(OutputRoot, "report.json");
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
            AssetDatabase.Refresh();
            Debug.Log("[Garage] Humanoid weapon assembly captures written to " + OutputRoot);
        }

        private static SampleReport CaptureSampleViews(
            GaragePanelCatalog catalog,
            CaptureSample sample,
            Camera camera,
            RenderTexture renderTexture)
        {
            var frame = catalog.FindFrame(sample.FrameId);
            var firepower = catalog.FindFirepower(sample.FirepowerId);
            var mobility = catalog.FindMobility(sample.MobilityId);
            var report = new SampleReport
            {
                sampleId = sample.SampleId,
                mobilityId = sample.MobilityId,
                frameId = sample.FrameId,
                firepowerId = sample.FirepowerId,
                mobilityName = mobility?.DisplayName,
                frameName = frame?.DisplayName,
                firepowerName = firepower?.DisplayName,
                imagePaths = Array.Empty<string>()
            };

            try
            {
                if (frame == null || firepower == null || mobility == null)
                    throw new InvalidOperationException("Sample part id could not be resolved from GaragePanelCatalog.");

                report.frameAssemblyForm = frame.AssemblyForm.ToString();
                report.firepowerAssemblyForm = firepower.AssemblyForm.ToString();
                report.firepowerSocketQuality = firepower.Alignment?.XfiSocketQuality;
                report.firepowerSocketName = firepower.Alignment?.XfiSocketName;
                report.frameTopSocketY = ResolveFrameTopY(frame);

                var viewModel = new GarageSlotViewModel(
                    "A-01",
                    sample.SampleId,
                    "capture",
                    "capture",
                    hasCommittedLoadout: true,
                    hasDraftChanges: false,
                    isEmpty: false,
                    isSelected: true,
                    frameId: frame.Id,
                    firepowerId: firepower.Id,
                    mobilityId: mobility.Id,
                    frameAlignment: frame.Alignment,
                    firepowerAlignment: firepower.Alignment,
                    mobilityAlignment: mobility.Alignment,
                    mobilityUsesAssemblyPivot: mobility.UseAssemblyPivot,
                    frameAssemblyForm: frame.AssemblyForm,
                    firepowerAssemblyForm: firepower.AssemblyForm);

                if (!GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                        viewModel,
                        camera,
                        frame.AssemblyPrefab,
                        firepower.AssemblyPrefab,
                        mobility.AssemblyPrefab,
                        out var previewRoot))
                    throw new InvalidOperationException("GarageUnitPreviewAssembly.TryCreatePreviewRoot returned false.");

                try
                {
                    var frameObject = FindDirectChild(previewRoot.transform, frame.Id);
                    var firepowerObject = FindDirectChild(previewRoot.transform, firepower.Id);
                    var mobilityObject = FindDirectChild(previewRoot.transform, mobility.Id);
                    if (frameObject == null || firepowerObject == null || mobilityObject == null)
                        throw new InvalidOperationException("Captured preview root is missing one or more direct part children.");

                    report.rawFrame = MeasurePart(previewRoot.transform, frameObject);
                    report.rawFirepower = MeasurePart(previewRoot.transform, firepowerObject);
                    report.rawMobility = MeasurePart(previewRoot.transform, mobilityObject);
                    report.rawFirepowerBottomVsFrameTop = report.rawFirepower.boundsMin.y - report.frameTopSocketY;

                    FitAssemblyToPreviewRoot(previewRoot);
                    report.fittedAssembly = MeasureRoot(previewRoot.transform);

                    var imagePaths = new List<string>();
                    CaptureView(previewRoot, camera, renderTexture, sample.SampleId, "front", 0f, imagePaths);
                    CaptureView(previewRoot, camera, renderTexture, sample.SampleId, "iso", 35f, imagePaths);
                    CaptureView(previewRoot, camera, renderTexture, sample.SampleId, "side", 90f, imagePaths);
                    report.imagePaths = imagePaths.ToArray();
                    report.success = true;
                }
                finally
                {
                    Object.DestroyImmediate(previewRoot);
                }
            }
            catch (Exception ex)
            {
                report.success = false;
                report.error = ex.Message;
                Debug.LogError("[Garage] Humanoid weapon capture failed for " + sample.SampleId + ": " + ex);
            }

            return report;
        }

        private static GameObject CreateCamera()
        {
            var cameraObject = new GameObject("GarageHumanoidWeaponCaptureCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.45f, 0.49f, 0.51f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 1.55f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 30f;
            camera.transform.position = Vector3.zero;
            camera.transform.rotation = Quaternion.identity;

            var lightObject = new GameObject("GarageHumanoidWeaponCaptureLight", typeof(Light));
            lightObject.transform.SetParent(cameraObject.transform, false);
            lightObject.transform.localEulerAngles = new Vector3(38f, -32f, 0f);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.92f, 0.97f, 1f, 1f);
            light.intensity = 2.4f;

            return cameraObject;
        }

        private static RenderTexture CreateRenderTexture()
        {
            var renderTexture = new RenderTexture(CaptureSize, CaptureSize, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 2,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            renderTexture.Create();
            return renderTexture;
        }

        private static void CaptureView(
            GameObject previewRoot,
            Camera camera,
            RenderTexture renderTexture,
            string sampleId,
            string viewName,
            float yaw,
            ICollection<string> imagePaths)
        {
            GarageUnitPreviewAssembly.SetYaw(previewRoot, yaw);
            camera.Render();
            RenderTexture.active = renderTexture;

            var texture = new Texture2D(CaptureSize, CaptureSize, TextureFormat.RGBA32, false);
            try
            {
                texture.ReadPixels(new Rect(0, 0, CaptureSize, CaptureSize), 0, 0);
                texture.Apply();
                var relativePath = Path.Combine(OutputRoot, sampleId + "-" + viewName + ".png");
                File.WriteAllBytes(relativePath, texture.EncodeToPNG());
                imagePaths.Add(relativePath.Replace('\\', '/'));
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static float ResolveFrameTopY(GaragePanelCatalog.FrameOption frame)
        {
            if (frame?.Alignment == null)
                return 0f;

            var top = frame.Alignment.FrameTopSocketOffset.sqrMagnitude > 0.000001f
                ? frame.Alignment.FrameTopSocketOffset
                : frame.Alignment.SocketOffset;
            return frame.Alignment.PivotOffset.y + top.y;
        }

        private static PartReport MeasurePart(Transform root, Transform part)
        {
            var report = new PartReport
            {
                localPosition = part.localPosition,
                localEulerAngles = part.localEulerAngles
            };

            if (TryGetLocalRendererBounds(root, part, out var bounds))
            {
                report.hasBounds = true;
                report.boundsMin = bounds.min;
                report.boundsMax = bounds.max;
                report.boundsCenter = bounds.center;
                report.boundsSize = bounds.size;
            }

            return report;
        }

        private static PartReport MeasureRoot(Transform root)
        {
            var report = new PartReport
            {
                localPosition = root.localPosition,
                localEulerAngles = root.localEulerAngles
            };

            if (TryGetLocalRendererBounds(root, root, out var bounds))
            {
                report.hasBounds = true;
                report.boundsMin = bounds.min;
                report.boundsMax = bounds.max;
                report.boundsCenter = bounds.center;
                report.boundsSize = bounds.size;
            }

            return report;
        }

        private static void FitAssemblyToPreviewRoot(GameObject previewRoot)
        {
            if (!TryGetWorldBounds(previewRoot, out var bounds))
                return;

            var centerLocal = previewRoot.transform.InverseTransformPoint(bounds.center);
            for (var i = 0; i < previewRoot.transform.childCount; i++)
            {
                var child = previewRoot.transform.GetChild(i);
                child.localPosition -= centerLocal;
            }

            var maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            if (maxExtent > 0.0001f)
            {
                var scale = Mathf.Clamp(1.32f / maxExtent, 0.55f, 3.1f);
                previewRoot.transform.localScale *= scale;
            }
        }

        private static Transform FindDirectChild(Transform parent, string nameFragment)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name.Contains(nameFragment))
                    return child;
            }

            return null;
        }

        private static bool TryGetWorldBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            var renderers = root != null ? root.GetComponentsInChildren<Renderer>(false) : null;
            if (renderers == null || renderers.Length == 0)
                return false;

            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return true;
        }

        private static bool TryGetLocalRendererBounds(Transform root, Transform target, out Bounds bounds)
        {
            bounds = default;
            var renderers = target != null ? target.GetComponentsInChildren<Renderer>(true) : null;
            if (root == null || renderers == null || renderers.Length == 0)
                return false;

            var initialized = false;
            for (var i = 0; i < renderers.Length; i++)
            {
                var worldBounds = renderers[i].bounds;
                var min = worldBounds.min;
                var max = worldBounds.max;
                var corners = new[]
                {
                    new Vector3(min.x, min.y, min.z),
                    new Vector3(min.x, min.y, max.z),
                    new Vector3(min.x, max.y, min.z),
                    new Vector3(min.x, max.y, max.z),
                    new Vector3(max.x, min.y, min.z),
                    new Vector3(max.x, min.y, max.z),
                    new Vector3(max.x, max.y, min.z),
                    new Vector3(max.x, max.y, max.z)
                };

                for (var cornerIndex = 0; cornerIndex < corners.Length; cornerIndex++)
                {
                    var local = root.InverseTransformPoint(corners[cornerIndex]);
                    if (!initialized)
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        initialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(local);
                    }
                }
            }

            return initialized;
        }

        [Serializable]
        private sealed class CaptureReport
        {
            public string generatedAtLocal;
            public string generatedAtUtc;
            public string outputRoot;
            public SampleReport[] samples;
        }

        [Serializable]
        private sealed class SampleReport
        {
            public string sampleId;
            public string mobilityId;
            public string frameId;
            public string firepowerId;
            public string mobilityName;
            public string frameName;
            public string firepowerName;
            public string frameAssemblyForm;
            public string firepowerAssemblyForm;
            public string firepowerSocketQuality;
            public string firepowerSocketName;
            public float frameTopSocketY;
            public float rawFirepowerBottomVsFrameTop;
            public bool success;
            public string error;
            public string[] imagePaths;
            public PartReport rawFrame;
            public PartReport rawFirepower;
            public PartReport rawMobility;
            public PartReport fittedAssembly;
        }

        [Serializable]
        private sealed class PartReport
        {
            public bool hasBounds;
            public Vector3 localPosition;
            public Vector3 localEulerAngles;
            public Vector3 boundsMin;
            public Vector3 boundsMax;
            public Vector3 boundsCenter;
            public Vector3 boundsSize;
        }

        private readonly struct CaptureSample
        {
            public CaptureSample(string sampleId, string mobilityId, string frameId, string firepowerId)
            {
                SampleId = sampleId;
                MobilityId = mobilityId;
                FrameId = frameId;
                FirepowerId = firepowerId;
            }

            public string SampleId { get; }
            public string MobilityId { get; }
            public string FrameId { get; }
            public string FirepowerId { get; }
        }
    }
}
