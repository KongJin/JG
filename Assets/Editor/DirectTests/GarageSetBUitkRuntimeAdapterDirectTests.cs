using Features.Garage.Application;
using Features.Garage.Application.Ports;
using Features.Garage.Domain;
using Features.Garage.Presentation;
using Features.Unit.Application;
using Features.Unit.Domain;
using Features.Unit.Infrastructure;
using NUnit.Framework;
using ProjectSD.EditorTools.UnityMcp;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Sound;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tests.Editor
{
    public sealed class GarageSetBUitkRuntimeAdapterDirectTests
    {
        private const string UxmlPath = "Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uxml";
        private const string UssPath = "Assets/UI/UIToolkit/GarageSetB/GarageSetBWorkspace.uss";
        private const string UitkSourceDirectory = "Assets/Scripts/Features/Garage/Presentation/Uitk";

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
                Assert.AreEqual("A-01", Label(root, "SlotName01Label").text);
                Assert.IsTrue(Button(root, "SlotCard01").ClassListContains("slot-card--active"));
                Assert.IsTrue(root.Q<VisualElement>("SlotIcon01Glyph").ClassListContains("uitk-icon--security"));
                Assert.IsTrue(Button(root, "FirepowerTabButton").ClassListContains("focus-tab--active"));
                Assert.AreEqual("무장 선택", Label(root, "PartListTitleLabel").text);
                Assert.AreEqual("부품 2개", Label(root, "PartListCountLabel").text);
                Assert.AreEqual("현재 장착", Label(root, "SelectedPartPreviewKickerLabel").text);
                Assert.AreEqual("레일건", Label(root, "SelectedPartPreviewTitleLabel").text);
                Assert.AreEqual("EN 24", Label(root, "SelectedPartEnergyLabel").text);
                Assert.AreEqual("ATK 840 | RNG 12.5", Label(root, "SelectedPartPreviewMetaLabel").text);
                Assert.AreEqual("ATK", Label(root, "SelectedPartStat01Label").text);
                Assert.AreEqual("840", Label(root, "SelectedPartStat01Value").text);
                Assert.AreEqual("RNG", Label(root, "SelectedPartStat02Label").text);
                Assert.AreEqual("12.5", Label(root, "SelectedPartStat02Value").text);
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
        public void Render_PartListKeepsRowsAccessibleBeyondInitialEight()
        {
            var fixture = CreateRuntimeAdapterFixture();
            try
            {
                fixture.Adapter.Render(
                    CreateSlots(),
                    CreatePartListWithOptionCount(12),
                    CreateEditor(),
                    new GarageResultViewModel(
                        "편성 중",
                        string.Empty,
                        "ATK 840",
                        isReady: false,
                        isDirty: true,
                        canSave: true,
                        primaryActionLabel: "임시 편성"),
                    GarageEditorFocus.Firepower,
                    isSaving: false);

                var root = fixture.Host;
                Assert.NotNull(Button(root, "PartRow12"));
                Assert.AreEqual("부품 12개", Label(root, "PartListCountLabel").text);
                Assert.AreEqual("부품 12", Label(root, "PartRow12NameLabel").text);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void Render_AddsCompactRadarAxisLabels()
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
                        primaryActionLabel: "임시 편성",
                        radar: new GarageStatRadarViewModel(
                            new[] { 0.8f, 0.6f, 0.7f, 0.5f, 0.4f, 0.9f, 0.65f },
                            null,
                            70)),
                    GarageEditorFocus.Firepower,
                    isSaving: false);

                var radar = fixture.Host.Q<VisualElement>("StatRadarGraph");
                Assert.NotNull(radar);
                CollectionAssert.AreEqual(
                    new[] { "ATK", "ASPD", "RNG", "HP", "DEF", "SPD", "MOV" },
                    radar.Query<Label>(className: "stat-radar-label").ToList().Select(label => label.text).ToArray());
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
        public void PartListSurface_HorizontalDragSelectsAdjacentTabInVisualOrder()
        {
            var root = LoadRoot();
            var surface = new GarageSetBPartListSurface(root);
            GarageEditorFocus selectedFocus = GarageEditorFocus.Mobility;
            surface.FocusSelected += focus => selectedFocus = focus;

            surface.Render(CreatePartList(GarageNovaPartPanelSlot.Mobility), GarageEditorFocus.Mobility);

            Assert.IsTrue(surface.TrySelectFocusFromHorizontalDrag(new Vector2(-64f, 3f)));
            Assert.AreEqual(GarageEditorFocus.Frame, selectedFocus);

            surface.Render(CreatePartList(GarageNovaPartPanelSlot.Frame), GarageEditorFocus.Frame);
            Assert.IsTrue(surface.TrySelectFocusFromHorizontalDrag(new Vector2(-64f, 3f)));
            Assert.AreEqual(GarageEditorFocus.Firepower, selectedFocus);

            surface.Render(CreatePartList(GarageNovaPartPanelSlot.Firepower), GarageEditorFocus.Firepower);
            Assert.IsTrue(surface.TrySelectFocusFromHorizontalDrag(new Vector2(64f, 3f)));
            Assert.AreEqual(GarageEditorFocus.Frame, selectedFocus);
        }

        [Test]
        public void SlotSurface_RendersSlotCardsAndClassesFromViewModels()
        {
            var root = LoadRoot();
            var surface = new GarageSetBSlotSurface(root);

            var slots = CreateSavedSlots();
            surface.Render(slots);

            Assert.AreEqual("A-01", Label(root, "SlotCode01Label").text);
            Assert.AreEqual("레일건", Label(root, "SlotName01Label").text);
            Assert.AreEqual("A-02", Label(root, "SlotCode02Label").text);
            Assert.AreEqual("발칸", Label(root, "SlotName02Label").text);
            Assert.IsTrue(Button(root, "SlotCard01").ClassListContains("slot-card--active"));
            Assert.IsFalse(Button(root, "SlotCard04").ClassListContains("slot-card--active"));
            Assert.IsTrue(Button(root, "SlotCard04").ClassListContains("slot-card--empty"));

            surface.Render(CreateSlots());
            Assert.AreEqual("A-01", Label(root, "SlotCode01Label").text);
            Assert.AreEqual("B-02", Label(root, "SlotCode02Label").text);
            Assert.IsTrue(root.Q<VisualElement>("SlotIcon01Glyph").ClassListContains("uitk-icon--security"));
            Assert.IsTrue(root.Q<VisualElement>("SlotIcon02Glyph").ClassListContains("uitk-icon--add"));
        }

        [Test]
        public void SlotSurface_ShowsClearButtonOnlyForSelectedNonEmptySlot()
        {
            var root = LoadRoot();
            var surface = new GarageSetBSlotSurface(root);

            surface.Render(CreateSlots());

            Assert.AreSame(root.Q<VisualElement>("SlotCard01Cell"), Button(root, "SlotClear01Button").parent);
            Assert.AreEqual(DisplayStyle.Flex, Button(root, "SlotClear01Button").style.display.value);
            Assert.IsTrue(Button(root, "SlotClear01Button").enabledSelf);
            Assert.IsTrue(Button(root, "SlotClear01Button").ClassListContains("slot-clear-button--visible"));
            Assert.AreEqual(DisplayStyle.None, Button(root, "SlotClear02Button").style.display.value);
            Assert.IsFalse(Button(root, "SlotClear02Button").enabledSelf);
        }

        [Test]
        public void UxmlAndUss_OwnStableAssemblyPreviewAndPartPickerLayout()
        {
            var root = LoadRoot();
            var slotStrip = root.Q<VisualElement>("SlotStrip");
            var unitPreview = root.Q<VisualElement>("PreviewCard");
            var focusBar = root.Q<VisualElement>("PartFocusBar");
            var pane = root.Q<VisualElement>("PartSelectionPane");
            var listCard = root.Q<VisualElement>("PartListCard");
            var content = slotStrip.parent;

            Assert.Less(IndexOfChild(content, slotStrip), IndexOfChild(content, unitPreview));
            Assert.Less(IndexOfChild(content, unitPreview), IndexOfChild(content, focusBar));
            Assert.Less(IndexOfChild(content, focusBar), IndexOfChild(content, pane));
            Assert.AreSame(pane, listCard.parent);
            Assert.IsNull(root.Q<VisualElement>("PartInspectorColumn"));
            Assert.IsNull(root.Q<VisualElement>("EditorCard"));
            Assert.IsInstanceOf<Button>(root.Q<VisualElement>("PartRow01"));

            var uss = File.ReadAllText(UssPath);
            StringAssert.Contains(".preview-card", uss);
            StringAssert.Contains("height: 156px;", uss);
            StringAssert.Contains(".unit-diagram", uss);
            StringAssert.Contains("width: 108px;", uss);
            StringAssert.Contains(".stat-radar-graph", uss);
            StringAssert.Contains(".stat-radar-label", uss);
            StringAssert.Contains(".slot-card--dragging", uss);
            StringAssert.Contains(".part-list-rows", uss);
            StringAssert.Contains("height: 252px;", uss);
            StringAssert.Contains(".save-dock", uss);
            StringAssert.Contains("position: relative;", uss);
        }

        [Test]
        public void StaticLayout_IsOwnedByUxmlAndUssNotRuntimeStyleRepair()
        {
            Assert.IsFalse(
                File.Exists(Path.Combine(UitkSourceDirectory, "GarageSetBUitkLayoutController.cs")),
                "Garage Set B static layout must not be repaired by a runtime layout controller.");

            var uss = File.ReadAllText(UssPath);
            StringAssert.Contains("Garage Set B authored static layout.", uss);
            StringAssert.DoesNotContain("previously owned by GarageSetBUitkLayoutController", uss);

            var forbiddenLayoutWrite = new Regex(
                @"\.style\.(?:height|width|minHeight|maxHeight|minWidth|maxWidth|left|right|top|bottom|margin[A-Za-z]*|padding[A-Za-z]*|position|flexGrow|flexShrink|flexDirection|alignSelf)\s*=");
            var violations = new List<string>();

            foreach (var path in Directory.GetFiles(UitkSourceDirectory, "*.cs"))
            {
                var fileName = Path.GetFileName(path);
                if (fileName == "GarageStatRadarElement.cs")
                    continue;

                var lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (!forbiddenLayoutWrite.IsMatch(line))
                        continue;

                    if (IsAllowedGarageUitkDataStyleWrite(fileName, line))
                        continue;

                    violations.Add($"{fileName}:{i + 1}: {line}");
                }
            }

            CollectionAssert.IsEmpty(violations);
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
            Assert.AreSame(root.Q<VisualElement>("SlotStrip").parent, saveDock.parent);
            Assert.Less(IndexOfChild(listCard, search), IndexOfChild(listCard, rows));
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
                Assert.AreEqual("출격 편성 저장", Button(root, "SaveButton").text);
                Assert.IsTrue(Button(root, "SaveButton").enabledSelf);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void PageController_CommandButtonsPublishSemanticClickSounds()
        {
            var fixture = CreateRuntimeAdapterFixture();
            try
            {
                var controller = fixture.DocumentObject.AddComponent<GarageSetBUitkPageController>();
                SetObjectReference(controller, "_adapter", fixture.Adapter);

                var eventBus = new EventBus();
                var soundKeys = new List<string>();
                eventBus.Subscribe<SoundRequestEvent>(this, e => soundKeys.Add(e.Request.SoundKey));

                controller.Initialize(
                    new InitializeGarageUseCase(new FakeGaragePersistence(), new FakeGarageNetwork(), cloudPort: null),
                    new ComposeUnitUseCase(new ValidUnitCompositionPort()),
                    new ValidateRosterUseCase(new AlwaysValidRosterValidationProvider()),
                    new SaveRosterUseCase(cloudPort: null, new FakeGaragePersistence(), new FakeGarageNetwork()),
                    eventBus,
                    CreateControllerCatalog());

                ClickButton(Button(fixture.Host, "SettingsButton"));
                CollectionAssert.AreEqual(new[] { "ui_click" }, soundKeys);

                SelectCompleteDraft(controller);
                soundKeys.Clear();
                ClickButton(Button(fixture.Host, "SlotClear01Button"));
                CollectionAssert.AreEqual(new[] { "ui_back" }, soundKeys);

                SelectCompleteDraft(controller);
                soundKeys.Clear();
                Assert.IsTrue(Button(fixture.Host, "SaveButton").enabledSelf);
                ClickButton(Button(fixture.Host, "SaveButton"));
                CollectionAssert.AreEqual(new[] { "garage_save" }, soundKeys);
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
        public void RuntimeAdapter_BindToHost_RebindsAfterDocumentRootBind()
        {
            var fixture = CreateRuntimeAdapterFixture(bindToHost: false);
            try
            {
                var document = fixture.DocumentObject.GetComponent<UIDocument>();
                LoadTreeInto(document.rootVisualElement);

                Assert.IsTrue(fixture.Adapter.Bind());

                var host = new VisualElement { name = "GarageUitkHost" };
                host.Add(new Label("Garage workspace is loading."));

                Assert.IsTrue(fixture.Adapter.BindToHost(host));
                fixture.Adapter.Render(
                    CreateSlots(),
                    CreatePartList(),
                    CreateEditor(),
                    new GarageResultViewModel(
                        "host-bound render",
                        string.Empty,
                        "ATK 840",
                        isReady: false,
                        isDirty: true,
                        canSave: true,
                        primaryActionLabel: "임시 편성"),
                    GarageEditorFocus.Firepower,
                    isSaving: false);

                Assert.NotNull(host.Q<VisualElement>("GarageSetBScreen"));
                Assert.AreEqual("host-bound render", Label(host, "CommandStatusLabel").text);
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
        public void ViewModelFactory_DoesNotMarkDraftSelectionAsEquippedBeforeSave()
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
                new GarageNovaPartsDraftSelection(null, "vulcan", null),
                new GarageNovaPartsEquippedSelection(null, "railgun", null),
                GarageEditorFocus.Firepower,
                string.Empty);

            Assert.AreEqual("vulcan", viewModel.SelectedPartId);
            Assert.IsFalse(viewModel.SelectedPartIsEquipped);
            Assert.IsTrue(viewModel.Options[1].IsSelected);
            Assert.IsFalse(viewModel.Options[1].IsEquipped);
            Assert.IsFalse(viewModel.Options[0].IsSelected);
            Assert.IsTrue(viewModel.Options[0].IsEquipped);
        }

        [Test]
        public void Render_DoesNotShowEquippedBadgeForUnsavedDraftSelection()
        {
            var fixture = CreateRuntimeAdapterFixture();
            try
            {
                fixture.Adapter.Render(
                    CreateSlots(),
                    CreateDraftOnlyPartList(),
                    CreateEditor(),
                    new GarageResultViewModel(
                        "편성 중",
                        string.Empty,
                        "ATK 840",
                        isReady: false,
                        isDirty: true,
                        canSave: true,
                        primaryActionLabel: "임시 편성"),
                    GarageEditorFocus.Firepower,
                    isSaving: false);

                var root = fixture.Host;
                Assert.AreEqual("선택 후보", Label(root, "SelectedPartPreviewKickerLabel").text);
                Assert.IsTrue(root.Q<VisualElement>("PartRow01").ClassListContains("part-row--selected"));
                Assert.AreEqual(DisplayStyle.None, Label(root, "PartRow01BadgeLabel").style.display.value);
                Assert.AreEqual("장착중", Label(root, "PartRow02BadgeLabel").text);
                Assert.AreEqual(DisplayStyle.Flex, Label(root, "PartRow02BadgeLabel").style.display.value);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
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

        private static void LoadTreeInto(VisualElement root)
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            Assert.NotNull(asset, $"UXML not found: {UxmlPath}");
            root.Clear();
            asset.CloneTree(root);
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
                    new GarageSlotDisplayData(
                        "A-01",
                        "A-01 가디언",
                        "레일건 / 중장갑",
                        "임시",
                        hasCommittedLoadout: true,
                        hasDraftChanges: true,
                        isEmpty: false,
                        isSelected: true,
                        showArrow: false,
                        callsign: "A-01",
                        roleLabel: "전선 고정"),
                    GarageSlotPreviewData.Empty),
                new GarageSlotViewModel(
                    new GarageSlotDisplayData(
                        "B-02",
                        "B-02",
                        "빈 슬롯",
                        "비어 있음",
                        hasCommittedLoadout: false,
                        hasDraftChanges: false,
                        isEmpty: true,
                        isSelected: false),
                    GarageSlotPreviewData.Empty),
                new GarageSlotViewModel(
                    new GarageSlotDisplayData(
                        "C-03",
                        "C-03",
                        "빈 슬롯",
                        "비어 있음",
                        hasCommittedLoadout: false,
                        hasDraftChanges: false,
                        isEmpty: true,
                        isSelected: false),
                    GarageSlotPreviewData.Empty),
                new GarageSlotViewModel(
                    new GarageSlotDisplayData(
                        "D-04",
                        "D-04",
                        "빈 슬롯",
                        "비어 있음",
                        hasCommittedLoadout: false,
                        hasDraftChanges: false,
                        isEmpty: true,
                        isSelected: false),
                    GarageSlotPreviewData.Empty),
            };
        }

        private static GarageSlotViewModel[] CreateSavedSlots()
        {
            return new[]
            {
                new GarageSlotViewModel(
                    new GarageSlotDisplayData(
                        "A-01",
                        "A-01 가디언",
                        "레일건 / 중장갑",
                        "현역",
                        hasCommittedLoadout: true,
                        hasDraftChanges: false,
                        isEmpty: false,
                        isSelected: true,
                        showArrow: false,
                        callsign: "A-01",
                        roleLabel: "전선 고정"),
                    new GarageSlotPreviewData(
                        loadoutKey: null,
                        frameId: "frame-a",
                        firepowerId: "fire-a",
                        mobilityId: "mob-a",
                        framePreviewPrefab: null,
                        firepowerPreviewPrefab: null,
                        mobilityPreviewPrefab: null,
                        frameAlignment: null,
                        firepowerAlignment: null,
                        mobilityAlignment: null,
                        mobilityUsesAssemblyPivot: false,
                        frameAssemblyForm: AssemblyForm.Unspecified,
                        firepowerAssemblyForm: AssemblyForm.Unspecified)),
                new GarageSlotViewModel(
                    new GarageSlotDisplayData(
                        "A-02",
                        "A-02 스트라이커",
                        "발칸 / 고기동",
                        "현역",
                        hasCommittedLoadout: true,
                        hasDraftChanges: false,
                        isEmpty: false,
                        isSelected: false,
                        showArrow: false,
                        callsign: "A-02",
                        roleLabel: "강습"),
                    new GarageSlotPreviewData(
                        loadoutKey: null,
                        frameId: "frame-b",
                        firepowerId: "fire-b",
                        mobilityId: "mob-b",
                        framePreviewPrefab: null,
                        firepowerPreviewPrefab: null,
                        mobilityPreviewPrefab: null,
                        frameAlignment: null,
                        firepowerAlignment: null,
                        mobilityAlignment: null,
                        mobilityUsesAssemblyPivot: false,
                        frameAssemblyForm: AssemblyForm.Unspecified,
                        firepowerAssemblyForm: AssemblyForm.Unspecified)),
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
                selectedDetailText: "ATK 840 | RNG 12.5",
                selectedPreviewPrefab: null,
                selectedAlignment: null,
                new[]
                {
                    new GarageNovaPartOptionViewModel(
                        slot,
                        "railgun",
                        "레일건",
                        "ATK 840 | RNG 12.5",
                        "Assets/Parts/Railgun.prefab",
                        isSelected: true,
                        needsNameReview: false,
                        energyText: "EN 24",
                        stats: new[]
                        {
                            new GarageNovaPartStatViewModel("ATK", "840", 100f),
                            new GarageNovaPartStatViewModel("RNG", "12.5", 78f),
                        },
                        isEquipped: true),
                    new GarageNovaPartOptionViewModel(
                        slot,
                        "vulcan",
                        "발칸",
                        "ATK 520 | RNG 8.0",
                        "Assets/Parts/Vulcan.prefab",
                        isSelected: false,
                        needsNameReview: false),
                },
                selectedPartId: "railgun",
                selectedEnergyText: "EN 24",
                selectedMetaText: "ATK 840 | RNG 12.5",
                selectedStats: new[]
                {
                    new GarageNovaPartStatViewModel("ATK", "840", 100f),
                    new GarageNovaPartStatViewModel("RNG", "12.5", 78f),
                },
                selectedPartIsEquipped: true);
        }

        private static GarageNovaPartsPanelViewModel CreateDraftOnlyPartList()
        {
            return new GarageNovaPartsPanelViewModel(
                GarageNovaPartPanelSlot.Firepower,
                searchText: string.Empty,
                countText: "부품 2개",
                selectedNameText: "발칸",
                selectedDetailText: "ATK 520 | RNG 8.0",
                selectedPreviewPrefab: null,
                selectedAlignment: null,
                new[]
                {
                    new GarageNovaPartOptionViewModel(
                        GarageNovaPartPanelSlot.Firepower,
                        "vulcan",
                        "발칸",
                        "ATK 520 | RNG 8.0",
                        "Assets/Parts/Vulcan.prefab",
                        isSelected: true,
                        needsNameReview: false),
                    new GarageNovaPartOptionViewModel(
                        GarageNovaPartPanelSlot.Firepower,
                        "railgun",
                        "레일건",
                        "ATK 840 | RNG 12.5",
                        "Assets/Parts/Railgun.prefab",
                        isSelected: false,
                        needsNameReview: false,
                        isEquipped: true),
                },
                selectedPartId: "vulcan",
                selectedMetaText: "ATK 520 | RNG 8.0",
                selectedPartIsEquipped: false);
        }

        private static GarageNovaPartsPanelViewModel CreatePartListWithOptionCount(int count)
        {
            var options = new GarageNovaPartOptionViewModel[count];
            for (int i = 0; i < count; i++)
            {
                options[i] = new GarageNovaPartOptionViewModel(
                    GarageNovaPartPanelSlot.Firepower,
                    $"part-{i + 1:00}",
                    $"부품 {i + 1:00}",
                    $"ATK {100 + i}",
                    $"Assets/Parts/Part{i + 1:00}.prefab",
                    isSelected: i == 0,
                    needsNameReview: false);
            }

            return new GarageNovaPartsPanelViewModel(
                GarageNovaPartPanelSlot.Firepower,
                searchText: string.Empty,
                countText: $"부품 {count}개",
                selectedNameText: options[0].DisplayName,
                selectedDetailText: options[0].DetailText,
                selectedPreviewPrefab: null,
                selectedAlignment: null,
                options,
                selectedPartId: options[0].Id,
                selectedMetaText: options[0].MetaText);
        }

        private static void SelectCompleteDraft(GarageSetBUitkPageController controller)
        {
            Assert.IsTrue(controller.TrySelectVisiblePart(
                GarageNovaPartPanelSlot.Frame,
                0,
                out _,
                out var hasFrameOptions));
            Assert.IsTrue(hasFrameOptions);

            Assert.IsTrue(controller.TrySelectVisiblePart(
                GarageNovaPartPanelSlot.Firepower,
                0,
                out _,
                out var hasFirepowerOptions));
            Assert.IsTrue(hasFirepowerOptions);

            Assert.IsTrue(controller.TrySelectVisiblePart(
                GarageNovaPartPanelSlot.Mobility,
                0,
                out _,
                out var hasMobilityOptions));
            Assert.IsTrue(hasMobilityOptions);
        }

        private static GaragePanelCatalog CreateControllerCatalog()
        {
            return new GaragePanelCatalog(
                new[]
                {
                    new GaragePanelCatalog.FrameOption { Id = "frame0", DisplayName = "가디언" },
                },
                new[]
                {
                    new GaragePanelCatalog.FirepowerOption { Id = "fire0", DisplayName = "단일탄", Range = 4f },
                },
                new[]
                {
                    new GaragePanelCatalog.MobilityOption { Id = "mob0", DisplayName = "중장갑", MoveRange = 3f },
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

        private static bool IsAllowedGarageUitkDataStyleWrite(string fileName, string line)
        {
            if (fileName == "GarageSetBUitkRenderCoordinator.cs" &&
                line.Contains("PreviewPowerFill.style.width"))
            {
                return true;
            }

            if (fileName == "GarageSetBPartPreviewSurface.cs" &&
                line.Contains("bar.Fill.style.height"))
            {
                return true;
            }

            return false;
        }

        private static void ClickButton(Button button)
        {
            Assert.AreEqual("Button.clicked", UitkHandlers.InvokeElementForTest(button, "click"));
        }

        private sealed class FakeGaragePersistence : IGaragePersistencePort
        {
            public void Save(GarageRoster roster)
            {
            }

            public GarageRoster Load()
            {
                return new GarageRoster();
            }

            public void Delete()
            {
            }
        }

        private sealed class FakeGarageNetwork : IGarageNetworkPort
        {
            public void SyncRoster(GarageRoster roster)
            {
            }

            public void SyncReady(bool isReady)
            {
            }

            public GarageRoster GetPlayerRoster(object playerId)
            {
                return null;
            }

            public bool IsPlayerReady(object playerId)
            {
                return false;
            }

            public Dictionary<object, GarageRoster> GetAllPlayersRosters()
            {
                return new Dictionary<object, GarageRoster>();
            }

            public GarageRoster GetLocalPlayerRoster()
            {
                return null;
            }
        }

        private sealed class AlwaysValidRosterValidationProvider : IRosterValidationProvider
        {
            public bool TryValidateComposition(
                string frameId,
                string firepowerModuleId,
                string mobilityModuleId,
                out string errorMessage)
            {
                errorMessage = null;
                return true;
            }
        }

        private sealed class ValidUnitCompositionPort : IUnitCompositionPort
        {
            public ModuleStats GetFrameBaseStats(string frameId)
            {
                return new ModuleStats(frameBaseHp: 600f, defense: 5f);
            }

            public ModuleStats GetFirepowerStats(string moduleId)
            {
                return new ModuleStats(attackDamage: 30f, attackSpeed: 1f, range: 4f);
            }

            public ModuleStats GetMobilityStats(string moduleId)
            {
                return new ModuleStats(moveSpeed: 3f, moveRange: 3f);
            }

            public CostCalculator.StatCostTuning GetCostTuning()
            {
                return CostCalculator.StatCostTuning.Default;
            }

            public string GetPassiveTraitId(string frameId)
            {
                return string.Empty;
            }

            public int GetPassiveTraitCostBonus(string frameId)
            {
                return 0;
            }
        }
    }
}
