using Features.Garage;
using Features.Garage.Infrastructure;
using Features.Garage.Presentation;
using Features.Unit.Infrastructure;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class GarageUnitPreviewAssemblyFrameTopDirectTests
    {
        private const string ModuleCatalogPath = "Assets/Data/Garage/ModuleCatalog.asset";
        private const string VisualCatalogPath = "Assets/Data/Garage/NovaGenerated/NovaPartVisualCatalog.asset";
        private const string AlignmentCatalogPath = "Assets/Data/Garage/NovaGenerated/NovaPartAlignmentCatalog.asset";

        [Test]
        public void TryCreatePreviewRoot_UsesFrameSocketOffsetWhenFrameTopSocketIsZero()
        {
            var cameraObject = new GameObject("GaragePreviewCameraTest", typeof(Camera));
            var framePrefab = new GameObject("FramePrefab");
            var firepowerPrefab = new GameObject("FirepowerPrefab");
            var mobilityPrefab = new GameObject("MobilityPrefab");
            GameObject previewRoot = null;

            try
            {
                var viewModel = new GarageSlotViewModel(
                    "A-01",
                    "A-01",
                    "test",
                    "test",
                    hasCommittedLoadout: true,
                    hasDraftChanges: false,
                    isEmpty: false,
                    isSelected: true,
                    frameId: "frame",
                    firepowerId: "firepower",
                    mobilityId: "mobility",
                    frameAlignment: AutoOkAlignment(
                        socketOffset: new Vector3(0f, 0.25f, 0f),
                        hasFrameTopSocket: true,
                        frameTopSocketOffset: Vector3.zero),
                    firepowerAlignment: AutoOkAlignment(
                        socketOffset: new Vector3(0f, -0.1f, 0f),
                        socketEuler: new Vector3(0f, 0f, 90f)),
                    mobilityAlignment: AutoOkAlignment(
                        socketOffset: Vector3.zero,
                        hasGxTreeSocket: true,
                        gxTreeSocketOffset: new Vector3(0f, 0.5f, 0f),
                        gxTreeSocketName: "legs"),
                    mobilityUsesAssemblyPivot: false);

                Assert.IsTrue(GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                    viewModel,
                    cameraObject.GetComponent<Camera>(),
                    framePrefab,
                    firepowerPrefab,
                    mobilityPrefab,
                    out previewRoot));

                var firepower = previewRoot.transform.Find("FirepowerPrefab(Clone)");

                Assert.NotNull(firepower);
                Assert.That(firepower.localPosition.y, Is.EqualTo(0.35f).Within(0.0001f));
            }
            finally
            {
                if (previewRoot != null)
                    Object.DestroyImmediate(previewRoot);

                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(framePrefab);
                Object.DestroyImmediate(firepowerPrefab);
                Object.DestroyImmediate(mobilityPrefab);
            }
        }

        [Test]
        public void TryCreatePreviewRoot_SeatsCassowaryKingpinSpitfireOnCatalogFrameSocket()
        {
            var moduleCatalog = AssetDatabase.LoadAssetAtPath<ModuleCatalog>(ModuleCatalogPath);
            var visualCatalog = AssetDatabase.LoadAssetAtPath<NovaPartVisualCatalog>(VisualCatalogPath);
            var alignmentCatalog = AssetDatabase.LoadAssetAtPath<NovaPartAlignmentCatalog>(AlignmentCatalogPath);
            var cameraObject = new GameObject("GaragePreviewCameraTest", typeof(Camera));
            GameObject previewRoot = null;

            try
            {
                Assert.NotNull(moduleCatalog);
                Assert.NotNull(visualCatalog);
                Assert.NotNull(alignmentCatalog);

                var catalog = new GaragePanelCatalogFactory().Build(
                    moduleCatalog,
                    visualCatalog,
                    alignmentCatalog);
                var frame = catalog.FindFrame("nova_frame_body26_kp");
                var firepower = catalog.FindFirepower("nova_fire_arm32_sppoo");
                var mobility = catalog.FindMobility("nova_mob_legs21_ksor");

                Assert.NotNull(frame);
                Assert.NotNull(firepower);
                Assert.NotNull(mobility);
                Assert.NotNull(frame.AssemblyPrefab);
                Assert.NotNull(firepower.AssemblyPrefab);
                Assert.NotNull(mobility.AssemblyPrefab);
                Assert.That(frame.Alignment.SocketOffset.y, Is.GreaterThan(0.0001f));
                Assert.That(frame.Alignment.FrameTopSocketOffset.sqrMagnitude, Is.LessThan(0.000001f));

                var viewModel = new GarageSlotViewModel(
                    "A-01",
                    "A-01",
                    "test",
                    "test",
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
                    mobilityUsesAssemblyPivot: mobility.UseAssemblyPivot);

                Assert.IsTrue(GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                    viewModel,
                    cameraObject.GetComponent<Camera>(),
                    frame.AssemblyPrefab,
                    firepower.AssemblyPrefab,
                    mobility.AssemblyPrefab,
                    out previewRoot));

                var firepowerObject = FindDirectChild(previewRoot.transform, firepower.Id);
                var expectedY = frame.Alignment.SocketOffset.y - firepower.Alignment.SocketOffset.y;

                Assert.NotNull(firepowerObject);
                Assert.That(firepowerObject.localPosition.y, Is.EqualTo(expectedY).Within(0.0001f));
            }
            finally
            {
                if (previewRoot != null)
                    Object.DestroyImmediate(previewRoot);

                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void TryCreatePreviewRoot_ClampsDirectionOnlyFirepowerSocketToVisibleBottom()
        {
            var cameraObject = new GameObject("GaragePreviewCameraTest", typeof(Camera));
            var framePrefab = new GameObject("FramePrefab");
            var firepowerPrefab = CreateVisibleBottomWeaponPrefab();
            var mobilityPrefab = new GameObject("MobilityPrefab");
            GameObject previewRoot = null;

            try
            {
                var viewModel = new GarageSlotViewModel(
                    "A-01",
                    "A-01",
                    "test",
                    "test",
                    hasCommittedLoadout: true,
                    hasDraftChanges: false,
                    isEmpty: false,
                    isSelected: true,
                    frameId: "frame",
                    firepowerId: "firepower",
                    mobilityId: "mobility",
                    frameAlignment: AutoOkAlignment(
                        socketOffset: Vector3.zero,
                        hasFrameTopSocket: true),
                    firepowerAlignment: AutoOkAlignment(
                        socketOffset: new Vector3(0f, -0.2f, 0f),
                        socketEuler: new Vector3(0f, 0f, 90f),
                        xfiSocketQuality: "xfi_weapon_direction_only"),
                    mobilityAlignment: AutoOkAlignment(
                        socketOffset: Vector3.zero,
                        hasGxTreeSocket: true,
                        gxTreeSocketOffset: new Vector3(0f, 0.5f, 0f),
                        gxTreeSocketName: "legs"),
                    mobilityUsesAssemblyPivot: false);

                Assert.IsTrue(GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                    viewModel,
                    cameraObject.GetComponent<Camera>(),
                    framePrefab,
                    firepowerPrefab,
                    mobilityPrefab,
                    out previewRoot));

                var firepower = previewRoot.transform.Find("FirepowerPrefab(Clone)");

                Assert.NotNull(firepower);
                Assert.That(firepower.localPosition.y, Is.EqualTo(0f).Within(0.0001f));
            }
            finally
            {
                if (previewRoot != null)
                    Object.DestroyImmediate(previewRoot);

                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(framePrefab);
                Object.DestroyImmediate(firepowerPrefab);
                Object.DestroyImmediate(mobilityPrefab);
            }
        }

        [Test]
        public void TryCreatePreviewRoot_SeatsCenturionProlixOnVisibleFirepowerBottom()
        {
            var moduleCatalog = AssetDatabase.LoadAssetAtPath<ModuleCatalog>(ModuleCatalogPath);
            var visualCatalog = AssetDatabase.LoadAssetAtPath<NovaPartVisualCatalog>(VisualCatalogPath);
            var alignmentCatalog = AssetDatabase.LoadAssetAtPath<NovaPartAlignmentCatalog>(AlignmentCatalogPath);
            var cameraObject = new GameObject("GaragePreviewCameraTest", typeof(Camera));
            GameObject previewRoot = null;

            try
            {
                Assert.NotNull(moduleCatalog);
                Assert.NotNull(visualCatalog);
                Assert.NotNull(alignmentCatalog);

                var catalog = new GaragePanelCatalogFactory().Build(
                    moduleCatalog,
                    visualCatalog,
                    alignmentCatalog);
                var frame = catalog.FindFrame("nova_frame_body13_scro");
                var firepower = catalog.FindFirepower("nova_fire_arm17_prrs");
                var mobility = catalog.FindMobility("nova_mob_legs10_prg");

                Assert.NotNull(frame);
                Assert.NotNull(firepower);
                Assert.NotNull(mobility);
                Assert.NotNull(frame.AssemblyPrefab);
                Assert.NotNull(firepower.AssemblyPrefab);
                Assert.NotNull(mobility.AssemblyPrefab);

                var viewModel = new GarageSlotViewModel(
                    "A-01",
                    "A-01",
                    "test",
                    "test",
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
                    mobilityUsesAssemblyPivot: mobility.UseAssemblyPivot);

                Assert.IsTrue(GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                    viewModel,
                    cameraObject.GetComponent<Camera>(),
                    frame.AssemblyPrefab,
                    firepower.AssemblyPrefab,
                    mobility.AssemblyPrefab,
                    out previewRoot));

                var firepowerObject = FindDirectChild(previewRoot.transform, firepower.Id);

                Assert.NotNull(firepowerObject);
                var firepowerBounds = CalculateLocalRendererBounds(previewRoot.transform, firepowerObject);
                Assert.That(
                    firepowerBounds.min.y,
                    Is.EqualTo(frame.Alignment.FrameTopSocketOffset.y).Within(0.02f));
            }
            finally
            {
                if (previewRoot != null)
                    Object.DestroyImmediate(previewRoot);

                Object.DestroyImmediate(cameraObject);
            }
        }

        private static GaragePanelCatalog.PartAlignment AutoOkAlignment(
            Vector3 socketOffset,
            Vector3 socketEuler = default,
            bool hasFrameTopSocket = false,
            Vector3 frameTopSocketOffset = default,
            bool hasGxTreeSocket = false,
            Vector3 gxTreeSocketOffset = default,
            string gxTreeSocketName = null,
            string xfiSocketQuality = null)
        {
            return new GaragePanelCatalog.PartAlignment
            {
                SocketOffset = socketOffset,
                SocketEuler = socketEuler,
                HasFrameTopSocket = hasFrameTopSocket,
                FrameTopSocketOffset = frameTopSocketOffset,
                HasGxTreeSocket = hasGxTreeSocket,
                GxTreeSocketOffset = gxTreeSocketOffset,
                GxTreeSocketName = gxTreeSocketName,
                XfiSocketQuality = xfiSocketQuality,
                QualityFlag = "auto_ok"
            };
        }

        private static GameObject CreateVisibleBottomWeaponPrefab()
        {
            var root = new GameObject("FirepowerPrefab");
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "VisibleMesh";
            cube.transform.SetParent(root.transform, false);
            cube.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            return root;
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

        private static Bounds CalculateLocalRendererBounds(Transform root, Transform target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers.Length, Is.GreaterThan(0), target.name);

            var initialized = false;
            var bounds = new Bounds();
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

            return bounds;
        }
    }
}
