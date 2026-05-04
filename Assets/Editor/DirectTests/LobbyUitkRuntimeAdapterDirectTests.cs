using Features.Lobby.Presentation;
using NUnit.Framework;
using Shared.Kernel;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public sealed class LobbyUitkRuntimeAdapterDirectTests
    {
        private const string LobbyShellPath = "Assets/UI/UIToolkit/Lobby/LobbyShell.uxml";
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

                var roomList = fixture.Document.rootVisualElement.Q<VisualElement>("RoomList");
                Assert.AreEqual(1, roomList.childCount);
                var row = roomList[0] as Button;
                Assert.NotNull(row);
                Assert.IsTrue(row.ClassListContains("lobby-room-row--selected"));
                Assert.AreEqual("Alpha", row.Q<Label>(className: "lobby-room-row__title").text);
                Assert.AreEqual("1/4 | Normal", row.Q<Label>(className: "lobby-room-row__meta").text);
                Assert.AreEqual("참가 가능", row.Q<Label>(className: "lobby-status-chip").text);
                Assert.AreEqual("1/4", row.Q<Label>(className: "lobby-slot-text").text);
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
                Assert.AreEqual(DisplayStyle.None, root.Q<VisualElement>("RoomList").style.display.value);
                Assert.AreEqual("열린 방이 없습니다.", root.Q<Label>("RoomListEmptyStateBodyLabel").text);
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
            }
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

        private static void AssertPageVisible(VisualElement root, string pageName)
        {
            Assert.AreEqual(DisplayStyle.Flex, root.Q<VisualElement>(pageName).style.display.value);
        }

        private static void AssertPageHidden(VisualElement root, string pageName)
        {
            Assert.AreEqual(DisplayStyle.None, root.Q<VisualElement>(pageName).style.display.value);
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
