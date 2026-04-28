using Features.Garage.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tests.Editor
{
    public sealed class GarageSetBUitkSurfaceDirectTests
    {
        private const string UxmlPath = "Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uxml";

        [Test]
        public void Render_MapsPresenterViewModelsToNamedElements()
        {
            var root = LoadRoot();
            var surface = new GarageSetBUitkSurface(root);

            surface.Render(
                CreateSlots(),
                CreateEditor(),
                new GarageResultViewModel(
                    "기체 편성 갱신 대기",
                    "저장 시 선택 슬롯과 전체 편성이 동시에 갱신됩니다.",
                    "ATK 840  |  RNG 12.5m",
                    isReady: false,
                    isDirty: true,
                    canSave: true,
                    primaryActionLabel: "출격 편성 저장"),
                GarageEditorFocus.Firepower,
                isSaving: false);

            Assert.AreEqual("기체 편성 갱신 대기", Label(root, "CommandStatusLabel").text);
            Assert.AreEqual("A-01", Label(root, "SlotCode01Label").text);
            Assert.AreEqual("전선 고정", Label(root, "SlotName01Label").text);
            Assert.IsTrue(Button(root, "SlotCard01").ClassListContains("slot-card--active"));
            Assert.AreEqual("주무장", Label(root, "FocusedPartBadgeLabel").text);
            Assert.AreEqual("레일건", Label(root, "FocusedPartTitleLabel").text);
            Assert.IsTrue(Button(root, "FirepowerTabButton").ClassListContains("focus-tab--active"));
            Assert.AreEqual("출격 편성 저장", Button(root, "SaveButton").text);
            Assert.IsTrue(Button(root, "SaveButton").enabledSelf);
        }

        [Test]
        public void Render_DisablesSaveWhenResultCannotSave()
        {
            var root = LoadRoot();
            var surface = new GarageSetBUitkSurface(root);

            surface.Render(
                CreateSlots(),
                CreateEditor(),
                new GarageResultViewModel(
                    "현역 편성",
                    "저장본이 최신입니다.",
                    "최근 작전 기록 없음",
                    isReady: true,
                    isDirty: false,
                    canSave: false,
                    primaryActionLabel: "현역 편성"),
                GarageEditorFocus.Frame,
                isSaving: false);

            Assert.AreEqual("현역 편성", Button(root, "SaveButton").text);
            Assert.IsFalse(Button(root, "SaveButton").enabledSelf);
            Assert.IsTrue(Button(root, "FrameTabButton").ClassListContains("focus-tab--active"));
        }

        [Test]
        public void SetPreviewTexture_TogglesRuntimePreviewImage()
        {
            var root = LoadRoot();
            var surface = new GarageSetBUitkSurface(root);
            var texture = new Texture2D(8, 8);

            surface.SetPreviewTexture(texture, true);

            var previewImage = root.Q<Image>("RuntimeUnitPreviewImage");
            Assert.NotNull(previewImage);
            Assert.AreSame(texture, previewImage.image);
            Assert.AreEqual(DisplayStyle.Flex, previewImage.style.display.value);
            Assert.AreEqual(DisplayStyle.None, Label(root, "UnitPreviewLabel").style.display.value);
            Assert.AreEqual("UNIT PREVIEW", Label(root, "PreviewTitleLabel").text);

            Object.DestroyImmediate(texture);
        }

        private static VisualElement LoadRoot()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            Assert.NotNull(asset, $"UXML not found: {UxmlPath}");
            return asset.CloneTree();
        }

        private static GarageSlotViewModel[] CreateSlots()
        {
            return new[]
            {
                new GarageSlotViewModel(
                    "A-01",
                    "A-01 가디언",
                    "레일건 / 중장갑",
                    "임시",
                    hasCommittedLoadout: true,
                    hasDraftChanges: true,
                    isEmpty: false,
                    isSelected: true,
                    callsign: "A-01",
                    roleLabel: "전선 고정"),
                new GarageSlotViewModel(
                    "B-02",
                    "B-02",
                    "빈 슬롯",
                    "비어 있음",
                    hasCommittedLoadout: false,
                    hasDraftChanges: false,
                    isEmpty: true,
                    isSelected: false),
                new GarageSlotViewModel(
                    "C-03",
                    "C-03",
                    "빈 슬롯",
                    "비어 있음",
                    hasCommittedLoadout: false,
                    hasDraftChanges: false,
                    isEmpty: true,
                    isSelected: false),
                new GarageSlotViewModel(
                    "D-04",
                    "D-04",
                    "빈 슬롯",
                    "비어 있음",
                    hasCommittedLoadout: false,
                    hasDraftChanges: false,
                    isEmpty: true,
                    isSelected: false),
            };
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

        private static Label Label(VisualElement root, string name)
        {
            var element = root.Q<Label>(name);
            Assert.NotNull(element, name);
            return element;
        }

        private static Button Button(VisualElement root, string name)
        {
            var element = root.Q<Button>(name);
            Assert.NotNull(element, name);
            return element;
        }
    }
}
