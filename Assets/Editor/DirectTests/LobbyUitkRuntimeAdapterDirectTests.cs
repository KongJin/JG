using Features.Lobby.Presentation;
using NUnit.Framework;
using ProjectSD.EditorTools.UnityMcp;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Sound;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public sealed class LobbyUitkRuntimeAdapterDirectTests
    {
        private const string LobbyShellPath = "Assets/UI/UIToolkit/Lobby/LobbyShell.uxml";
        private const string LobbyShellUssPath = "Assets/UI/UIToolkit/Lobby/LobbyShell.uss";
        private const string OperationMemoryPath = "Assets/UI/UIToolkit/OperationMemory/OperationMemoryWorkspace.uxml";
        private const string AccountSyncPath = "Assets/UI/UIToolkit/AccountSync/AccountSyncConsole.uxml";
        private const string ConnectionPath = "Assets/UI/UIToolkit/ConnectionReconnect/ConnectionReconnectControl.uxml";

        [Test]
        public void Bind_ClonesShellAndAuxiliarySurfaces()
        {
            var fixture = CreateFixture();
            try
            {
                Assert.IsTrue(fixture.Adapter.Bind());

                fixture.Adapter.ShowRecordsPage();
                fixture.Adapter.ShowAccountPage();
                fixture.Adapter.ShowConnectionPage();

                var root = fixture.Document.rootVisualElement;
                Assert.NotNull(root.Q<VisualElement>("LobbyShellScreen"));
                Assert.NotNull(root.Q<VisualElement>("OperationMemoryScreen"));
                Assert.NotNull(root.Q<Label>("PilotIdLabel"));
                Assert.NotNull(root.Q<VisualElement>("ConnectionReconnectScreen"));
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void ShowPages_UpdatesVisibleHostNavShellAndClonesRouteSurfaceOnce()
        {
            var fixture = CreateFixture();
            try
            {
                fixture.Adapter.ShowLobbyPage();

                var root = fixture.Document.rootVisualElement;
                AssertPageVisible(root, "LobbyUitkPage");
                AssertPageHidden(root, "GarageUitkHost");
                AssertPageHidden(root, "RecordsUitkHost");
                Assert.AreEqual("로비", root.Q<Label>("ShellTitleLabel").text);
                Assert.AreEqual("동기화 대기", root.Q<Label>("ShellStateLabel").text);
                Assert.IsTrue(root.Q<Button>("LobbyNavButton").ClassListContains("shared-nav-item--selected"));

                fixture.Adapter.ShowRecordsPage();
                var recordsHost = root.Q<VisualElement>("RecordsUitkHost");
                var childCountAfterFirstShow = recordsHost.childCount;

                AssertPageHidden(root, "LobbyUitkPage");
                AssertPageVisible(root, "RecordsUitkHost");
                Assert.AreEqual("기록", root.Q<Label>("ShellTitleLabel").text);
                Assert.AreEqual("LOCAL LOG / SYNC PENDING", root.Q<Label>("ShellStateLabel").text);
                Assert.IsFalse(root.Q<Button>("LobbyNavButton").ClassListContains("shared-nav-item--selected"));
                Assert.IsTrue(root.Q<Button>("RecordsNavButton").ClassListContains("shared-nav-item--selected"));

                fixture.Adapter.ShowRecordsPage();
                Assert.AreEqual(childCountAfterFirstShow, recordsHost.childCount);

                fixture.Adapter.ShowAccountPage();
                AssertPageHidden(root, "RecordsUitkHost");
                AssertPageVisible(root, "AccountUitkHost");
                Assert.AreEqual("계정", root.Q<Label>("ShellTitleLabel").text);
                Assert.IsFalse(root.Q<Button>("RecordsNavButton").ClassListContains("shared-nav-item--selected"));
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void ShellTopButtons_RequestLobbyWhenCurrentAuxiliaryPage()
        {
            var fixture = CreateFixture();
            try
            {
                var lobbyRequests = 0;
                var accountRequests = 0;
                var connectionRequests = 0;
                fixture.Adapter.LobbyPageRequested += () => lobbyRequests++;
                fixture.Adapter.AccountPageRequested += () => accountRequests++;
                fixture.Adapter.ConnectionPageRequested += () => connectionRequests++;

                fixture.Adapter.ShowConnectionPage();
                var root = fixture.Document.rootVisualElement;
                ClickButton(Button(root, "ShellMenuButton"));

                Assert.AreEqual(1, lobbyRequests);
                Assert.AreEqual(0, connectionRequests);

                fixture.Adapter.ShowAccountPage();
                ClickButton(Button(root, "ShellSettingsButton"));

                Assert.AreEqual(2, lobbyRequests);
                Assert.AreEqual(0, accountRequests);

                fixture.Adapter.ShowLobbyPage();
                ClickButton(Button(root, "ShellSettingsButton"));
                ClickButton(Button(root, "ShellMenuButton"));

                Assert.AreEqual(1, accountRequests);
                Assert.AreEqual(1, connectionRequests);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void Uss_NavigationBarHasNoBlackPaddingBand()
        {
            var uss = File.ReadAllText(LobbyShellUssPath);

            StringAssert.Contains(".lobby-shell-screen", uss);
            StringAssert.Contains("padding-bottom: 0;", uss);
            StringAssert.Contains(".shared-workspace", uss);
            StringAssert.Contains("margin-bottom: 0;", uss);
            StringAssert.Contains(".shared-navigation-bar", uss);
            StringAssert.Contains("padding-left: 0;", uss);
            StringAssert.Contains("padding-right: 0;", uss);
            StringAssert.Contains("padding-top: 0;", uss);
            StringAssert.Contains("#RoomListScroll", uss);
            StringAssert.Contains("max-height: 600px;", uss);
        }

        [Test]
        public void Bind_HidesCreateRoomCardBeforeRoomStateArrives()
        {
            var fixture = CreateFixture();
            try
            {
                Assert.IsTrue(fixture.Adapter.Bind());

                var root = fixture.Document.rootVisualElement;
                Assert.AreEqual(DisplayStyle.None, root.Q<ScrollView>("RoomListScroll").style.display.value);
                Assert.AreEqual(DisplayStyle.None, root.Q<VisualElement>("RoomListEmptyStateCard").style.display.value);
                Assert.AreEqual(DisplayStyle.None, root.Q<VisualElement>("CreateRoomCard").style.display.value);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void LobbyPageController_OpenLobbyPageRendersEditorPreviewRoom()
        {
            var documentObject = new GameObject("LobbyPageControllerPreviewRoomTest");
            try
            {
                var document = documentObject.AddComponent<UIDocument>();
                var controller = documentObject.AddComponent<LobbyPageController>();
                var serialized = new SerializedObject(controller);
                serialized.FindProperty("_document").objectReferenceValue = document;
                serialized.FindProperty("_lobbyShellTree").objectReferenceValue = LoadTree(LobbyShellPath);
                serialized.FindProperty("_showEditorPreviewRoom").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                controller.OpenLobbyPage();

                var root = document.rootVisualElement;
                Assert.AreEqual("열린 방 20개", root.Q<Label>("RoomCountLabel").text);
                Assert.AreEqual(DisplayStyle.Flex, root.Q<ScrollView>("RoomListScroll").style.display.value);
                Assert.AreEqual(DisplayStyle.None, root.Q<VisualElement>("RoomListEmptyStateCard").style.display.value);
                Assert.AreEqual(DisplayStyle.Flex, root.Q<VisualElement>("CreateRoomCard").style.display.value);
                Assert.AreEqual(20, root.Q<VisualElement>("RoomList").childCount);
                Assert.AreEqual(
                    "UI 점검용 샘플 방 01",
                    root.Q<VisualElement>("RoomList")[0].Q<Label>(className: "lobby-room-row__title").text);
                Assert.AreEqual(
                    "UI 점검용 샘플 방 20",
                    root.Q<VisualElement>("RoomList")[19].Q<Label>(className: "lobby-room-row__title").text);
            }
            finally
            {
                Object.DestroyImmediate(documentObject);
            }
        }

        [Test]
        public void RenderRooms_MapsViewModelToRows()
        {
            var fixture = CreateFixture();
            try
            {
                fixture.Adapter.RenderRooms(new LobbyRoomListViewModel(
                    "열린 방 1개",
                    new[]
                    {
                        new LobbyRoomRowViewModel(
                            new DomainEntityId("room-1"),
                            "Alpha",
                            "1/4 | Normal",
                            "참가 가능",
                            canJoin: true,
                            isSelected: true,
                            filledSlots: 1,
                            totalSlots: 4)
                    }));

                var root = fixture.Document.rootVisualElement;
                var roomList = root.Q<VisualElement>("RoomList");
                Assert.AreEqual(1, roomList.childCount);
                var row = roomList[0] as Button;
                Assert.NotNull(row);
                Assert.IsTrue(row.ClassListContains("lobby-room-row--selected"));
                Assert.AreEqual("Alpha", row.Q<Label>(className: "lobby-room-row__title").text);
                Assert.AreEqual("1/4 | Normal", row.Q<Label>(className: "lobby-room-row__meta").text);
                Assert.AreEqual("참가 가능", row.Q<Label>(className: "lobby-status-chip").text);
                Assert.AreEqual("1/4", row.Q<Label>(className: "lobby-slot-text").text);
                Assert.AreEqual(DisplayStyle.Flex, root.Q<ScrollView>("RoomListScroll").style.display.value);
                Assert.AreEqual(DisplayStyle.None, root.Q<VisualElement>("RoomListEmptyStateCard").style.display.value);
                Assert.AreEqual(DisplayStyle.Flex, root.Q<VisualElement>("CreateRoomCard").style.display.value);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void RenderRooms_EmptyShowsEmptyStateCard()
        {
            var fixture = CreateFixture();
            try
            {
                fixture.Adapter.RenderRooms(LobbyRoomListViewModel.Empty);

                var root = fixture.Document.rootVisualElement;
                Assert.AreEqual(DisplayStyle.Flex, root.Q<VisualElement>("RoomListEmptyStateCard").style.display.value);
                Assert.AreEqual(DisplayStyle.None, root.Q<ScrollView>("RoomListScroll").style.display.value);
                Assert.AreEqual(DisplayStyle.None, root.Q<VisualElement>("RoomList").style.display.value);
                Assert.AreEqual("열린 방이 없습니다.", root.Q<Label>("RoomListEmptyStateBodyLabel").text);
                Assert.AreEqual(DisplayStyle.None, root.Q<VisualElement>("CreateRoomCard").style.display.value);
                Assert.AreEqual("작전 개설", root.Q<Button>("EmptyStateCreateButton").Q<Label>(className: "lobby-button-label").text);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void Uxml_LobbyHomeStartsWithRoomListAndHasNoCallsignHero()
        {
            var root = LoadLobbyShellRoot();

            Assert.IsNull(root.Q<VisualElement>("LobbyHeaderCard"));
            Assert.IsNull(root.Q<TextField>("DisplayNameInput"));
            Assert.NotNull(root.Q<ScrollView>("RoomListScroll"));
            AssertNoLabelText(root, "실시간 매칭과 편성 진입");
            AssertNoLabelText(root, "콜사인");
            Assert.AreEqual("열린 방",
                root.Q<VisualElement>("RoomsSectionCard").Q<Label>(className: "uitk-section-title").text);
            Assert.AreEqual("작전 개설",
                root.Q<Button>("CreateRoomOpenButton").Q<Label>(className: "lobby-button-label").text);
            Assert.AreEqual("작전 개설",
                root.Q<Button>("EmptyStateCreateButton").Q<Label>(className: "lobby-button-label").text);
        }

        [Test]
        public void RenderGarageSummary_MapsStatusSummaryWithoutCta()
        {
            var fixture = CreateFixture();
            try
            {
                fixture.Adapter.RenderGarageSummary(new LobbyGarageSummaryViewModel(
                    "출격 가능",
                    "현역 3/8",
                    "저장된 편성이 최소 출격 기준을 충족합니다.",
                    filledSlots: 3,
                    totalSlots: 8,
                    isReady: true));

                var root = fixture.Document.rootVisualElement;
                Assert.AreEqual("출격 가능", root.Q<Label>("GarageSummaryStatusLabel").text);
                Assert.AreEqual("현역 3/8", root.Q<Label>("GarageSummaryTitleLabel").text);
                Assert.AreEqual("저장된 편성이 최소 출격 기준을 충족합니다.", root.Q<Label>("GarageSummaryBodyLabel").text);
                Assert.IsTrue(root.Q<Label>("GarageSummaryStatusLabel").ClassListContains("lobby-status-chip--ready"));
                Assert.AreEqual(9, root.Q<VisualElement>("GarageSummarySlotRow").childCount);
                Assert.IsNull(root.Q<Button>("OpenGarageButton"));
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void RenderRoomSelection_MapsViewModelToOverlayState()
        {
            var fixture = CreateFixture();
            try
            {
                fixture.Adapter.RenderRoomSelection(new LobbyRoomSelectionViewModel(
                    new DomainEntityId("room-1"),
                    "Alpha",
                    "2/4 | Normal",
                    "참가 가능",
                    "현재 열린 작전입니다.",
                    filledSlots: 2,
                    totalSlots: 4,
                    canJoin: true));

                var root = fixture.Document.rootVisualElement;
                Assert.AreEqual(DisplayStyle.Flex, root.Q<VisualElement>("RoomSelectionOverlay").style.display.value);
                Assert.AreEqual("Alpha", root.Q<Label>("RoomSelectionTitleLabel").text);
                Assert.AreEqual("2/4 | Normal", root.Q<Label>("RoomSelectionMetaLabel").text);
                Assert.AreEqual("참가 가능", root.Q<Label>("RoomSelectionStatusLabel").text);
                Assert.AreEqual(5, root.Q<VisualElement>("RoomSelectionSlotRow").childCount);
                Assert.IsTrue(root.Q<Button>("JoinSelectedRoomButton").enabledSelf);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void RenderRoomDetail_MapsViewModelToActionState()
        {
            var fixture = CreateFixture();
            try
            {
                fixture.Adapter.RenderRoomDetail(new LobbyRoomDetailViewModel(
                    "Alpha",
                    "1/4 | Normal",
                    new[] { "Pilot | Red | READY" },
                    localIsReady: true,
                    readyButtonText: "Cancel",
                    canStartGame: true));

                var root = fixture.Document.rootVisualElement;
                Assert.AreEqual("Cancel", root.Q<Button>("ReadyButton").text);
                Assert.IsTrue(root.Q<Button>("StartButton").enabledSelf);
                Assert.AreEqual(1, root.Q<VisualElement>("MemberList").childCount);
                Assert.AreEqual(DisplayStyle.Flex,
                    root.Q<VisualElement>("RoomDetailCard").style.display.value);
                Assert.AreEqual(DisplayStyle.Flex,
                    root.Q<VisualElement>("RoomActionRow").style.display.value);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void RenderRoomDetail_EmptyHidesActionRow()
        {
            var fixture = CreateFixture();
            try
            {
                fixture.Adapter.RenderRoomDetail(LobbyRoomDetailViewModel.Empty);

                var root = fixture.Document.rootVisualElement;
                Assert.AreEqual(DisplayStyle.None,
                    root.Q<VisualElement>("RoomDetailCard").style.display.value);
                Assert.AreEqual(DisplayStyle.None,
                    root.Q<VisualElement>("RoomActionRow").style.display.value);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void RenderRoomDetail_WithMembersShowsActionRow()
        {
            var fixture = CreateFixture();
            try
            {
                fixture.Adapter.RenderRoomDetail(new LobbyRoomDetailViewModel(
                    "Alpha",
                    "1/4 | Normal",
                    new[] { "Pilot | Red | READY" },
                    localIsReady: false,
                    readyButtonText: "Ready",
                    canStartGame: false));

                var root = fixture.Document.rootVisualElement;
                Assert.AreEqual(DisplayStyle.Flex,
                    root.Q<VisualElement>("RoomActionRow").style.display.value);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void CommandButtons_PublishSemanticClickSoundsOnce()
        {
            var fixture = CreateFixture();
            try
            {
                var eventBus = new EventBus();
                var soundKeys = new List<string>();
                eventBus.Subscribe<SoundRequestEvent>(this, e => soundKeys.Add(e.Request.SoundKey));
                fixture.Adapter.SetClickSoundPublisher(eventBus);
                Assert.IsTrue(fixture.Adapter.Bind());

                var root = fixture.Document.rootVisualElement;
                ClickButton(Button(root, "CreateRoomButton"));
                ClickButton(Button(root, "GarageNavButton"));
                ClickButton(Button(root, "CreateRoomCancelButton"));

                CollectionAssert.AreEqual(
                    new[] { "ui_confirm", "ui_select", "ui_back" },
                    soundKeys);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void AuxiliaryCommandButtons_PublishSemanticClickSounds()
        {
            var fixture = CreateFixture();
            try
            {
                var eventBus = new EventBus();
                var soundKeys = new List<string>();
                eventBus.Subscribe<SoundRequestEvent>(this, e => soundKeys.Add(e.Request.SoundKey));
                fixture.Adapter.SetClickSoundPublisher(eventBus);

                fixture.Adapter.ShowAccountPage();
                fixture.Adapter.ShowConnectionPage();

                var root = fixture.Document.rootVisualElement;
                ClickButton(Button(root, "ManualSyncRetryButton"));
                ClickButton(Button(root, "LinkAccountButton"));
                ClickButton(Button(root, "ManualRetryButton"));

                CollectionAssert.AreEqual(
                    new[] { "ui_retry", "ui_click", "ui_retry" },
                    soundKeys);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
        }

        [Test]
        public void LobbyRoomInputHandler_DoesNotPublishCommandClickSounds()
        {
            var source = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts",
                "Features",
                "Lobby",
                "Presentation",
                "Input",
                "LobbyRoomInputHandler.cs"));

            StringAssert.DoesNotContain("SoundRequestEvent", source);
            StringAssert.DoesNotContain("PublishSound", source);
        }

        [Test]
        public void Bind_MissingRequiredShellElementThrows()
        {
            var documentObject = new GameObject("LobbyUitkMissingRequiredTest");
            try
            {
                var document = documentObject.AddComponent<UIDocument>();
                document.rootVisualElement.Add(new VisualElement { name = "LobbyShellScreen" });
                var adapter = new LobbyUitkRuntimeAdapter(
                    document,
                    garageDocument: null,
                    garageAdapter: null,
                    lobbyShellTree: null,
                    operationMemoryTree: null,
                    accountSyncTree: null,
                    connectionReconnectTree: null,
                    documentObject);

                var exception = Assert.Throws<InvalidOperationException>(() => adapter.Bind());
                StringAssert.Contains("Lobby UITK element not found", exception.Message);

                var secondException = Assert.Throws<InvalidOperationException>(() => adapter.Bind());
                StringAssert.Contains("Lobby UITK element not found", secondException.Message);
            }
            finally
            {
                Object.DestroyImmediate(documentObject);
            }
        }

        [Test]
        public void PageController_RequiresOperationRecordStoreAtInitialize()
        {
            var controllerObject = new GameObject("LobbyPageControllerMissingStoreTest");
            try
            {
                var controller = controllerObject.AddComponent<LobbyPageController>();

                var exception = Assert.Throws<ArgumentNullException>(() =>
                    controller.Initialize(
                        eventBus: null,
                        eventPublisher: null,
                        useCases: null,
                        operationRecordStore: null));

                Assert.AreEqual("operationRecordStore", exception.ParamName);
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
            }
        }

        private static AdapterFixture CreateFixture()
        {
            var documentObject = new GameObject("LobbyUitkRuntimeAdapterTest");
            var document = documentObject.AddComponent<UIDocument>();
            var adapter = new LobbyUitkRuntimeAdapter(
                document,
                garageDocument: null,
                garageAdapter: null,
                LoadTree(LobbyShellPath),
                LoadTree(OperationMemoryPath),
                LoadTree(AccountSyncPath),
                LoadTree(ConnectionPath),
                documentObject);

            return new AdapterFixture(documentObject, document, adapter);
        }

        private static VisualTreeAsset LoadTree(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            Assert.NotNull(asset, $"UXML not found: {path}");
            return asset;
        }

        private static VisualElement LoadLobbyShellRoot()
        {
            var root = new VisualElement();
            LoadTree(LobbyShellPath).CloneTree(root);
            return root;
        }

        private static void AssertPageVisible(VisualElement root, string pageName)
        {
            Assert.AreEqual(DisplayStyle.Flex, root.Q<VisualElement>(pageName).style.display.value);
        }

        private static void AssertPageHidden(VisualElement root, string pageName)
        {
            Assert.AreEqual(DisplayStyle.None, root.Q<VisualElement>(pageName).style.display.value);
        }

        private static Button Button(VisualElement root, string name)
        {
            var button = root.Q<Button>(name);
            Assert.NotNull(button, name);
            return button;
        }

        private static void ClickButton(Button button)
        {
            Assert.AreEqual("Button.clicked", UitkHandlers.InvokeElementForTest(button, "click"));
        }

        private static void AssertNoLabelText(VisualElement root, string text)
        {
            var labels = root.Query<Label>().ToList();
            for (var i = 0; i < labels.Count; i++)
                Assert.AreNotEqual(text, labels[i].text);
        }

        private readonly struct AdapterFixture
        {
            public AdapterFixture(
                GameObject documentObject,
                UIDocument document,
                LobbyUitkRuntimeAdapter adapter)
            {
                DocumentObject = documentObject;
                Document = document;
                Adapter = adapter;
            }

            public GameObject DocumentObject { get; }
            public UIDocument Document { get; }
            public LobbyUitkRuntimeAdapter Adapter { get; }
        }
    }
}
