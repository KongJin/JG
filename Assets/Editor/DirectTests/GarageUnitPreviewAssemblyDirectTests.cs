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
            string gxTreeSocketName = null)
        {
            return new GaragePanelCatalog.PartAlignment
            {
                SocketOffset = socketOffset,
                HasGxTreeSocket = hasGxTreeSocket,
                GxTreeSocketOffset = gxTreeSocketOffset,
                GxTreeSocketName = gxTreeSocketName,
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
