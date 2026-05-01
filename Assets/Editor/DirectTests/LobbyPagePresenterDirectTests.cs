using System.Collections.Generic;
using Features.Account.Application;
using Features.Account.Domain;
using Features.Garage.Domain;
using Features.Lobby.Application.Events;
using Features.Lobby.Application.Ports;
using Features.Lobby.Domain;
using Features.Lobby.Presentation;
using Features.Player.Domain;
using NUnit.Framework;
using Shared.Kernel;

namespace Tests.Editor
{
    public sealed class LobbyPagePresenterDirectTests
    {
        [Test]
        public void BuildRooms_MapsRoomSnapshotRows()
        {
            var presenter = new LobbyPagePresenter();
            var room = CreateRoom("room-1", "Alpha", capacity: 4, ownerId: "pilot-1");

            var viewModel = presenter.BuildRooms(new[] { new RoomSnapshot(room) });

            Assert.AreEqual("1 open room", viewModel.CountText);
            Assert.AreEqual(1, viewModel.Rows.Count);
            Assert.AreEqual(new DomainEntityId("room-1"), viewModel.Rows[0].RoomId);
            Assert.AreEqual("Alpha  1/4  Normal", viewModel.Rows[0].Text);
            Assert.IsTrue(viewModel.Rows[0].IsEnabled);
        }

        [Test]
        public void BuildRooms_DisablesClosedNetworkRoomRows()
        {
            var presenter = new LobbyPagePresenter();

            var viewModel = presenter.BuildRooms(new[]
            {
                new RoomListItem(
                    new DomainEntityId("room-1"),
                    "Closed",
                    playerCount: 1,
                    maxPlayers: 4,
                    isOpen: false)
            });

            Assert.AreEqual("1 open room", viewModel.CountText);
            Assert.IsFalse(viewModel.Rows[0].IsEnabled);
        }

        [Test]
        public void BuildRoomDetail_MapsLocalReadyAndOwnerState()
        {
            var presenter = new LobbyPagePresenter();
            var room = CreateRoom("room-1", "Alpha", capacity: 4, ownerId: "pilot-1");
            room.SetReady(new DomainEntityId("pilot-1"), true);

            var viewModel = presenter.BuildRoomDetail(
                new RoomSnapshot(room),
                new DomainEntityId("pilot-1"));

            Assert.AreEqual("Alpha", viewModel.TitleText);
            Assert.AreEqual("1/4 | Normal", viewModel.MetaText);
            Assert.AreEqual("Pilot | Red | READY", viewModel.MemberRows[0]);
            Assert.IsTrue(viewModel.LocalIsReady);
            Assert.AreEqual("Cancel", viewModel.ReadyButtonText);
            Assert.IsTrue(viewModel.CanStartGame);
        }

        [Test]
        public void BuildAccount_MapsProfileSettingsAndOperationCount()
        {
            var presenter = new LobbyPagePresenter();
            var accountData = new AccountData
            {
                GarageRoster = new GarageRoster(new List<GarageRoster.UnitLoadout>
                {
                    new("frame-a", "fire-a", "move-a")
                }),
                Settings = new UserSettings
                {
                    bgmVolume = 0.35f,
                    sfxVolume = 0.6f
                }
            };
            var profile = new AccountProfile
            {
                uid = "abcdefghijklmnop",
                displayName = "Pilot One",
                authType = "google"
            };

            var viewModel = presenter.BuildAccount(profile, accountData, operationCount: 3);

            Assert.AreEqual("Pilot One", viewModel.PilotIdText);
            Assert.AreEqual("G-LINK OK", viewModel.GoogleLinkStatusText);
            Assert.AreEqual("UID abcdefgh", viewModel.UidStatusText);
            Assert.AreEqual("1/4", viewModel.GarageSyncStateText);
            Assert.AreEqual("3/5", viewModel.OperationSyncStateText);
            Assert.AreEqual("35%", viewModel.BgmValueText);
            Assert.AreEqual("60%", viewModel.SfxValueText);
            Assert.AreEqual("READY", viewModel.CloudModeText);
        }

        [Test]
        public void BuildOperationMemory_FormatsLatestAndRecentRows()
        {
            var presenter = new LobbyPagePresenter();
            var records = new RecentOperationRecords();
            records.AddOrReplace(new OperationRecord
            {
                operationId = "op-1",
                endedAtUnixMs = 0L,
                result = OperationRecordResult.BaseCollapsed,
                survivalSeconds = 83f,
                reachedWave = 7,
                hasCoreHealthPercent = true,
                coreHealthPercent = 0.42f,
                unitKillCount = 12,
                primaryRosterUnits = new List<string> { "frame|fire|move" }
            });

            var viewModel = presenter.BuildOperationMemory(records);

            Assert.IsTrue(viewModel.Latest.HasRecord);
            Assert.AreEqual("거점 붕괴", viewModel.Latest.ResultText);
            Assert.AreEqual("01:23", viewModel.Latest.SurvivalText);
            Assert.AreEqual("42%", viewModel.Latest.CoreText);
            Assert.AreEqual(1, viewModel.RecentRows.Count);
            StringAssert.Contains("공세 07", viewModel.RecentRows[0].MetaText);
            StringAssert.Contains("frame / fire / move", viewModel.RecentRows[0].MetaText);
            Assert.AreEqual("1/5 RECORDS STORED", viewModel.Trace.CountChipText);
        }

        private static Room CreateRoom(string roomId, string roomName, int capacity, string ownerId)
        {
            var owner = new RoomMember(new DomainEntityId(ownerId), "Pilot", TeamType.Red, isReady: false);
            var result = Room.Create(new DomainEntityId(roomId), roomName, capacity, owner);
            Assert.IsFalse(result.IsFailure, result.Error);
            return result.Value;
        }
    }
}
