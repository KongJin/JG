using Features.Garage.Presentation;
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
        public void Render_UsesCompleteSelectedSlotAssemblyInSidePreviewBeforeSinglePartFallback()
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

                Assert.IsTrue(partPreviewRenderer.HasPreview);
                Assert.NotNull(previewImage);
                Assert.AreSame(partPreviewRenderer.PreviewTexture, previewImage.image);
                Assert.AreEqual(DisplayStyle.Flex, previewImage.style.display.value);
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
        public void Render_FallsBackToSelectedSinglePartWhenSelectedSlotIsIncomplete()
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
                "A-01",
                "A-01 가디언",
                "레일건 / 중장갑",
                "임시",
                hasCommittedLoadout: true,
                hasDraftChanges: true,
                isEmpty: false,
                isSelected: true,
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
                    gxTreeSocketName: "legs"));
        }

        private static GarageSlotViewModel CreateIncompletePreviewSlot()
        {
            return new GarageSlotViewModel(
                "A-01",
                "조립 중",
                "조립 중",
                "임시",
                hasCommittedLoadout: false,
                hasDraftChanges: true,
                isEmpty: false,
                isSelected: true,
                frameId: "frame");
        }

        private static GarageNovaPartsPanelViewModel CreatePartListWithoutSinglePartPreview()
        {
            return new GarageNovaPartsPanelViewModel(
                GarageNovaPartPanelSlot.Firepower,
                searchText: string.Empty,
                countText: "1 PARTS",
                selectedNameText: "레일건",
                selectedDetailText: "ATK 840 | RNG 12.5 | T3",
                canApply: true,
                selectedPreviewPrefab: null,
                selectedAlignment: null,
                System.Array.Empty<GarageNovaPartOptionViewModel>());
        }

        private static GarageNovaPartsPanelViewModel CreatePartListWithSinglePartPreview(GameObject previewPrefab)
        {
            return new GarageNovaPartsPanelViewModel(
                GarageNovaPartPanelSlot.Firepower,
                searchText: string.Empty,
                countText: "1 PARTS",
                selectedNameText: "레일건",
                selectedDetailText: "ATK 840 | RNG 12.5 | T3",
                canApply: true,
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
    }
}
