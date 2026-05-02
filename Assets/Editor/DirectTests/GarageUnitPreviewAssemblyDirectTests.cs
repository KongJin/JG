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
            var catalog = BuildCatalog();

            AssertMobilityUsesCatalogSocket(catalog, "nova_mob_legs34_dpns");
            AssertMobilityUsesCatalogSocket(catalog, "nova_mob_legs3_ktpr");
            AssertMobilityUsesCatalogSocket(catalog, "nova_mob_legs20_spod");
            AssertMobilityHasGxTreeSocket(catalog, "nova_mob_legs20_spod");
        }

        [Test]
        public void CatalogFactory_ProvidesAssemblyPrefabAndSocketsForEveryGeneratedMobilityPart()
        {
            var catalog = BuildCatalog();

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

        [TestCase(true, true, 0.62f, 0.02f, 0.62f)]
        [TestCase(true, false, 0.62f, 0f, 0.62f)]
        [TestCase(false, false, 0f, 0f, 0.34f)]
        public void TryCreatePreviewRoot_UsesExpectedMobilitySocket(
            bool hasGxTreeSocket,
            bool hasXfiAttachSocket,
            float gxTreeSocketY,
            float xfiAttachSocketY,
            float expectedSocketY)
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
                        hasGxTreeSocket: hasGxTreeSocket,
                        gxTreeSocketOffset: new Vector3(0f, gxTreeSocketY, 0f),
                        gxTreeSocketName: hasGxTreeSocket ? "legs" : null,
                        hasXfiAttachSocket: hasXfiAttachSocket,
                        xfiAttachSocketOffset: new Vector3(0f, xfiAttachSocketY, 0f)),
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
                Assert.That(mobility.localPosition.y, Is.EqualTo(-expectedSocketY).Within(0.0001f));
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
            var cameraObject = new GameObject("GaragePreviewCameraTest", typeof(Camera));
            var framePrefab = new GameObject("FramePrefab");
            var firepowerPrefab = new GameObject("FirepowerPrefab");
            var mobilityPrefab = new GameObject("MobilityPrefab");
            GameObject previewRoot = null;

            try
            {
                var catalog = BuildCatalog();
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
        public void TryCreatePreviewRoot_UsesProfileAnchorForRoadRunnerKomodoNemesisDirectionOnlyTowerLoadout()
        {
            var cameraObject = new GameObject("GaragePreviewCameraTest", typeof(Camera));
            GameObject previewRoot = null;

            try
            {
                var catalog = BuildCatalog();
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
                    mobilityUsesAssemblyPivot: mobility.UseAssemblyPivot,
                    frameAssemblyForm: frame.AssemblyForm,
                    firepowerAssemblyForm: firepower.AssemblyForm);

                Assert.That(frame.AssemblyForm, Is.EqualTo(AssemblyForm.Tower));
                Assert.That(firepower.AssemblyForm, Is.EqualTo(AssemblyForm.Tower));
                Assert.That(firepower.Alignment.XfiSocketQuality, Is.EqualTo("xfi_weapon_direction_only"));
                Assert.That(firepower.Alignment.AssemblyAnchorMode, Is.EqualTo("FrameTopSocket"));
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

        [Test]
        public void TryCreatePreviewRoot_UsesPromotedGxTreeSocketForEveryGeneratedMobilityPart()
        {
            var cameraObject = new GameObject("GaragePreviewCameraTest", typeof(Camera));
            var framePrefab = new GameObject("FramePrefab");
            var firepowerPrefab = new GameObject("FirepowerPrefab");

            try
            {
                var catalog = BuildCatalog();

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
        public void HasPreviewAssemblyData_RejectsMobilityVisualBoundsOnlySocket()
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
                    Vector3.zero,
                    hasVisualBounds: true),
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

        private static GaragePanelCatalog.PartAlignment AutoOkAlignment(
            Vector3 socketOffset,
            bool hasFrameTopSocket = false,
            bool hasGxTreeSocket = false,
            Vector3 gxTreeSocketOffset = default,
            string gxTreeSocketName = null,
            bool hasXfiAttachSocket = false,
            Vector3 xfiAttachSocketOffset = default,
            bool hasVisualBounds = false)
        {
            return new GaragePanelCatalog.PartAlignment
            {
                SocketOffset = socketOffset,
                HasVisualBounds = hasVisualBounds,
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

    }
}
