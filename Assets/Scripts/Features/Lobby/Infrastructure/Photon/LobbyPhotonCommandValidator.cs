using Photon.Pun;
using Shared.Kernel;

namespace Features.Lobby.Infrastructure.Photon
{
    internal static class LobbyPhotonCommandValidator
    {
        public static Result ValidateConnected() =>
            PhotonNetwork.IsConnectedAndReady
                ? Result.Success()
                : Result.Failure("Photon is not connected and ready.");

        public static Result ValidateNotInRoom() =>
            !PhotonNetwork.InRoom ? Result.Success() : Result.Failure("Already in a room.");

        public static Result ValidateInRoom() =>
            PhotonNetwork.InRoom ? Result.Success() : Result.Failure("Not in a room.");

        public static Result ValidateLocalMember(PhotonPlayerPropertyManager propertyManager, DomainEntityId memberId)
        {
            if (!propertyManager.TryGetLocalMemberId(out var localMemberId))
                return Result.Failure("Local member id is missing.");

            return localMemberId.Equals(memberId)
                ? Result.Success()
                : Result.Failure("Can only modify local member.");
        }
    }
}
