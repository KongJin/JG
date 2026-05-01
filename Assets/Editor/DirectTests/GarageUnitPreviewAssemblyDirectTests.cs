using Features.Garage;
using Features.Garage.Presentation;
using Features.Garage.Infrastructure;
using Features.Unit.Infrastructure;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class GarageUnitPreviewAssemblyDirectTests
    {
        private const string ModuleCatalogPath = "Assets/Data/Garage/ModuleCatalog.asset";
        private const string VisualCatalogPath = "Assets/Data/Garage/NovaGenerated/NovaPartVisualCatalog.asset";
        private const string AlignmentCatalogPath = "Assets/Data/Garage/NovaGenerated/NovaPartAlignmentCatalog.asset";

        [Test]
        public void CatalogFactory_DoesNotTreatGeneratedMobilityAssemblyPrefabsAsSocketPivots()
        {
            var moduleCatalog = AssetDatabase.LoadAssetAtPath<ModuleCatalog>(ModuleCatalogPath);
            var visualCatalog = AssetDatabase.LoadAssetAtPath<NovaPartVisualCatalog>(VisualCatalogPath);
            var alignmentCatalog = AssetDatabase.LoadAssetAtPath<NovaPartAlignmentCatalog>(AlignmentCatalogPath);

            Assert.NotNull(moduleCatalog);
            Assert.NotNull(visualCatalog);
            Assert.NotNull(alignmentCatalog);

            var catalog = new GaragePanelCatalogFactory().Build(
                moduleCatalog,
                visualCatalog,
                alignmentCatalog);

            AssertMobilityUsesCatalogSocket(catalog, "nova_mob_legs34_dpns");
            AssertMobilityUsesCatalogSocket(catalog, "nova_mob_legs3_ktpr");
            AssertMobilityUsesCatalogSocket(catalog, "nova_mob_legs20_spod");
            AssertMobilityHasGxTreeSocket(catalog, "nova_mob_legs20_spod");
        }

        [Test]
        public void CatalogFactory_ProvidesAssemblyPrefabAndSocketsForEveryGeneratedMobilityPart()
        {
            var moduleCatalog = AssetDatabase.LoadAssetAtPath<ModuleCatalog>(ModuleCatalogPath);
            var visualCatalog = AssetDatabase.LoadAssetAtPath<NovaPartVisualCatalog>(VisualCatalogPath);
            var alignmentCatalog = AssetDatabase.LoadAssetAtPath<NovaPartAlignmentCatalog>(AlignmentCatalogPath);

            Assert.NotNull(moduleCatalog);
            Assert.NotNull(visualCatalog);
            Assert.NotNull(alignmentCatalog);

            var catalog = new GaragePanelCatalogFactory().Build(
                moduleCatalog,
                visualCatalog,
                alignmentCatalog);

            Assert.That(catalog.Mobility.Count, Is.EqualTo(49));

            for (int i = 0; i < catalog.Mobility.Count; i++)
            {
                var mobility = catalog.Mobility[i];

                Assert.NotNull(mobility, $"mobility[{i}]");
                Assert.NotNull(mobility.AssemblyPrefab, mobility.Id);
                Assert.IsFalse(mobility.UseAssemblyPivot, mobility.Id);
                Assert.NotNull(mobility.Alignment, mobility.Id);
                Assert.AreEqual("auto_ok", mobility.Alignment.QualityFlag, mobility.Id);
                Assert.IsTrue(mobility.Alignment.HasXfiAttachSocket, mobility.Id);
                Assert.AreEqual("legs.body", mobility.Alignment.XfiSocketName, mobility.Id);
                Assert.IsTrue(mobility.Alignment.HasGxTreeSocket, mobility.Id);
                Assert.AreEqual("legs", mobility.Alignment.GxTreeSocketName, mobility.Id);
            }
        }

        [Test]
        public void CatalogFactory_ProvidesAssemblyDataForGeneratedSampleLoadout()
        {
            var moduleCatalog = AssetDatabase.LoadAssetAtPath<ModuleCatalog>(ModuleCatalogPath);
            var visualCatalog = AssetDatabase.LoadAssetAtPath<NovaPartVisualCatalog>(VisualCatalogPath);
            var alignmentCatalog = AssetDatabase.LoadAssetAtPath<NovaPartAlignmentCatalog>(AlignmentCatalogPath);

            Assert.NotNull(moduleCatalog);
            Assert.NotNull(visualCatalog);
            Assert.NotNull(alignmentCatalog);

            var catalog = new GaragePanelCatalogFactory().Build(
                moduleCatalog,
                visualCatalog,
                alignmentCatalog);
            var frame = catalog.FindFrame("nova_frame_body25_bosro");
            var firepower = catalog.FindFirepower("nova_fire_arm13_prs");
            var mobility = catalog.FindMobility("nova_mob_legs1_rdrn");

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
                frameId: frame?.Id,
                firepowerId: firepower?.Id,
                mobilityId: mobility?.Id,
                frameAlignment: frame?.Alignment,
                firepowerAlignment: firepower?.Alignment,
                mobilityAlignment: mobility?.Alignment,
                mobilityUsesAssemblyPivot: mobility?.UseAssemblyPivot ?? false);

            Assert.IsTrue(GarageUnitPreviewAssembly.HasPreviewAssemblyData(viewModel));
        }

        [Test]
        public void TryCreatePreviewRoot_PrefersMobilityGxTreeSocketForAssemblyPrefab()
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
                    firepowerId: "fire",
                    mobilityId: "mobility",
                    frameAlignment: AutoOkAlignment(Vector3.zero, hasFrameTopSocket: true),
                    firepowerAlignment: AutoOkAlignment(Vector3.zero),
                    mobilityAlignment: AutoOkAlignment(
                        new Vector3(0f, 0.34f, 0f),
                        hasGxTreeSocket: true,
                        gxTreeSocketOffset: new Vector3(0f, 0.62f, 0f),
                        gxTreeSocketName: "legs",
                        hasXfiAttachSocket: true,
                        xfiAttachSocketOffset: new Vector3(0f, 0.02f, 0f)),
                    mobilityUsesAssemblyPivot: false);

                Assert.IsTrue(GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                    viewModel,
                    cameraObject.GetComponent<Camera>(),
                    framePrefab,
                    firepowerPrefab,
                    mobilityPrefab,
                    out previewRoot));

                var mobility = previewRoot.transform.Find("MobilityPrefab(Clone)");

                Assert.NotNull(mobility);
                Assert.That(mobility.localPosition.y, Is.EqualTo(-0.62f).Within(0.0001f));
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
        public void TryCreatePreviewRoot_FallsBackToMobilityGxTreeSocketWhenXfiAttachSocketIsAbsent()
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
                    firepowerId: "fire",
                    mobilityId: "mobility",
                    frameAlignment: AutoOkAlignment(Vector3.zero, hasFrameTopSocket: true),
                    firepowerAlignment: AutoOkAlignment(Vector3.zero),
                    mobilityAlignment: AutoOkAlignment(
                        new Vector3(0f, 0.34f, 0f),
                        hasGxTreeSocket: true,
                        gxTreeSocketOffset: new Vector3(0f, 0.62f, 0f),
                        gxTreeSocketName: "legs"),
                    mobilityUsesAssemblyPivot: false);

                Assert.IsTrue(GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                    viewModel,
                    cameraObject.GetComponent<Camera>(),
                    framePrefab,
                    firepowerPrefab,
                    mobilityPrefab,
                    out previewRoot));

                var mobility = previewRoot.transform.Find("MobilityPrefab(Clone)");

                Assert.NotNull(mobility);
                Assert.That(mobility.localPosition.y, Is.EqualTo(-0.62f).Within(0.0001f));
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
        public void TryCreatePreviewRoot_FallsBackToMobilitySocketOffsetWhenGxTreeSocketIsAbsent()
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
                    firepowerId: "fire",
                    mobilityId: "mobility",
                    frameAlignment: AutoOkAlignment(Vector3.zero, hasFrameTopSocket: true),
                    firepowerAlignment: AutoOkAlignment(Vector3.zero),
                    mobilityAlignment: AutoOkAlignment(new Vector3(0f, 0.34f, 0f)),
                    mobilityUsesAssemblyPivot: false);

                Assert.IsTrue(GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                    viewModel,
                    cameraObject.GetComponent<Camera>(),
                    framePrefab,
                    firepowerPrefab,
                    mobilityPrefab,
                    out previewRoot));

                var mobility = previewRoot.transform.Find("MobilityPrefab(Clone)");

                Assert.NotNull(mobility);
                Assert.That(mobility.localPosition.y, Is.EqualTo(-0.34f).Within(0.0001f));
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
        public void TryCreatePreviewRoot_UsesPromotedSpiderGxTreeSocket()
        {
            var moduleCatalog = AssetDatabase.LoadAssetAtPath<ModuleCatalog>(ModuleCatalogPath);
            var visualCatalog = AssetDatabase.LoadAssetAtPath<NovaPartVisualCatalog>(VisualCatalogPath);
            var alignmentCatalog = AssetDatabase.LoadAssetAtPath<NovaPartAlignmentCatalog>(AlignmentCatalogPath);
            var cameraObject = new GameObject("GaragePreviewCameraTest", typeof(Camera));
            var framePrefab = new GameObject("FramePrefab");
            var firepowerPrefab = new GameObject("FirepowerPrefab");
            var mobilityPrefab = new GameObject("MobilityPrefab");
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
                var mobility = catalog.FindMobility("nova_mob_legs20_spod");
                Assert.NotNull(mobility);
                Assert.NotNull(mobility.Alignment);
                Assert.IsTrue(mobility.Alignment.HasXfiAttachSocket);
                Assert.IsTrue(mobility.Alignment.HasGxTreeSocket);

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
                    firepowerId: "fire",
                    mobilityId: mobility.Id,
                    frameAlignment: AutoOkAlignment(Vector3.zero, hasFrameTopSocket: true),
                    firepowerAlignment: AutoOkAlignment(Vector3.zero),
                    mobilityAlignment: mobility.Alignment,
                    mobilityUsesAssemblyPivot: false);

                Assert.IsTrue(GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                    viewModel,
                    cameraObject.GetComponent<Camera>(),
                    framePrefab,
                    firepowerPrefab,
                    mobilityPrefab,
                    out previewRoot));

                var mobilityObject = previewRoot.transform.Find("MobilityPrefab(Clone)");

                Assert.NotNull(mobilityObject);
                Assert.That(
                    mobilityObject.localPosition.y,
                    Is.EqualTo(-mobility.Alignment.GxTreeSocketOffset.y).Within(0.000001f));
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
        public void TryCreatePreviewRoot_StacksRoadRunnerKomodoNemesisInVerticalOrder()
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
                var frame = catalog.FindFrame("nova_frame_body16_kmdo");
                var firepower = catalog.FindFirepower("nova_fire_arm21_nmsz");
                var mobility = catalog.FindMobility("nova_mob_legs1_rdrn");

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

                var frameObject = FindDirectChild(previewRoot.transform, "nova_frame_body16_kmdo");
                var firepowerObject = FindDirectChild(previewRoot.transform, "nova_fire_arm21_nmsz");
                var mobilityObject = FindDirectChild(previewRoot.transform, "nova_mob_legs1_rdrn");

                Assert.NotNull(frameObject);
                Assert.NotNull(firepowerObject);
                Assert.NotNull(mobilityObject);

                var frameBounds = CalculateLocalRendererBounds(previewRoot.transform, frameObject);
                var firepowerBounds = CalculateLocalRendererBounds(previewRoot.transform, firepowerObject);
                var mobilityBounds = CalculateLocalRendererBounds(previewRoot.transform, mobilityObject);

                Assert.That(frameBounds.min.y, Is.EqualTo(mobilityBounds.max.y).Within(0.02f));
                Assert.That(firepowerBounds.center.y, Is.GreaterThan(frameBounds.center.y));
            }
            finally
            {
                if (previewRoot != null)
                    Object.DestroyImmediate(previewRoot);

                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void TryCreatePreviewRoot_UsesPromotedGxTreeSocketForEveryGeneratedMobilityPart()
        {
            var moduleCatalog = AssetDatabase.LoadAssetAtPath<ModuleCatalog>(ModuleCatalogPath);
            var visualCatalog = AssetDatabase.LoadAssetAtPath<NovaPartVisualCatalog>(VisualCatalogPath);
            var alignmentCatalog = AssetDatabase.LoadAssetAtPath<NovaPartAlignmentCatalog>(AlignmentCatalogPath);
            var cameraObject = new GameObject("GaragePreviewCameraTest", typeof(Camera));
            var framePrefab = new GameObject("FramePrefab");
            var firepowerPrefab = new GameObject("FirepowerPrefab");

            try
            {
                Assert.NotNull(moduleCatalog);
                Assert.NotNull(visualCatalog);
                Assert.NotNull(alignmentCatalog);

                var catalog = new GaragePanelCatalogFactory().Build(
                    moduleCatalog,
                    visualCatalog,
                    alignmentCatalog);

                Assert.That(catalog.Mobility.Count, Is.EqualTo(49));

                for (int i = 0; i < catalog.Mobility.Count; i++)
                {
                    var mobility = catalog.Mobility[i];
                    var mobilityPrefab = new GameObject("MobilityPrefab");
                    GameObject previewRoot = null;

                    try
                    {
                        Assert.NotNull(mobility.Alignment, mobility.Id);
                        Assert.IsTrue(mobility.Alignment.HasGxTreeSocket, mobility.Id);

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
                            firepowerId: "fire",
                            mobilityId: mobility.Id,
                            frameAlignment: AutoOkAlignment(Vector3.zero, hasFrameTopSocket: true),
                            firepowerAlignment: AutoOkAlignment(Vector3.zero),
                            mobilityAlignment: mobility.Alignment,
                            mobilityUsesAssemblyPivot: false);

                        Assert.IsTrue(
                            GarageUnitPreviewAssembly.TryCreatePreviewRoot(
                                viewModel,
                                cameraObject.GetComponent<Camera>(),
                                framePrefab,
                                firepowerPrefab,
                                mobilityPrefab,
                                out previewRoot),
                            mobility.Id);

                        var mobilityObject = previewRoot.transform.Find("MobilityPrefab(Clone)");

                        Assert.NotNull(mobilityObject, mobility.Id);
                        Assert.That(
                            mobilityObject.localPosition.x,
                            Is.EqualTo(-mobility.Alignment.GxTreeSocketOffset.x).Within(0.000001f),
                            mobility.Id);
                        Assert.That(
                            mobilityObject.localPosition.y,
                            Is.EqualTo(-mobility.Alignment.GxTreeSocketOffset.y).Within(0.000001f),
                            mobility.Id);
                        Assert.That(
                            mobilityObject.localPosition.z,
                            Is.EqualTo(-mobility.Alignment.GxTreeSocketOffset.z).Within(0.000001f),
                            mobility.Id);
                    }
                    finally
                    {
                        if (previewRoot != null)
                            Object.DestroyImmediate(previewRoot);

                        Object.DestroyImmediate(mobilityPrefab);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(framePrefab);
                Object.DestroyImmediate(firepowerPrefab);
            }
        }

        [Test]
        public void HasPreviewAssemblyData_RequiresFrameTopSocket()
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
                firepowerId: "fire",
                mobilityId: "mobility",
                frameAlignment: AutoOkAlignment(Vector3.zero, hasFrameTopSocket: false),
                firepowerAlignment: AutoOkAlignment(Vector3.zero),
                mobilityAlignment: AutoOkAlignment(new Vector3(0f, 0.34f, 0f)),
                mobilityUsesAssemblyPivot: false);

            Assert.IsFalse(GarageUnitPreviewAssembly.HasPreviewAssemblyData(viewModel));
        }

        [Test]
        public void TryCreatePreviewRoot_DoesNotUseImplicitPoseWhenAssemblyDataIsMissing()
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
                    firepowerId: "fire",
                    mobilityId: "mobility",
                    frameAlignment: AutoOkAlignment(Vector3.zero, hasFrameTopSocket: true),
                    firepowerAlignment: AutoOkAlignment(Vector3.zero),
                    mobilityAlignment: AutoOkAlignment(Vector3.zero),
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

        private static GaragePanelCatalog.PartAlignment AutoOkAlignment(
            Vector3 socketOffset,
            bool hasFrameTopSocket = false,
            bool hasGxTreeSocket = false,
            Vector3 gxTreeSocketOffset = default,
            string gxTreeSocketName = null,
            bool hasXfiAttachSocket = false,
            Vector3 xfiAttachSocketOffset = default)
        {
            return new GaragePanelCatalog.PartAlignment
            {
                SocketOffset = socketOffset,
                HasGxTreeSocket = hasGxTreeSocket,
                GxTreeSocketOffset = gxTreeSocketOffset,
                GxTreeSocketName = gxTreeSocketName,
                HasXfiAttachSocket = hasXfiAttachSocket,
                XfiAttachSocketOffset = xfiAttachSocketOffset,
                HasFrameTopSocket = hasFrameTopSocket,
                FrameTopSocketOffset = Vector3.zero,
                QualityFlag = "auto_ok"
            };
        }

        private static void AssertMobilityUsesCatalogSocket(GaragePanelCatalog catalog, string partId)
        {
            var mobility = catalog.FindMobility(partId);

            Assert.NotNull(mobility, partId);
            Assert.NotNull(mobility.AssemblyPrefab, partId);
            Assert.NotNull(mobility.Alignment, partId);
            Assert.IsFalse(mobility.UseAssemblyPivot, partId);
            Assert.That(mobility.Alignment.SocketOffset.sqrMagnitude, Is.GreaterThan(0.000001f), partId);
        }

        private static void AssertMobilityHasGxTreeSocket(GaragePanelCatalog catalog, string partId)
        {
            var mobility = catalog.FindMobility(partId);

            Assert.NotNull(mobility, partId);
            Assert.NotNull(mobility.Alignment, partId);
            Assert.IsTrue(mobility.Alignment.HasGxTreeSocket, partId);
            Assert.AreEqual("legs", mobility.Alignment.GxTreeSocketName, partId);
            Assert.That(mobility.Alignment.GxTreeSocketOffset.y, Is.EqualTo(0.622261f).Within(0.000001f), partId);
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
