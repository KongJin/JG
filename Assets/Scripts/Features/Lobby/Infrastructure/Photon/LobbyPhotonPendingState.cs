using Features.Lobby.Domain;
using Shared.Kernel;

namespace Features.Lobby.Infrastructure.Photon
{
    internal sealed class LobbyPhotonPendingState
    {
        private Room _pendingCreateRoom;
        private bool _pendingJoin;
        private DomainEntityId _pendingLeaveRoomId;
        private DomainEntityId _pendingLeaveMemberId;

        public void SetCreateRoom(Room room)
        {
            _pendingCreateRoom = room;
            _pendingJoin = false;
        }

        public void SetJoinRoom()
        {
            _pendingCreateRoom = null;
            _pendingJoin = true;
        }

        public void ClearJoin()
        {
            _pendingJoin = false;
        }

        public void ClearCreateRoom()
        {
            _pendingCreateRoom = null;
        }

        public void SetLeaveRoom(DomainEntityId roomId, DomainEntityId memberId)
        {
            _pendingLeaveRoomId = roomId;
            _pendingLeaveMemberId = memberId;
        }

        public Room TakePendingCreateRoom()
        {
            var room = _pendingCreateRoom;
            _pendingCreateRoom = null;
            return room;
        }

        public bool TryConsumeJoin()
        {
            if (!_pendingJoin)
                return false;

            _pendingJoin = false;
            return true;
        }

        public (DomainEntityId RoomId, DomainEntityId MemberId) TakePendingLeave()
        {
            var roomId = _pendingLeaveRoomId;
            var memberId = _pendingLeaveMemberId;
            _pendingLeaveRoomId = default;
            _pendingLeaveMemberId = default;
            return (roomId, memberId);
        }

        public void Clear()
        {
            _pendingCreateRoom = null;
            _pendingJoin = false;
            _pendingLeaveRoomId = default;
            _pendingLeaveMemberId = default;
        }
    }
}
