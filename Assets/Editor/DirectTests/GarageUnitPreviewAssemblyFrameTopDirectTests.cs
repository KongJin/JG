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
                        hasVisualBounds: true,
                        visualBoundsCenter: new Vector3(5f, 0f, 0f),
                        visualBoundsMin: new Vector3(4.5f, -0.5f, -0.5f),
                        visualBoundsMax: new Vector3(5.5f, 0.5f, 0.5f)),
                    mobilityUsesAssemblyPivot: false);

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
                var mobilityBounds = CalculateLocalRendererBounds(previewRoot.transform, mobility);

                Assert.That(frameBounds.center.x, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(firepowerBounds.min.y, Is.EqualTo(frameBounds.max.y).Within(0.0001f));
                Assert.That(firepowerBounds.center.x, Is.EqualTo(frameBounds.center.x).Within(0.0001f));
                Assert.That(mobilityBounds.max.y, Is.EqualTo(frameBounds.min.y).Within(0.0001f));
                Assert.That(mobilityBounds.center.x, Is.EqualTo(frameBounds.center.x).Within(0.0001f));
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
        public void TryCreatePreviewRoot_UsesCanonicalHumanoidRigForDirectionOnlyFirepower()
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
                        assemblyAnchorMode: "HumanoidShellRearToFrameFront"),
                    mobilityAlignment: AutoOkAlignment(
                        socketOffset: Vector3.zero,
                        hasGxTreeSocket: true,
                        gxTreeSocketOffset: new Vector3(0f, 0.5f, 0f),
                        gxTreeSocketName: "legs"),
                    mobilityUsesAssemblyPivot: false,
                    frameAssemblyForm: AssemblyForm.Humanoid,
                    firepowerAssemblyForm: AssemblyForm.Humanoid);

                Assert.IsTrue(GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                    viewModel,
                    cameraObject.GetComponent<Camera>(),
                    framePrefab,
                    firepowerPrefab,
                    mobilityPrefab,
                    out previewRoot));

                var frame = previewRoot.transform.Find("FramePrefab(Clone)");
                var firepower = previewRoot.transform.Find("FirepowerPrefab(Clone)");

                Assert.NotNull(frame);
                Assert.NotNull(firepower);

                var frameBounds = CalculateLocalRendererBounds(previewRoot.transform, frame);
                var firepowerBounds = CalculateLocalRendererBounds(previewRoot.transform, firepower);

                AssertCanonicalHumanoidRigPlacement(frameBounds, firepowerBounds);
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
        public void TryCreatePreviewRoot_UsesCanonicalHumanoidRigForKingpinSpitfire()
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
                Assert.AreEqual(AssemblyForm.Humanoid, frame.AssemblyForm);
                Assert.AreEqual(AssemblyForm.Humanoid, firepower.AssemblyForm);
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
                    mobilityUsesAssemblyPivot: mobility.UseAssemblyPivot,
                    frameAssemblyForm: frame.AssemblyForm,
                    firepowerAssemblyForm: firepower.AssemblyForm);

                Assert.IsTrue(GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                    viewModel,
                    cameraObject.GetComponent<Camera>(),
                    frame.AssemblyPrefab,
                    firepower.AssemblyPrefab,
                    mobility.AssemblyPrefab,
                    out previewRoot));

                var firepowerObject = FindDirectChild(previewRoot.transform, firepower.Id);
                var frameObject = FindDirectChild(previewRoot.transform, frame.Id);
                var mobilityObject = FindDirectChild(previewRoot.transform, mobility.Id);

                Assert.NotNull(firepowerObject);
                Assert.NotNull(frameObject);
                Assert.NotNull(mobilityObject);

                var firepowerBounds = CalculateLocalRendererBounds(previewRoot.transform, firepowerObject);
                var frameBounds = CalculateLocalRendererBounds(previewRoot.transform, frameObject);
                var mobilityBounds = CalculateLocalRendererBounds(previewRoot.transform, mobilityObject);

                Assert.That(firepower.Alignment.XfiSocketQuality, Is.EqualTo("xfi_weapon_direction_only"));
                AssertCanonicalHumanoidRigPlacement(frameBounds, firepowerBounds);
                Assert.That(mobilityBounds.max.y, Is.GreaterThan(frameBounds.min.y));
            }
            finally
            {
                if (previewRoot != null)
                    Object.DestroyImmediate(previewRoot);

                Object.DestroyImmediate(cameraObject);
            }
        }

        [TestCase("nova_fire_arm24_bzk")]
        [TestCase("nova_fire_arm29_sdbt")]
        [TestCase("nova_fire_s_arm52_bzk")]
        public void TryCreatePreviewRoot_UsesCanonicalHumanoidRigForKnownProtrudingFirepower(string firepowerId)
        {
            var moduleCatalog = AssetDatabase.LoadAssetAtPath<ModuleCatalog>(ModuleCatalogPath);
            var visualCatalog = AssetDatabase.LoadAssetAtPath<NovaPartVisualCatalog>(VisualCatalogPath);
            var alignmentCatalog = AssetDatabase.LoadAssetAtPath<NovaPartAlignmentCatalog>(AlignmentCatalogPath);
            var cameraObject = new GameObject("GaragePreviewCameraTest", typeof(Camera));
            GameObject previewRoot = null;

            try
            {
                var catalog = new GaragePanelCatalogFactory().Build(
                    moduleCatalog,
                    visualCatalog,
                    alignmentCatalog);
                var frame = catalog.FindFrame("nova_frame_body26_kp");
                var firepower = catalog.FindFirepower(firepowerId);
                var mobility = catalog.FindMobility("nova_mob_legs21_ksor");

                Assert.NotNull(frame);
                Assert.NotNull(firepower);
                Assert.NotNull(mobility);
                Assert.That(firepower.Alignment.XfiSocketQuality, Is.EqualTo("xfi_weapon_direction_only"));

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

                Assert.IsTrue(GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                    viewModel,
                    cameraObject.GetComponent<Camera>(),
                    frame.AssemblyPrefab,
                    firepower.AssemblyPrefab,
                    mobility.AssemblyPrefab,
                    out previewRoot));

                var frameObject = FindDirectChild(previewRoot.transform, frame.Id);
                var firepowerObject = FindDirectChild(previewRoot.transform, firepower.Id);

                Assert.NotNull(frameObject);
                Assert.NotNull(firepowerObject);

                var frameBounds = CalculateLocalRendererBounds(previewRoot.transform, frameObject);
                var firepowerBounds = CalculateLocalRendererBounds(previewRoot.transform, firepowerObject);

                AssertCanonicalHumanoidRigPlacement(frameBounds, firepowerBounds, zTolerance: 0.02f);
            }
            finally
            {
                if (previewRoot != null)
                    Object.DestroyImmediate(previewRoot);

                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void TryCreatePreviewRoot_RejectsDirectionOnlyFirepowerOutsideCanonicalHumanoidRig()
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
                    Is.EqualTo(ResolveEffectiveFrameTopY(frame)).Within(0.02f));
            }
            finally
            {
                if (previewRoot != null)
                    Object.DestroyImmediate(previewRoot);

                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void TryCreatePreviewRoot_UsesCanonicalHumanoidRigForRegimentHandcannon()
        {
            var moduleCatalog = AssetDatabase.LoadAssetAtPath<ModuleCatalog>(ModuleCatalogPath);
            var visualCatalog = AssetDatabase.LoadAssetAtPath<NovaPartVisualCatalog>(VisualCatalogPath);
            var alignmentCatalog = AssetDatabase.LoadAssetAtPath<NovaPartAlignmentCatalog>(AlignmentCatalogPath);
            var cameraObject = new GameObject("GaragePreviewCameraTest", typeof(Camera));
            GameObject previewRoot = null;

            try
            {
                var catalog = new GaragePanelCatalogFactory().Build(
                    moduleCatalog,
                    visualCatalog,
                    alignmentCatalog);
                var frame = catalog.FindFrame("nova_frame_body17_rzmt");
                var firepower = catalog.FindFirepower("nova_fire_arm15_hdkn");
                var mobility = catalog.FindMobility("nova_mob_legs16_sprt");

                Assert.NotNull(frame);
                Assert.NotNull(firepower);
                Assert.NotNull(mobility);

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

                Assert.IsTrue(GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                    viewModel,
                    cameraObject.GetComponent<Camera>(),
                    frame.AssemblyPrefab,
                    firepower.AssemblyPrefab,
                    mobility.AssemblyPrefab,
                    out previewRoot));

                var frameObject = FindDirectChild(previewRoot.transform, frame.Id);
                var firepowerObject = FindDirectChild(previewRoot.transform, firepower.Id);

                Assert.NotNull(frameObject);
                Assert.NotNull(firepowerObject);

                var frameBounds = CalculateLocalRendererBounds(previewRoot.transform, frameObject);
                var firepowerBounds = CalculateLocalRendererBounds(previewRoot.transform, firepowerObject);
                Assert.That(firepower.Alignment.XfiSocketQuality, Is.EqualTo("xfi_weapon_direction_only"));
                AssertCanonicalHumanoidRigPlacement(frameBounds, firepowerBounds);
            }
            finally
            {
                if (previewRoot != null)
                    Object.DestroyImmediate(previewRoot);

                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void TryCreatePreviewRoot_UsesCanonicalHumanoidRigForSquadronHammerShock()
        {
            var moduleCatalog = AssetDatabase.LoadAssetAtPath<ModuleCatalog>(ModuleCatalogPath);
            var visualCatalog = AssetDatabase.LoadAssetAtPath<NovaPartVisualCatalog>(VisualCatalogPath);
            var alignmentCatalog = AssetDatabase.LoadAssetAtPath<NovaPartAlignmentCatalog>(AlignmentCatalogPath);
            var cameraObject = new GameObject("GaragePreviewCameraTest", typeof(Camera));
            GameObject previewRoot = null;

            try
            {
                var catalog = new GaragePanelCatalogFactory().Build(
                    moduleCatalog,
                    visualCatalog,
                    alignmentCatalog);
                var frame = catalog.FindFrame("nova_frame_body10_skdr");
                var firepower = catalog.FindFirepower("nova_fire_arm39_hmsk");
                var mobility = catalog.FindMobility("nova_mob_legs24_sts");

                Assert.NotNull(frame);
                Assert.NotNull(firepower);
                Assert.NotNull(mobility);
                Assert.AreEqual(AssemblyForm.Humanoid, frame.AssemblyForm);
                Assert.AreEqual(AssemblyForm.Humanoid, firepower.AssemblyForm);

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

                Assert.IsTrue(GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                    viewModel,
                    cameraObject.GetComponent<Camera>(),
                    frame.AssemblyPrefab,
                    firepower.AssemblyPrefab,
                    mobility.AssemblyPrefab,
                    out previewRoot));

                var frameObject = FindDirectChild(previewRoot.transform, frame.Id);
                var firepowerObject = FindDirectChild(previewRoot.transform, firepower.Id);

                Assert.NotNull(frameObject);
                Assert.NotNull(firepowerObject);

                var frameBounds = CalculateLocalRendererBounds(previewRoot.transform, frameObject);
                var firepowerBounds = CalculateLocalRendererBounds(previewRoot.transform, firepowerObject);

                Assert.That(firepower.Alignment.XfiSocketQuality, Is.EqualTo("xfi_weapon_direction_only"));
                AssertCanonicalHumanoidRigPlacement(frameBounds, firepowerBounds);
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
            string assemblyAnchorMode = null)
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
                QualityFlag = "auto_ok"
            };
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

        private static void AssertCanonicalHumanoidRigPlacement(
            Bounds frameBounds,
            Bounds firepowerBounds,
            float zTolerance = 0.0001f)
        {
            Assert.That(
                firepowerBounds.min.x,
                Is.GreaterThan(frameBounds.max.x + frameBounds.size.x * 0.2f));
            Assert.That(
                firepowerBounds.min.z,
                Is.EqualTo(frameBounds.max.z + frameBounds.size.z * 0.12f).Within(zTolerance));
            Assert.That(firepowerBounds.max.y, Is.GreaterThan(frameBounds.min.y));
            Assert.That(firepowerBounds.min.y, Is.LessThan(frameBounds.max.y));
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

        private static float ResolveEffectiveFrameTopY(GaragePanelCatalog.FrameOption frame)
        {
            var framePositionY = frame.Alignment.HasVisualBounds
                ? frame.Alignment.PivotOffset.y - frame.Alignment.VisualBoundsCenter.y
                : frame.Alignment.PivotOffset.y;
            var top = frame.Alignment.FrameTopSocketOffset.sqrMagnitude > 0.000001f
                ? frame.Alignment.FrameTopSocketOffset
                : frame.Alignment.SocketOffset;
            return framePositionY + top.y;
        }
    }
}
