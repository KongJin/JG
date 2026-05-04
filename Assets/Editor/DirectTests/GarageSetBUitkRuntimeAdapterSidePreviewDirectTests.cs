using Features.Garage.Presentation;
using Features.Unit.Domain;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tests.Editor
{
    public sealed class GarageSetBUitkRuntimeAdapterSidePreviewDirectTests
    {
        private const string UxmlPath = "Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uxml";

        [Test]
        public void Render_DoesNotReuseCompleteSelectedSlotAssemblyForSelectedPartPreview()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            var documentObject = new GameObject("GarageSetBUitkRuntimeAdapterTest");
            var rendererObject = new GameObject("PartPreviewRenderer", typeof(Camera), typeof(GarageSetBUitkPreviewRenderer));
            var framePrefab = new GameObject("FramePrefab");
            var firepowerPrefab = new GameObject("FirepowerPrefab");
            var mobilityPrefab = new GameObject("MobilityPrefab");

            try
            {
                Assert.NotNull(asset, $"UXML not found: {UxmlPath}");

                var document = documentObject.AddComponent<UIDocument>();
                document.visualTreeAsset = asset;
                var adapter = documentObject.AddComponent<GarageSetBUitkRuntimeAdapter>();
                var partPreviewRenderer = rendererObject.GetComponent<GarageSetBUitkPreviewRenderer>();

                SetObjectReference(adapter, "_document", document);
                SetObjectReference(adapter, "_partPreviewRenderer", partPreviewRenderer);

                var host = new VisualElement { name = "GarageUitkHost" };
                Assert.IsTrue(adapter.BindToHost(host));

                adapter.Render(
                    new[] { CreateCompletePreviewSlot(framePrefab, firepowerPrefab, mobilityPrefab) },
                    CreatePartListWithoutSinglePartPreview(),
                    CreateEditor(),
                    new GarageResultViewModel(
                        "편성 중",
                        "저장 대기",
                        "ATK 840",
                        isReady: false,
                        isDirty: true,
                        canSave: true,
                        primaryActionLabel: "임시 편성"),
                    GarageEditorFocus.Firepower,
                    isSaving: false);

                var previewImage = host.Q<Image>("SelectedPartPreviewImage");

                Assert.IsFalse(partPreviewRenderer.HasPreview);
                Assert.NotNull(previewImage);
                Assert.IsNull(previewImage.image);
                Assert.AreEqual(DisplayStyle.None, previewImage.style.display.value);
            }
            finally
            {
                Object.DestroyImmediate(framePrefab);
                Object.DestroyImmediate(firepowerPrefab);
                Object.DestroyImmediate(mobilityPrefab);
                Object.DestroyImmediate(rendererObject);
                Object.DestroyImmediate(documentObject);
            }
        }

        [Test]
        public void Render_ShowsSelectedSinglePartWhenSelectedSlotIsIncomplete()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            var documentObject = new GameObject("GarageSetBUitkRuntimeAdapterTest");
            var rendererObject = new GameObject("PartPreviewRenderer", typeof(Camera), typeof(GarageSetBUitkPreviewRenderer));
            var singlePartPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);

            try
            {
                Assert.NotNull(asset, $"UXML not found: {UxmlPath}");

                var document = documentObject.AddComponent<UIDocument>();
                document.visualTreeAsset = asset;
                var adapter = documentObject.AddComponent<GarageSetBUitkRuntimeAdapter>();
                var partPreviewRenderer = rendererObject.GetComponent<GarageSetBUitkPreviewRenderer>();

                SetObjectReference(adapter, "_document", document);
                SetObjectReference(adapter, "_partPreviewRenderer", partPreviewRenderer);

                var host = new VisualElement { name = "GarageUitkHost" };
                Assert.IsTrue(adapter.BindToHost(host));

                adapter.Render(
                    new[] { CreateIncompletePreviewSlot() },
                    CreatePartListWithSinglePartPreview(singlePartPrefab),
                    CreateEditor(),
                    new GarageResultViewModel(
                        "편성 중",
                        "세 파츠를 모두 선택",
                        "ATK 대기",
                        isReady: false,
                        isDirty: true,
                        canSave: false,
                        primaryActionLabel: "임시 편성"),
                    GarageEditorFocus.Firepower,
                    isSaving: false);

                var previewImage = host.Q<Image>("SelectedPartPreviewImage");

                Assert.IsTrue(partPreviewRenderer.HasPreview);
                Assert.NotNull(previewImage);
                Assert.AreSame(partPreviewRenderer.PreviewTexture, previewImage.image);
                Assert.AreEqual(DisplayStyle.Flex, previewImage.style.display.value);
            }
            finally
            {
                Object.DestroyImmediate(singlePartPrefab);
                Object.DestroyImmediate(rendererObject);
                Object.DestroyImmediate(documentObject);
            }
        }

        [Test]
        public void Renderers_UseSeparatePreviewLayersForUnitAndSelectedPart()
        {
            var unitRendererObject = new GameObject(
                "GarageSetBPreviewCamera",
                typeof(Camera),
                typeof(GarageSetBUitkPreviewRenderer));
            var partRendererObject = new GameObject(
                "GarageSetBPartPreviewCamera",
                typeof(Camera),
                typeof(GarageSetBUitkPreviewRenderer));
            var framePrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var firepowerPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var mobilityPrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var selectedPartPrefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            try
            {
                var unitRenderer = unitRendererObject.GetComponent<GarageSetBUitkPreviewRenderer>();
                var partRenderer = partRendererObject.GetComponent<GarageSetBUitkPreviewRenderer>();
                var unitCamera = unitRendererObject.GetComponent<Camera>();
                var partCamera = partRendererObject.GetComponent<Camera>();

                Assert.IsTrue(unitRenderer.Render(CreateCompletePreviewSlot(framePrefab, firepowerPrefab, mobilityPrefab)));
                Assert.IsTrue(partRenderer.RenderPart(CreatePartListWithSinglePartPreview(selectedPartPrefab)));

                var unitRoot = unitRenderer.CurrentPreviewRoot;
                var partRoot = partRenderer.CurrentPreviewRoot;

                Assert.NotNull(unitRoot);
                Assert.NotNull(partRoot);
                Assert.AreNotEqual(unitCamera.cullingMask, partCamera.cullingMask);
                Assert.AreEqual(1 << unitRoot.layer, unitCamera.cullingMask);
                Assert.AreEqual(1 << partRoot.layer, partCamera.cullingMask);
                Assert.AreEqual(0, unitCamera.cullingMask & (1 << partRoot.layer));
                Assert.AreEqual(0, partCamera.cullingMask & (1 << unitRoot.layer));
                AssertAllChildrenUseLayer(unitRoot.transform, unitRoot.layer);
                AssertAllChildrenUseLayer(partRoot.transform, partRoot.layer);
            }
            finally
            {
                Object.DestroyImmediate(selectedPartPrefab);
                Object.DestroyImmediate(mobilityPrefab);
                Object.DestroyImmediate(firepowerPrefab);
                Object.DestroyImmediate(framePrefab);
                Object.DestroyImmediate(partRendererObject);
                Object.DestroyImmediate(unitRendererObject);
            }
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GarageSlotViewModel CreateCompletePreviewSlot(
            GameObject framePrefab,
            GameObject firepowerPrefab,
            GameObject mobilityPrefab)
        {
            return new GarageSlotViewModel(
                new GarageSlotDisplayData(
                    "A-01",
                    "A-01 가디언",
                    "레일건 / 중장갑",
                    "임시",
                    hasCommittedLoadout: true,
                    hasDraftChanges: true,
                    isEmpty: false,
                    isSelected: true),
                new GarageSlotPreviewData(
                    loadoutKey: null,
                    frameId: "frame",
                    firepowerId: "fire",
                    mobilityId: "mobility",
                    framePreviewPrefab: framePrefab,
                    firepowerPreviewPrefab: firepowerPrefab,
                    mobilityPreviewPrefab: mobilityPrefab,
                    frameAlignment: AutoOkAlignment(Vector3.zero, hasFrameTopSocket: true),
                    firepowerAlignment: AutoOkAlignment(Vector3.zero),
                    mobilityAlignment: AutoOkAlignment(
                        Vector3.zero,
                        hasGxTreeSocket: true,
                        gxTreeSocketOffset: new Vector3(0f, 0.3f, 0f),
                        gxTreeSocketName: "legs"),
                    mobilityUsesAssemblyPivot: false,
                    frameAssemblyForm: AssemblyForm.Unspecified,
                    firepowerAssemblyForm: AssemblyForm.Unspecified));
        }

        private static GarageSlotViewModel CreateIncompletePreviewSlot()
        {
            return new GarageSlotViewModel(
                new GarageSlotDisplayData(
                    "A-01",
                    "조립 중",
                    "조립 중",
                    "임시",
                    hasCommittedLoadout: false,
                    hasDraftChanges: true,
                    isEmpty: false,
                    isSelected: true),
                new GarageSlotPreviewData(
                    loadoutKey: null,
                    frameId: "frame",
                    firepowerId: null,
                    mobilityId: null,
                    framePreviewPrefab: null,
                    firepowerPreviewPrefab: null,
                    mobilityPreviewPrefab: null,
                    frameAlignment: null,
                    firepowerAlignment: null,
                    mobilityAlignment: null,
                    mobilityUsesAssemblyPivot: false,
                    frameAssemblyForm: AssemblyForm.Unspecified,
                    firepowerAssemblyForm: AssemblyForm.Unspecified));
        }

        private static GarageNovaPartsPanelViewModel CreatePartListWithoutSinglePartPreview()
        {
            return new GarageNovaPartsPanelViewModel(
                GarageNovaPartPanelSlot.Firepower,
                searchText: string.Empty,
                countText: "부품 1개",
                selectedNameText: "레일건",
                selectedDetailText: "ATK 840 | RNG 12.5 | T3",
                selectedPreviewPrefab: null,
                selectedAlignment: null,
                System.Array.Empty<GarageNovaPartOptionViewModel>());
        }

        private static GarageNovaPartsPanelViewModel CreatePartListWithSinglePartPreview(GameObject previewPrefab)
        {
            return new GarageNovaPartsPanelViewModel(
                GarageNovaPartPanelSlot.Firepower,
                searchText: string.Empty,
                countText: "부품 1개",
                selectedNameText: "레일건",
                selectedDetailText: "ATK 840 | RNG 12.5 | T3",
                selectedPreviewPrefab: previewPrefab,
                selectedAlignment: AutoOkAlignment(Vector3.zero),
                System.Array.Empty<GarageNovaPartOptionViewModel>());
        }

        private static GarageEditorViewModel CreateEditor()
        {
            return new GarageEditorViewModel(
                "A-01 가디언",
                "저장 가능",
                "가디언",
                "HP 900 | ASPD 1.20",
                "레일건",
                "ATK 840 | RNG 12.5",
                "중장갑",
                "MOV 3.5 | ANC 4.0",
                isClearInteractable: true);
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
                HasFrameTopSocket = hasFrameTopSocket,
                HasGxTreeSocket = hasGxTreeSocket,
                GxTreeSocketOffset = gxTreeSocketOffset,
                GxTreeSocketName = gxTreeSocketName,
                QualityFlag = "auto_ok"
            };
        }

        private static void AssertAllChildrenUseLayer(Transform root, int layer)
        {
            Assert.NotNull(root);
            Assert.AreEqual(layer, root.gameObject.layer);
            for (var i = 0; i < root.childCount; i++)
            {
                AssertAllChildrenUseLayer(root.GetChild(i), layer);
            }
        }
    }
}
