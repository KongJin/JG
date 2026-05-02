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

                Assert.IsTrue(GarageUnitPreviewAssembly.HasPreviewAssemblyData(viewModel));
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
        public void TryCreatePreviewRoot_UsesVisualBoundsWhenPrefabPivotsAreOffset()
        {
            var cameraObject = new GameObject("GaragePreviewCameraTest", typeof(Camera));
            var framePrefab = CreateOffsetCubePrefab("FramePrefab", new Vector3(2f, 0f, 0f));
            var firepowerPrefab = CreateOffsetCubePrefab("FirepowerPrefab", new Vector3(-3f, 0f, 0f));
            var mobilityPrefab = CreateOffsetCubePrefab("MobilityPrefab", new Vector3(5f, 0f, 0f));
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
                        socketEuler: Vector3.zero,
                        hasFrameTopSocket: true,
                        frameTopSocketOffset: new Vector3(2f, 0.5f, 0f),
                        hasVisualBounds: true,
                        visualBoundsCenter: new Vector3(2f, 0f, 0f),
                        visualBoundsMin: new Vector3(1.5f, -0.5f, -0.5f),
                        visualBoundsMax: new Vector3(2.5f, 0.5f, 0.5f)),
                    firepowerAlignment: AutoOkAlignment(
                        socketOffset: Vector3.zero,
                        socketEuler: new Vector3(0f, 0f, 90f),
                        hasVisualBounds: true,
                        visualBoundsCenter: new Vector3(-3f, 0f, 0f),
                        visualBoundsMin: new Vector3(-3.5f, -0.5f, -0.5f),
                        visualBoundsMax: new Vector3(-2.5f, 0.5f, 0.5f),
                        assemblyAnchorMode: "FrameTopSocket"),
                    mobilityAlignment: AutoOkAlignment(
                        socketOffset: Vector3.zero,
                        hasGxTreeSocket: true,
                        gxTreeSocketOffset: new Vector3(0f, 0.5f, 0f),
                        gxTreeSocketName: "legs",
                        hasVisualBounds: true,
                        visualBoundsCenter: new Vector3(5f, 0f, 0f),
                        visualBoundsMin: new Vector3(4.5f, -0.5f, -0.5f),
                        visualBoundsMax: new Vector3(5.5f, 0.5f, 0.5f)),
                    mobilityUsesAssemblyPivot: false);

                Assert.IsTrue(GarageUnitPreviewAssembly.HasPreviewAssemblyData(viewModel));
                Assert.IsTrue(GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                    viewModel,
                    cameraObject.GetComponent<Camera>(),
                    framePrefab,
                    firepowerPrefab,
                    mobilityPrefab,
                    out previewRoot));

                var frame = previewRoot.transform.Find("FramePrefab(Clone)");
                var firepower = previewRoot.transform.Find("FirepowerPrefab(Clone)");
                var mobility = previewRoot.transform.Find("MobilityPrefab(Clone)");

                var frameBounds = CalculateLocalRendererBounds(previewRoot.transform, frame);
                var firepowerBounds = CalculateLocalRendererBounds(previewRoot.transform, firepower);
                Assert.That(frameBounds.center.x, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(firepowerBounds.min.y, Is.EqualTo(frameBounds.max.y).Within(0.0001f));
                Assert.That(firepowerBounds.center.x, Is.EqualTo(frameBounds.center.x).Within(0.0001f));
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
        public void TryCreatePreviewRoot_RejectsLegacyHandRigAdapterForDirectionOnlyFirepower()
        {
            var cameraObject = new GameObject("GaragePreviewCameraTest", typeof(Camera));
            var framePrefab = CreateBoxPrefab("FramePrefab", Vector3.zero, Vector3.one);
            var firepowerPrefab = CreateBoxPrefab("FirepowerPrefab", new Vector3(0f, 0f, 1.5f), new Vector3(0.5f, 0.5f, 3f));
            var mobilityPrefab = CreateBoxPrefab("MobilityPrefab", Vector3.zero, Vector3.one);
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
                        hasFrameTopSocket: true,
                        assemblyAnchorMode: "Disabled",
                        hasVisualBounds: true,
                        visualBoundsCenter: Vector3.zero,
                        visualBoundsMin: new Vector3(-0.5f, -0.5f, -0.5f),
                        visualBoundsMax: new Vector3(0.5f, 0.5f, 0.5f)),
                    firepowerAlignment: AutoOkAlignment(
                        socketOffset: Vector3.zero,
                        xfiSocketQuality: "xfi_weapon_direction_only",
                        hasVisualBounds: true,
                        visualBoundsCenter: new Vector3(0f, 0f, 1.5f),
                        visualBoundsMin: new Vector3(-0.25f, -0.25f, 0f),
                        visualBoundsMax: new Vector3(0.25f, 0.25f, 3f),
                        assemblyAnchorMode: "ObsoleteHumanoidHandRig",
                        assemblyReviewResult: "match"),
                    mobilityAlignment: AutoOkAlignment(
                        socketOffset: Vector3.zero,
                        hasGxTreeSocket: true,
                        gxTreeSocketOffset: new Vector3(0f, 0.5f, 0f),
                        gxTreeSocketName: "legs"),
                    mobilityUsesAssemblyPivot: false,
                    frameAssemblyForm: AssemblyForm.Humanoid,
                    firepowerAssemblyForm: AssemblyForm.Humanoid);

                Assert.IsFalse(GarageUnitPreviewAssembly.HasPreviewAssemblyData(viewModel));
                Assert.IsFalse(GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                    viewModel,
                    cameraObject.GetComponent<Camera>(),
                    framePrefab,
                    firepowerPrefab,
                    mobilityPrefab,
                    out previewRoot));
                Assert.IsNull(previewRoot);
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
        public void TryCreatePreviewRoot_RejectsDisabledDirectionOnlyFirepower()
        {
            var cameraObject = new GameObject("GaragePreviewCameraTest", typeof(Camera));
            var framePrefab = CreateBoxPrefab("FramePrefab", Vector3.zero, Vector3.one);
            var firepowerPrefab = CreateBoxPrefab("FirepowerPrefab", new Vector3(0f, 0f, 1.5f), new Vector3(0.5f, 0.5f, 3f));
            var mobilityPrefab = CreateBoxPrefab("MobilityPrefab", Vector3.zero, Vector3.one);
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
                        hasFrameTopSocket: true,
                        assemblyAnchorMode: "Disabled",
                        hasVisualBounds: true,
                        visualBoundsCenter: Vector3.zero,
                        visualBoundsMin: new Vector3(-0.5f, -0.5f, -0.5f),
                        visualBoundsMax: new Vector3(0.5f, 0.5f, 0.5f)),
                    firepowerAlignment: AutoOkAlignment(
                        socketOffset: Vector3.zero,
                        xfiSocketQuality: "xfi_weapon_direction_only",
                        hasVisualBounds: true,
                        visualBoundsCenter: new Vector3(0f, 0f, 1.5f),
                        visualBoundsMin: new Vector3(-0.25f, -0.25f, 0f),
                        visualBoundsMax: new Vector3(0.25f, 0.25f, 3f),
                        assemblyAnchorMode: "Disabled",
                        assemblyReviewResult: "pending"),
                    mobilityAlignment: AutoOkAlignment(
                        socketOffset: Vector3.zero,
                        hasGxTreeSocket: true,
                        gxTreeSocketOffset: new Vector3(0f, 0.5f, 0f),
                        gxTreeSocketName: "legs"),
                    mobilityUsesAssemblyPivot: false,
                    frameAssemblyForm: AssemblyForm.Humanoid,
                    firepowerAssemblyForm: AssemblyForm.Humanoid);

                Assert.IsFalse(GarageUnitPreviewAssembly.HasPreviewAssemblyData(viewModel));
                Assert.IsFalse(GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                    viewModel,
                    cameraObject.GetComponent<Camera>(),
                    framePrefab,
                    firepowerPrefab,
                    mobilityPrefab,
                    out previewRoot));
                Assert.IsNull(previewRoot);
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

        [TestCase("nova_frame_body26_kp", "nova_fire_arm32_sppoo", "nova_mob_legs21_ksor")]
        [TestCase("nova_frame_body26_kp", "nova_fire_arm24_bzk", "nova_mob_legs21_ksor")]
        [TestCase("nova_frame_body17_rzmt", "nova_fire_arm15_hdkn", "nova_mob_legs16_sprt")]
        [TestCase("nova_frame_body10_skdr", "nova_fire_arm39_hmsk", "nova_mob_legs24_sts")]
        [TestCase("nova_frame_body10_skdr", "nova_fire_arm29_sdbt", "nova_mob_legs7_hb")]
        public void GeneratedCatalog_ContainsOnlySupportedFrameFirepowerSamples(
            string frameId,
            string firepowerId,
            string mobilityId)
        {
            var catalog = BuildCatalog();

            Assert.IsNull(catalog.FindFrame(frameId));
            Assert.IsNull(catalog.FindFirepower(firepowerId));
            Assert.NotNull(catalog.FindMobility(mobilityId));
        }

        [Test]
        public void TryCreatePreviewRoot_RejectsDirectionOnlyFirepowerWithoutExplicitAssemblyAnchor()
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

                Assert.IsFalse(GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                    viewModel,
                    cameraObject.GetComponent<Camera>(),
                    framePrefab,
                    firepowerPrefab,
                    mobilityPrefab,
                    out previewRoot));

                Assert.IsNull(previewRoot);
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
        public void TryCreatePreviewRoot_RejectsDirectionOnlyFirepowerWithoutApprovedOriginalProfile()
        {
            var cameraObject = new GameObject("GaragePreviewCameraTest", typeof(Camera));
            var framePrefab = CreateBoxPrefab("FramePrefab", Vector3.zero, Vector3.one);
            var firepowerPrefab = CreateBoxPrefab("FirepowerPrefab", Vector3.zero, Vector3.one);
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
                        hasFrameTopSocket: true,
                        hasVisualBounds: true,
                        visualBoundsCenter: Vector3.zero,
                        visualBoundsMin: new Vector3(-0.5f, -0.5f, -0.5f),
                        visualBoundsMax: new Vector3(0.5f, 0.5f, 0.5f)),
                    firepowerAlignment: AutoOkAlignment(
                        socketOffset: Vector3.zero,
                        xfiSocketQuality: "xfi_weapon_direction_only",
                        hasVisualBounds: true,
                        visualBoundsCenter: Vector3.zero,
                        visualBoundsMin: new Vector3(-0.5f, -0.5f, -0.5f),
                        visualBoundsMax: new Vector3(0.5f, 0.5f, 0.5f)),
                    mobilityAlignment: AutoOkAlignment(
                        socketOffset: Vector3.zero,
                        hasGxTreeSocket: true,
                        gxTreeSocketOffset: new Vector3(0f, 0.5f, 0f),
                        gxTreeSocketName: "legs"),
                    mobilityUsesAssemblyPivot: false,
                    frameAssemblyForm: AssemblyForm.Humanoid,
                    firepowerAssemblyForm: AssemblyForm.Humanoid);

                Assert.IsFalse(GarageUnitPreviewAssembly.HasPreviewAssemblyData(viewModel));
                Assert.IsFalse(GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                    viewModel,
                    cameraObject.GetComponent<Camera>(),
                    framePrefab,
                    firepowerPrefab,
                    mobilityPrefab,
                    out previewRoot));

                Assert.IsNull(previewRoot);
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
        public void TryCreatePreviewRoot_UsesProfileAnchorForCenturionProlixDirectionOnlyShoulderLoadout()
        {
            var cameraObject = new GameObject("GaragePreviewCameraTest", typeof(Camera));
            GameObject previewRoot = null;

            try
            {
                var catalog = BuildCatalog();
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
                    mobilityUsesAssemblyPivot: mobility.UseAssemblyPivot,
                    frameAssemblyForm: frame.AssemblyForm,
                    firepowerAssemblyForm: firepower.AssemblyForm);

                Assert.That(frame.AssemblyForm, Is.EqualTo(AssemblyForm.Shoulder));
                Assert.That(firepower.AssemblyForm, Is.EqualTo(AssemblyForm.Shoulder));
                Assert.That(firepower.Alignment.XfiSocketQuality, Is.EqualTo("xfi_weapon_direction_only"));
                Assert.That(frame.Alignment.AssemblyAnchorMode, Is.EqualTo("ShoulderPair"));
                Assert.That(firepower.Alignment.AssemblyAnchorMode, Is.EqualTo("ShoulderPair"));
                Assert.IsTrue(GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                    viewModel,
                    cameraObject.GetComponent<Camera>(),
                    frame.AssemblyPrefab,
                    firepower.AssemblyPrefab,
                    mobility.AssemblyPrefab,
                    out previewRoot));
                Assert.NotNull(previewRoot);
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
            string xfiSocketQuality = null,
            bool hasVisualBounds = false,
            Vector3 visualBoundsCenter = default,
            Vector3 visualBoundsMin = default,
            Vector3 visualBoundsMax = default,
            string assemblyAnchorMode = null,
            string assemblyReviewResult = null)
        {
            return new GaragePanelCatalog.PartAlignment
            {
                HasVisualBounds = hasVisualBounds,
                VisualBoundsCenter = visualBoundsCenter,
                VisualBoundsMin = visualBoundsMin,
                VisualBoundsMax = visualBoundsMax,
                SocketOffset = socketOffset,
                SocketEuler = socketEuler,
                HasFrameTopSocket = hasFrameTopSocket,
                FrameTopSocketOffset = frameTopSocketOffset,
                HasGxTreeSocket = hasGxTreeSocket,
                GxTreeSocketOffset = gxTreeSocketOffset,
                GxTreeSocketName = gxTreeSocketName,
                XfiSocketQuality = xfiSocketQuality,
                AssemblyAnchorMode = assemblyAnchorMode,
                AssemblyReviewResult = assemblyReviewResult,
                QualityFlag = "auto_ok"
            };
        }

        private static GaragePanelCatalog BuildCatalog()
        {
            var moduleCatalog = AssetDatabase.LoadAssetAtPath<ModuleCatalog>(ModuleCatalogPath);
            var visualCatalog = AssetDatabase.LoadAssetAtPath<NovaPartVisualCatalog>(VisualCatalogPath);
            var alignmentCatalog = AssetDatabase.LoadAssetAtPath<NovaPartAlignmentCatalog>(AlignmentCatalogPath);

            Assert.NotNull(moduleCatalog);
            Assert.NotNull(visualCatalog);
            Assert.NotNull(alignmentCatalog);

            return new GaragePanelCatalogFactory().Build(
                moduleCatalog,
                visualCatalog,
                alignmentCatalog);
        }

        private static GameObject CreateOffsetCubePrefab(string name, Vector3 localCenter)
        {
            var root = new GameObject(name);
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "VisibleMesh";
            cube.transform.SetParent(root.transform, false);
            cube.transform.localPosition = localCenter;
            return root;
        }

        private static GameObject CreateBoxPrefab(string name, Vector3 localCenter, Vector3 localScale)
        {
            var root = new GameObject(name);
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "VisibleMesh";
            cube.transform.SetParent(root.transform, false);
            cube.transform.localPosition = localCenter;
            cube.transform.localScale = localScale;
            return root;
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
