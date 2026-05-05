using Features.Lobby.Application;
using Features.Lobby.Application.Events;
using Features.Lobby.Domain;
using Shared.ErrorHandling;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Lobby.Presentation
{
    public sealed class LobbyRoomInputHandler
    {
        private readonly LobbyUseCases _useCases;
        private readonly IEventPublisher _eventPublisher;

        public LobbyRoomInputHandler(LobbyUseCases useCases, IEventPublisher eventPublisher)
        {
            _useCases = useCases;
            _eventPublisher = eventPublisher;
        }

        public Result CreateRoom(string roomName, int capacity, string ownerDisplayName, int difficultyPresetId)
        {
            return PublishFailureIfNeeded(
                _useCases.CreateRoom(roomName, capacity, ownerDisplayName, difficultyPresetId));
        }

        public Result JoinRoom(DomainEntityId roomId, string memberDisplayName)
        {
            return PublishFailureIfNeeded(_useCases.JoinRoom(roomId, memberDisplayName));
        }

        public Result LeaveRoom(DomainEntityId roomId, DomainEntityId memberId)
        {
            return PublishFailureIfNeeded(_useCases.LeaveRoom(roomId, memberId));
        }

        public Result ChangeTeam(DomainEntityId roomId, DomainEntityId memberId, TeamType team)
        {
            return PublishFailureIfNeeded(_useCases.ChangeTeam(roomId, memberId, team));
        }

        public Result SetReady(DomainEntityId roomId, DomainEntityId memberId, bool isReady)
        {
            return PublishFailureIfNeeded(_useCases.SetReady(roomId, memberId, isReady));
        }

        public Result StartGame(DomainEntityId roomId)
        {
            return PublishFailureIfNeeded(_useCases.StartGame(roomId));
        }

        public void PublishFailure(string error)
        {
            PublishFailureIfNeeded(Result.Failure(error));
        }

        private Result PublishFailureIfNeeded(Result result)
        {
            UiErrorResultBridge.PublishBannerIfFailure(_eventPublisher, result, "Lobby");
            return result;
        }
    }
}
