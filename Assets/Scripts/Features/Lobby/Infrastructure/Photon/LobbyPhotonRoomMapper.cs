using System.Collections.Generic;
using ExitGames.Client.Photon;
using Features.Lobby.Domain;
using Photon.Realtime;
using Shared.Kernel;
using PhotonPlayer = Photon.Realtime.Player;
using PhotonRoom = Photon.Realtime.Room;

namespace Features.Lobby.Infrastructure.Photon
{
    internal static class LobbyPhotonRoomMapper
    {
        public static int ReadDifficultyPresetFromProps(Hashtable props)
        {
            if (props == null ||
                !props.TryGetValue(LobbyPhotonConstants.DifficultyPresetKey, out var raw) ||
                raw == null)
                return 0;

            return raw switch
            {
                int i => i,
                byte b => b,
                short s => s,
                long l => (int)l,
                _ => 0,
            };
        }

        public static RoomMember BuildMemberFromPlayer(PhotonPlayer player)
        {
            if (
                !player.CustomProperties.TryGetValue(
                    LobbyPhotonConstants.MemberIdKey,
                    out var midRaw
                ) || midRaw is not string midStr
            )
                return null;

            var memberId = new DomainEntityId(midStr);
            var displayName =
                player.CustomProperties.TryGetValue(
                    LobbyPhotonConstants.DisplayNameKey,
                    out var dnRaw
                ) && dnRaw is string dnStr
                    ? dnStr
                    : player.NickName ?? "Player";
            var team =
                player.CustomProperties.TryGetValue(LobbyPhotonConstants.TeamKey, out var tRaw)
                && tRaw is int tInt
                    ? (TeamType)tInt
                    : TeamType.None;
            var isReady =
                player.CustomProperties.TryGetValue(LobbyPhotonConstants.IsReadyKey, out var rRaw)
                && rRaw is bool rBool
                && rBool;

            return new RoomMember(memberId, displayName, team, isReady);
        }

        public static List<RoomMember> BuildMembersFromPlayers(PhotonRoom photonRoom)
        {
            var members = new List<RoomMember>();
            foreach (var player in photonRoom.Players.Values)
            {
                var member = BuildMemberFromPlayer(player);
                if (member != null)
                    members.Add(member);
            }
            return members;
        }
    }
}
