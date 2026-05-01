using Features.Lobby.Application.Events;
using Features.Lobby.Domain;
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
        public void RenderRooms_MapsRoomSnapshotToRows()
        {
            var fixture = CreateFixture();
            try
            {
                var room = CreateRoom("room-1", "Alpha", capacity: 4, ownerId: "pilot-1");

                fixture.Adapter.RenderRooms(new[] { new RoomSnapshot(room) });

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
        public void RenderRoomDetail_ReturnsLocalReadyAndUpdatesActionState()
        {
            var fixture = CreateFixture();
            try
            {
                var room = CreateRoom("room-1", "Alpha", capacity: 4, ownerId: "pilot-1");
                room.SetReady(new DomainEntityId("pilot-1"), true);

                bool localReady = fixture.Adapter.RenderRoomDetail(
                    new RoomSnapshot(room),
                    new DomainEntityId("pilot-1"));

                var root = fixture.Document.rootVisualElement;
                Assert.IsTrue(localReady);
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

        private static Room CreateRoom(string roomId, string roomName, int capacity, string ownerId)
        {
            var owner = new RoomMember(new DomainEntityId(ownerId), "Pilot", TeamType.Red, isReady: false);
            var result = Room.Create(new DomainEntityId(roomId), roomName, capacity, owner);
            Assert.IsFalse(result.IsFailure, result.Error);
            return result.Value;
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
