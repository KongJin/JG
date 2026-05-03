using Features.Garage.Presentation;
using Features.Unit.Infrastructure;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace Tests.Editor
{
    public sealed class GarageSetBUitkRuntimeAdapterDirectTests
    {
        private const string UxmlPath = "Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uxml";

        [Test]
        public void Render_MapsPresenterViewModelsToNamedElements()
        {
            var fixture = CreateRuntimeAdapterFixture();
            try
            {
                fixture.Adapter.Render(
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

                var root = fixture.Host;
                Assert.AreEqual("기체 편성 갱신 대기", Label(root, "CommandStatusLabel").text);
                Assert.AreEqual("A-01", Label(root, "SlotCode01Label").text);
                Assert.AreEqual("전선 고정", Label(root, "SlotName01Label").text);
                Assert.IsTrue(Button(root, "SlotCard01").ClassListContains("slot-card--active"));
                Assert.IsTrue(root.Q<VisualElement>("SlotIcon01Glyph").ClassListContains("uitk-icon--security"));
                Assert.IsTrue(Button(root, "FirepowerTabButton").ClassListContains("focus-tab--active"));
                Assert.AreEqual("무장 선택", Label(root, "PartListTitleLabel").text);
                Assert.AreEqual("부품 2개", Label(root, "PartListCountLabel").text);
                Assert.AreEqual("현재 장착", Label(root, "SelectedPartPreviewKickerLabel").text);
                Assert.AreEqual("레일건", Label(root, "SelectedPartPreviewTitleLabel").text);
                Assert.AreEqual("EN 24", Label(root, "SelectedPartEnergyLabel").text);
                Assert.AreEqual("EN 24 | ATK 840 | RNG 12.5", Label(root, "SelectedPartPreviewMetaLabel").text);
                Assert.AreEqual("ATK", Label(root, "SelectedPartStat01Label").text);
                Assert.AreEqual("840", Label(root, "SelectedPartStat01Value").text);
                Assert.AreEqual("RNG", Label(root, "SelectedPartStat02Label").text);
                Assert.AreEqual("12.5", Label(root, "SelectedPartStat02Value").text);
                Assert.AreEqual(100f, root.Q<VisualElement>("SelectedPartStat01Fill").style.width.value.value);
                Assert.Greater(root.Q<VisualElement>("SelectedPartStat01Fill").style.height.value.value, 0f);
                Assert.IsTrue(root.Q<VisualElement>("PartRow01").ClassListContains("part-row--selected"));
                Assert.AreEqual(
                    "저장 시 선택 슬롯과 전체 편성이 동시에 갱신됩니다.",
                    Label(root, "SaveValidationLabel").text);
                Assert.AreEqual(DisplayStyle.Flex, Label(root, "SaveValidationLabel").style.display.value);
                Assert.AreEqual("출격 편성 저장", Button(root, "SaveButton").text);
                Assert.IsTrue(Button(root, "SaveButton").enabledSelf);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
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
        public void SlotSurface_RendersSlotCardsAndClassesFromViewModels()
        {
            var root = LoadRoot();
            var surface = new GarageSetBSlotSurface(root);

            var slots = CreateSavedSlots();
            surface.Render(slots);

            Assert.AreEqual("A-01", Label(root, "SlotCode01Label").text);
            Assert.AreEqual("전선 고정", Label(root, "SlotName01Label").text);
            Assert.AreEqual("현역", Label(root, "SlotCode02Label").text);
            Assert.AreEqual("강습", Label(root, "SlotName02Label").text);
            Assert.IsTrue(Button(root, "SlotCard01").ClassListContains("slot-card--active"));
            Assert.IsFalse(Button(root, "SlotCard04").ClassListContains("slot-card--active"));
            Assert.IsTrue(Button(root, "SlotCard04").ClassListContains("slot-card--empty"));

            surface.Render(CreateSlots());
            Assert.AreEqual("A-01", Label(root, "SlotCode01Label").text);
            Assert.AreEqual("A-02", Label(root, "SlotCode02Label").text);
            Assert.IsTrue(root.Q<VisualElement>("SlotIcon01Glyph").ClassListContains("uitk-icon--security"));
            Assert.IsTrue(root.Q<VisualElement>("SlotIcon02Glyph").ClassListContains("uitk-icon--smart-toy"));
        }

        [Test]
        public void RuntimeLayout_PrioritizesAssemblyPreviewBeforePartPicker()
        {
            var fixture = CreateRuntimeAdapterFixture();
            try
            {
                var root = fixture.Host;
                var slotStrip = root.Q<VisualElement>("SlotStrip");
                var unitPreview = root.Q<VisualElement>("PreviewCard");
                var focusBar = root.Q<VisualElement>("PartFocusBar");
                var selectedPreview = root.Q<VisualElement>("SelectedPartPreviewCard");
                var pane = root.Q<VisualElement>("PartSelectionPane");
                var listCard = root.Q<VisualElement>("PartListCard");
                var rows = root.Q<ScrollView>("PartListRows");
                var unitPreviewHost = root.Q<VisualElement>("UnitPreviewHost");
                var workspace = root.Q<VisualElement>("WorkspaceScroll");
                var saveDock = root.Q<VisualElement>("SaveDock");
                var content = slotStrip.parent;

                Assert.Less(IndexOfChild(content, slotStrip), IndexOfChild(content, unitPreview));
                Assert.Less(IndexOfChild(content, unitPreview), IndexOfChild(content, focusBar));
                Assert.Less(IndexOfChild(content, focusBar), IndexOfChild(content, selectedPreview));
                Assert.Less(IndexOfChild(content, selectedPreview), IndexOfChild(content, pane));
                Assert.AreSame(pane, listCard.parent);
                Assert.IsNull(root.Q<VisualElement>("PartInspectorColumn"));
                Assert.IsNull(root.Q<VisualElement>("EditorCard"));
                Assert.AreEqual(156f, unitPreview.style.height.value.value);
                Assert.AreEqual(108f, unitPreviewHost.style.width.value.value);
                Assert.AreEqual(108f, unitPreviewHost.style.height.value.value);
                Assert.AreEqual(24f, unitPreviewHost.style.marginLeft.value.value);
                Assert.AreEqual(22f, unitPreviewHost.style.marginTop.value.value);
                Assert.AreEqual(DisplayStyle.None, root.Q<Label>("PreviewTitleLabel").style.display.value);
                Assert.AreEqual(DisplayStyle.None, root.Q<VisualElement>("PreviewTagRow").style.display.value);
                Assert.AreEqual(108f, root.Q<VisualElement>("StatRadarGraph").style.width.value.value);
                Assert.AreEqual(108f, root.Q<VisualElement>("StatRadarGraph").style.height.value.value);
                Assert.AreEqual(24f, root.Q<VisualElement>("StatRadarGraph").style.top.value.value);
                Assert.AreEqual(86f, Button(root, "SlotCard01").style.height.value.value);
                Assert.AreEqual(86f, Button(root, "SlotCard01").style.minHeight.value.value);
                Assert.AreEqual(86f, Button(root, "SlotCard01").style.maxHeight.value.value);
                Assert.AreEqual(0f, Button(root, "SlotCard01").style.flexShrink.value);
                Assert.AreEqual(StyleKeyword.Auto, rows.style.height.keyword);
                Assert.AreEqual(0f, rows.style.flexGrow.value);
                Assert.AreEqual(0f, rows.mouseWheelScrollSize);
                Assert.AreEqual(ScrollView.TouchScrollBehavior.Clamped, rows.touchScrollBehavior);
                Assert.AreEqual(ScrollView.NestedInteractionKind.StopScrolling, rows.nestedInteractionKind);
                Assert.AreEqual(Vector2.zero, rows.scrollOffset);
                Assert.IsFalse(root.Q<VisualElement>("PartRow01") is Button);
                Assert.AreEqual(50f, root.Q<VisualElement>("PartRow01").style.height.value.value);
                Assert.AreEqual(50f, root.Q<VisualElement>("PartRow01").style.minHeight.value.value);
                Assert.AreEqual(50f, root.Q<VisualElement>("PartRow01").style.maxHeight.value.value);
                Assert.AreEqual(0f, root.Q<VisualElement>("PartRow01").style.flexShrink.value);
                Assert.AreEqual(16f, workspace.style.paddingBottom.value.value);
                Assert.AreEqual(0f, workspace.style.marginBottom.value.value);
                Assert.AreEqual(62f, root.style.marginBottom.value.value);
                Assert.AreSame(listCard, saveDock.parent);
                Assert.AreEqual(Position.Relative, saveDock.style.position.value);
                Assert.AreEqual(StyleKeyword.Auto, saveDock.style.bottom.keyword);
                Assert.AreEqual(6f, saveDock.style.marginTop.value.value);
                Assert.AreEqual(0f, saveDock.style.marginBottom.value.value);
                Assert.AreEqual(0f, saveDock.style.flexShrink.value);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void Uxml_PartListUsesHiddenScrollerChromeAndSearchField()
        {
            var root = LoadRoot();
            var listCard = root.Q<VisualElement>("PartListCard");
            var rows = root.Q<ScrollView>("PartListRows");
            var search = root.Q<TextField>("PartSearchField");
            var saveDock = root.Q<VisualElement>("SaveDock");

            Assert.NotNull(listCard);
            Assert.NotNull(rows);
            Assert.NotNull(search);
            Assert.NotNull(saveDock);
            Assert.AreSame(listCard, saveDock.parent);
            Assert.Less(IndexOfChild(listCard, rows), IndexOfChild(listCard, saveDock));
            Assert.AreEqual(ScrollerVisibility.Hidden, rows.horizontalScrollerVisibility);
            Assert.AreEqual(ScrollerVisibility.Hidden, rows.verticalScrollerVisibility);
            Assert.AreEqual("부품 검색", search.label);
        }

        [Test]
        public void Render_HidesSaveDockWhenCleanReady()
        {
            var fixture = CreateRuntimeAdapterFixture();
            try
            {
                fixture.Adapter.Render(
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

                var root = fixture.Host;
                Assert.AreEqual("현역 편성", Button(root, "SaveButton").text);
                Assert.IsFalse(Button(root, "SaveButton").enabledSelf);
                Assert.AreEqual("저장본이 최신입니다.", Label(root, "SaveValidationLabel").text);
                Assert.AreEqual(DisplayStyle.None, Label(root, "SaveValidationLabel").style.display.value);
                Assert.AreEqual(DisplayStyle.None, root.Q<VisualElement>("SaveDock").style.display.value);
                Assert.AreEqual(16f, root.Q<VisualElement>("WorkspaceScroll").style.paddingBottom.value.value);
                Assert.IsTrue(Button(root, "FrameTabButton").ClassListContains("focus-tab--active"));
                Assert.AreEqual("프레임 선택", Label(root, "PartListTitleLabel").text);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void Render_ShowsSaveDockWhenResultCanSave()
        {
            var fixture = CreateRuntimeAdapterFixture();
            try
            {
                fixture.Adapter.Render(
                    CreateSlots(),
                    CreatePartList(GarageNovaPartPanelSlot.Frame),
                    CreateEditor(),
                    new GarageResultViewModel(
                        "편성 변경 있음",
                        "저장 대기",
                        "최근 작전 기록 없음",
                        isReady: false,
                        isDirty: true,
                        canSave: true,
                        primaryActionLabel: "출격 편성 저장"),
                    GarageEditorFocus.Frame,
                    isSaving: false);

                var root = fixture.Host;
                Assert.AreEqual(DisplayStyle.Flex, root.Q<VisualElement>("SaveDock").style.display.value);
                Assert.AreEqual(16f, root.Q<VisualElement>("WorkspaceScroll").style.paddingBottom.value.value);
                Assert.AreEqual("출격 편성 저장", Button(root, "SaveButton").text);
                Assert.IsTrue(Button(root, "SaveButton").enabledSelf);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void Render_WithoutPreviewRenderer_HidesRuntimePreviewImage()
        {
            var fixture = CreateRuntimeAdapterFixture();
            try
            {
                fixture.Adapter.Render(
                    CreateSlots(),
                    CreatePartList(),
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

                var root = fixture.Host;
                var previewImage = root.Q<Image>("RuntimeUnitPreviewImage");
                Assert.NotNull(previewImage);
                Assert.IsNull(previewImage.image);
                Assert.AreEqual(DisplayStyle.None, previewImage.style.display.value);
                Assert.AreEqual(DisplayStyle.Flex, Label(root, "UnitPreviewLabel").style.display.value);
                Assert.AreEqual("설계도 확인", Label(root, "PreviewTitleLabel").text);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void Render_WithoutPartPreviewRenderer_HidesSelectedPartPreviewImage()
        {
            var fixture = CreateRuntimeAdapterFixture();
            try
            {
                fixture.Adapter.Render(
                    CreateSlots(),
                    CreatePartList(),
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

                var root = fixture.Host;
                var previewImage = root.Q<Image>("SelectedPartPreviewImage");
                Assert.NotNull(previewImage);
                Assert.IsNull(previewImage.image);
                Assert.AreEqual(DisplayStyle.None, previewImage.style.display.value);
                Assert.AreEqual(DisplayStyle.Flex, Label(root, "SelectedPartPreviewLabel").style.display.value);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void RuntimeAdapter_BindToHost_ReplacesLobbyPlaceholderWithGarageScreen()
        {
            var fixture = CreateRuntimeAdapterFixture(bindToHost: false);
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
                Assert.IsTrue(fixture.Adapter.SetDocumentRootVisible(false));

                var screen = fixture.Host.Q<VisualElement>("GarageSetBScreen");
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

            Assert.AreEqual("부품 1/2개", viewModel.CountText);
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

            Assert.AreEqual("부품 12개", viewModel.CountText);
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

            Assert.AreEqual("부품 1개", viewModel.CountText);
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

        private static RuntimeAdapterFixture CreateRuntimeAdapterFixture(bool bindToHost = true)
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            Assert.NotNull(asset, $"UXML not found: {UxmlPath}");

            var documentObject = new GameObject("GarageSetBUitkRuntimeAdapterTest");
            var document = documentObject.AddComponent<UIDocument>();
            document.visualTreeAsset = asset;
            var adapter = documentObject.AddComponent<GarageSetBUitkRuntimeAdapter>();
            SetObjectReference(adapter, "_document", document);

            var host = new VisualElement { name = "GarageUitkHost" };
            if (bindToHost)
                Assert.IsTrue(adapter.BindToHost(host));

            return new RuntimeAdapterFixture(documentObject, adapter, host);
        }

        private readonly struct RuntimeAdapterFixture
        {
            public RuntimeAdapterFixture(
                GameObject documentObject,
                GarageSetBUitkRuntimeAdapter adapter,
                VisualElement host)
            {
                DocumentObject = documentObject;
                Adapter = adapter;
                Host = host;
            }

            public GameObject DocumentObject { get; }
            public GarageSetBUitkRuntimeAdapter Adapter { get; }
            public VisualElement Host { get; }
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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

        private static GarageSlotViewModel[] CreateSavedSlots()
        {
            return new[]
            {
                new GarageSlotViewModel(
                    "A-01",
                    "A-01 가디언",
                    "레일건 / 중장갑",
                    "현역",
                    hasCommittedLoadout: true,
                    hasDraftChanges: false,
                    isEmpty: false,
                    isSelected: true,
                    callsign: "A-01",
                    roleLabel: "전선 고정",
                    frameId: "frame-a",
                    firepowerId: "fire-a",
                    mobilityId: "mob-a"),
                new GarageSlotViewModel(
                    "A-02",
                    "A-02 스트라이커",
                    "발칸 / 고기동",
                    "현역",
                    hasCommittedLoadout: true,
                    hasDraftChanges: false,
                    isEmpty: false,
                    isSelected: false,
                    callsign: "A-02",
                    roleLabel: "강습",
                    frameId: "frame-b",
                    firepowerId: "fire-b",
                    mobilityId: "mob-b"),
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
                countText: "부품 2개",
                selectedNameText: "레일건",
                selectedDetailText: "EN 24 | ATK 840 | RNG 12.5 | T3",
                selectedPreviewPrefab: null,
                selectedAlignment: null,
                new[]
                {
                    new GarageNovaPartOptionViewModel(
                        slot,
                        "railgun",
                        "레일건",
                        "EN 24 | ATK 840 | RNG 12.5 | T3",
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

        private static int IndexOfChild(VisualElement parent, VisualElement child)
        {
            Assert.NotNull(parent);
            Assert.NotNull(child);

            int index = 0;
            foreach (var candidate in parent.Children())
            {
                if (ReferenceEquals(candidate, child))
                    return index;

                index++;
            }

            Assert.Fail($"Child {child.name} was not under {parent.name}.");
            return -1;
        }
    }
}
