using Features.Garage.Presentation;
using Features.Unit.Infrastructure;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

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
                CreatePartList(),
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
            Assert.IsTrue(root.Q<VisualElement>("SlotIcon01Glyph").ClassListContains("uitk-icon--security"));
            Assert.AreEqual("주무장", Label(root, "FocusedPartBadgeLabel").text);
            Assert.AreEqual("레일건", Label(root, "FocusedPartTitleLabel").text);
            Assert.IsTrue(root.Q<VisualElement>("FocusedPartIconGlyph").ClassListContains("uitk-icon--swords"));
            Assert.IsTrue(Button(root, "FirepowerTabButton").ClassListContains("focus-tab--active"));
            Assert.AreEqual("무장 선택", Label(root, "PartListTitleLabel").text);
            Assert.AreEqual("2 PARTS", Label(root, "PartListCountLabel").text);
            Assert.IsTrue(Button(root, "PartRow01").ClassListContains("part-row--selected"));
            Assert.AreEqual("출격 편성 저장", Button(root, "SaveButton").text);
            Assert.IsTrue(Button(root, "SaveButton").enabledSelf);
        }

        [Test]
        public void Uxml_OrdersPartTabsAsMobilityFrameFirepower()
        {
            var root = LoadRoot();
            var focusBar = root.Q<VisualElement>("PartFocusBar");

            var names = focusBar.Children().OfType<Button>().Select(button => button.name).ToArray();

            CollectionAssert.AreEqual(
                new[] { "MobilityTabButton", "FrameTabButton", "FirepowerTabButton" },
                names);
        }

        [Test]
        public void Uxml_PlacesSelectedPartPreviewBesidePartList()
        {
            var root = LoadRoot();
            var pane = root.Q<VisualElement>("PartSelectionPane");
            var inspector = root.Q<VisualElement>("PartInspectorColumn");
            var listCard = root.Q<VisualElement>("PartListCard");
            var statsPanel = root.Q<VisualElement>("SelectedPartStatsPanel");
            var previewHost = root.Q<VisualElement>("SelectedPartPreviewHost");

            Assert.NotNull(pane);
            Assert.AreSame(pane, inspector.parent);
            Assert.AreSame(pane, listCard.parent);
            Assert.AreNotSame(listCard, inspector.parent);
            Assert.AreSame(inspector, statsPanel.parent);
            Assert.AreSame(inspector, previewHost.parent);
            Assert.AreNotSame(listCard, previewHost.parent);
        }

        [Test]
        public void Uxml_PartListUsesHiddenScrollerChromeAndSearchField()
        {
            var root = LoadRoot();
            var rows = root.Q<ScrollView>("PartListRows");
            var search = root.Q<TextField>("PartSearchField");

            Assert.NotNull(rows);
            Assert.NotNull(search);
            Assert.AreEqual(ScrollerVisibility.Hidden, rows.horizontalScrollerVisibility);
            Assert.AreEqual(ScrollerVisibility.Hidden, rows.verticalScrollerVisibility);
            Assert.AreEqual("부품 검색", search.label);
        }

        [Test]
        public void Render_DisablesSaveWhenResultCannotSave()
        {
            var root = LoadRoot();
            var surface = new GarageSetBUitkSurface(root);

            surface.Render(
                CreateSlots(),
                CreatePartList(GarageNovaPartPanelSlot.Frame),
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
            Assert.AreEqual("프레임 선택", Label(root, "PartListTitleLabel").text);
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

        [Test]
        public void SetPartPreviewTexture_TogglesSelectedPartPreviewImage()
        {
            var root = LoadRoot();
            var surface = new GarageSetBUitkSurface(root);
            var texture = new Texture2D(8, 8);

            surface.SetPartPreviewTexture(texture, true);

            var previewImage = root.Q<Image>("SelectedPartPreviewImage");
            Assert.NotNull(previewImage);
            Assert.AreSame(texture, previewImage.image);
            Assert.AreEqual(DisplayStyle.Flex, previewImage.style.display.value);
            Assert.AreEqual(DisplayStyle.None, Label(root, "SelectedPartPreviewLabel").style.display.value);

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void RuntimeAdapter_BindToHost_ReplacesLobbyPlaceholderWithGarageScreen()
        {
            var fixture = CreateRuntimeAdapterFixture();
            try
            {
                var host = new VisualElement { name = "GarageUitkHost" };
                host.Add(new Label("Garage workspace is loading."));

                Assert.IsTrue(fixture.Adapter.BindToHost(host));

                Assert.NotNull(host.Q<VisualElement>("GarageSetBScreen"));
                Assert.IsFalse(host.Query<Label>().ToList().Any(label => label.text == "Garage workspace is loading."));
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void RuntimeAdapter_SetDocumentRootVisible_DoesNotHideHostBoundGarageScreen()
        {
            var fixture = CreateRuntimeAdapterFixture();
            try
            {
                var host = new VisualElement { name = "GarageUitkHost" };

                Assert.IsTrue(fixture.Adapter.BindToHost(host));
                Assert.IsTrue(fixture.Adapter.SetDocumentRootVisible(false));

                var screen = host.Q<VisualElement>("GarageSetBScreen");
                Assert.NotNull(screen);
                Assert.AreEqual(DisplayStyle.Flex, screen.style.display.value);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void ViewModelFactory_FiltersOptionsBySearchText()
        {
            var catalog = new GaragePanelCatalog(
                frames: System.Array.Empty<GaragePanelCatalog.FrameOption>(),
                firepower: new[]
                {
                    new GaragePanelCatalog.FirepowerOption
                    {
                        Id = "railgun",
                        DisplayName = "Rail Slug",
                        AttackDamage = 920,
                        Range = 15,
                        Tier = 3,
                    },
                    new GaragePanelCatalog.FirepowerOption
                    {
                        Id = "vulcan",
                        DisplayName = "Vulcan Cannon",
                        AttackDamage = 840,
                        Range = 12.5f,
                        Tier = 3,
                    },
                },
                mobility: System.Array.Empty<GaragePanelCatalog.MobilityOption>());

            var viewModel = GarageNovaPartsPanelViewModelFactory.Build(
                catalog,
                new GarageNovaPartsDraftSelection(null, "railgun", null),
                GarageEditorFocus.Firepower,
                "rail");

            Assert.AreEqual("1/2 PARTS", viewModel.CountText);
            Assert.AreEqual(1, viewModel.Options.Count);
            Assert.AreEqual("railgun", viewModel.Options[0].Id);
            Assert.IsTrue(viewModel.Options[0].IsSelected);
        }

        [Test]
        public void ViewModelFactory_ReturnsAllFilteredOptionsForScrollableList()
        {
            var firepower = new System.Collections.Generic.List<GaragePanelCatalog.FirepowerOption>();
            for (int i = 0; i < 12; i++)
            {
                firepower.Add(new GaragePanelCatalog.FirepowerOption
                {
                    Id = $"arm{i:00}",
                    DisplayName = $"Arm {i:00}",
                    AttackDamage = 10 + i,
                    Range = 4,
                    Tier = 1,
                });
            }

            var catalog = new GaragePanelCatalog(
                frames: System.Array.Empty<GaragePanelCatalog.FrameOption>(),
                firepower: firepower,
                mobility: System.Array.Empty<GaragePanelCatalog.MobilityOption>());

            var viewModel = GarageNovaPartsPanelViewModelFactory.Build(
                catalog,
                new GarageNovaPartsDraftSelection(null, "arm00", null),
                GarageEditorFocus.Firepower,
                string.Empty);

            Assert.AreEqual("12 PARTS", viewModel.CountText);
            Assert.AreEqual(12, viewModel.Options.Count);
            Assert.AreEqual("arm11", viewModel.Options[11].Id);
        }

        [Test]
        public void ViewModelFactory_FiltersFirepowerBySelectedFrameAssemblyForm()
        {
            var catalog = new GaragePanelCatalog(
                frames: new[]
                {
                    new GaragePanelCatalog.FrameOption
                    {
                        Id = "tower-frame",
                        DisplayName = "타워 프레임",
                        AssemblyForm = AssemblyForm.Tower,
                    },
                },
                firepower: new[]
                {
                    new GaragePanelCatalog.FirepowerOption
                    {
                        Id = "tower-arm",
                        DisplayName = "타워 무장",
                        AssemblyForm = AssemblyForm.Tower,
                    },
                    new GaragePanelCatalog.FirepowerOption
                    {
                        Id = "shoulder-arm",
                        DisplayName = "어깨 무장",
                        AssemblyForm = AssemblyForm.Shoulder,
                    },
                },
                mobility: System.Array.Empty<GaragePanelCatalog.MobilityOption>());

            var viewModel = GarageNovaPartsPanelViewModelFactory.Build(
                catalog,
                new GarageNovaPartsDraftSelection("tower-frame", "shoulder-arm", null),
                GarageEditorFocus.Firepower,
                string.Empty);

            Assert.AreEqual("1 PARTS", viewModel.CountText);
            Assert.AreEqual(1, viewModel.Options.Count);
            Assert.AreEqual("tower-arm", viewModel.Options[0].Id);
            Assert.AreEqual("선택 대기", viewModel.SelectedNameText);
        }

        private static VisualElement LoadRoot()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            Assert.NotNull(asset, $"UXML not found: {UxmlPath}");
            return asset.CloneTree();
        }

        private static RuntimeAdapterFixture CreateRuntimeAdapterFixture()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            Assert.NotNull(asset, $"UXML not found: {UxmlPath}");

            var documentObject = new GameObject("GarageSetBUitkRuntimeAdapterTest");
            var document = documentObject.AddComponent<UIDocument>();
            document.visualTreeAsset = asset;
            var adapter = documentObject.AddComponent<GarageSetBUitkRuntimeAdapter>();
            var serializedAdapter = new SerializedObject(adapter);
            serializedAdapter.FindProperty("_document").objectReferenceValue = document;
            serializedAdapter.ApplyModifiedPropertiesWithoutUndo();

            return new RuntimeAdapterFixture(documentObject, adapter);
        }

        private readonly struct RuntimeAdapterFixture
        {
            public RuntimeAdapterFixture(GameObject documentObject, GarageSetBUitkRuntimeAdapter adapter)
            {
                DocumentObject = documentObject;
                Adapter = adapter;
            }

            public GameObject DocumentObject { get; }
            public GarageSetBUitkRuntimeAdapter Adapter { get; }
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

        private static GarageNovaPartsPanelViewModel CreatePartList(
            GarageNovaPartPanelSlot slot = GarageNovaPartPanelSlot.Firepower)
        {
            return new GarageNovaPartsPanelViewModel(
                slot,
                searchText: string.Empty,
                countText: "2 PARTS",
                selectedNameText: "레일건",
                selectedDetailText: "ATK 840 | RNG 12.5 | T3",
                canApply: true,
                selectedPreviewPrefab: null,
                selectedAlignment: null,
                new[]
                {
                    new GarageNovaPartOptionViewModel(
                        slot,
                        "railgun",
                        "레일건",
                        "ATK 840 | RNG 12.5 | T3",
                        "Assets/Parts/Railgun.prefab",
                        isSelected: true,
                        needsNameReview: false),
                    new GarageNovaPartOptionViewModel(
                        slot,
                        "vulcan",
                        "발칸",
                        "ATK 520 | RNG 8.0 | T1",
                        "Assets/Parts/Vulcan.prefab",
                        isSelected: false,
                        needsNameReview: false),
                });
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
