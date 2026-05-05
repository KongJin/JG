using Features.Lobby.Domain;
using NUnit.Framework;
using Shared.Kernel;

namespace Tests.Editor
{
    public sealed class LobbyRoomStartRuleDirectTests
    {
        [Test]
        public void CanStartGame_AllowsOneReadyMemberAndIgnoresEmptySlots()
        {
            var room = CreateRoom(capacity: 4, ownerReady: true);

            Assert.IsTrue(room.CanStartGame());
            Assert.IsTrue(LobbyRule.CanStartGame(room).IsSuccess);
        }

        [Test]
        public void CanStartGame_BlocksUnreadyMember()
        {
            var room = CreateRoom(capacity: 4, ownerReady: true);
            room.AddMember(new RoomMember(
                new DomainEntityId("pilot-2"),
                "Pilot 2",
                TeamType.Blue,
                isReady: false));

            var result = LobbyRule.CanStartGame(room);

            Assert.IsFalse(room.CanStartGame());
            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual("All current room members must be ready.", result.Error);
        }

        [Test]
        public void CanStartGame_AllowsPartiallyFilledRoomWhenCurrentMembersAreReady()
        {
            var room = CreateRoom(capacity: 4, ownerReady: true);
            room.AddMember(new RoomMember(
                new DomainEntityId("pilot-2"),
                "Pilot 2",
                TeamType.Blue,
                isReady: true));

            Assert.IsTrue(room.CanStartGame());
            Assert.IsTrue(LobbyRule.CanStartGame(room).IsSuccess);
        }

        private static Room CreateRoom(int capacity, bool ownerReady)
        {
            var owner = new RoomMember(
                new DomainEntityId("pilot-1"),
                "Pilot",
                TeamType.Red,
                ownerReady);
            var result = Room.Create(new DomainEntityId("room-1"), "Alpha", capacity, owner);
            Assert.IsFalse(result.IsFailure, result.Error);
            return result.Value;
        }
    }
}
