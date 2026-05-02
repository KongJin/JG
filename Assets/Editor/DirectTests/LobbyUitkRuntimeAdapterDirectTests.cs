using Features.Lobby.Presentation;
using NUnit.Framework;
using Shared.Kernel;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
                            "Alpha  1/4  Normal",
                            isEnabled: true)
                    }));

                var roomList = fixture.Document.rootVisualElement.Q<VisualElement>("RoomList");
                Assert.AreEqual(1, roomList.childCount);
                var row = roomList[0] as Button;
                Assert.NotNull(row);
                StringAssert.Contains("Alpha", row.text);
                StringAssert.Contains("1/4", row.text);
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
            }
            finally
            {
                Object.DestroyImmediate(fixture.DocumentObject);
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
