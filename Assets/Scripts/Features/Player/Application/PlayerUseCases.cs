using Features.Player.Application.Events;
using Features.Player.Application.Ports;
using Features.Player.Domain;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Math;
using Shared.Time;

namespace Features.Player.Application
{
    public sealed class PlayerUseCases
    {
        private readonly IPlayerMotorPort _motor;
        private readonly IPlayerNetworkCommandPort _network;
        private readonly IEventPublisher _eventBus;
        private readonly IClockPort _clock;

        private Domain.Player _localPlayer;

        public PlayerUseCases(
            IPlayerMotorPort motor,
            IPlayerNetworkCommandPort network,
            IEventPublisher eventBus,
            IClockPort clock
        )
        {
            _motor = motor;
            _network = network;
            _eventBus = eventBus;
            _clock = clock;
        }

        public Result<Domain.Player> Spawn(PlayerSpec spec)
        {
            return Spawn(spec, default);
        }

        public Result<Domain.Player> Spawn(PlayerSpec spec, DomainEntityId playerId)
        {
            var resolvedPlayerId = string.IsNullOrWhiteSpace(playerId.Value)
                ? _clock.NewId()
                : playerId;
            var player = new Domain.Player(resolvedPlayerId, spec);
            _localPlayer = player;
            return Result<Domain.Player>.Success(player);
        }

        public static Result<Domain.Player> SpawnRemote(PlayerSpec spec, DomainEntityId playerId)
        {
            var player = new Domain.Player(playerId, spec);
            return Result<Domain.Player>.Success(player);
        }

        public Result Move(Domain.Player player, Float2 moveInput, float deltaTime)
        {
            var delta = new Float3(moveInput.X, 0f, moveInput.Y);
            var result = _motor.Move(delta);
            player.ApplyMovement(result.Position, result.IsGrounded);
            return Result.Success();
        }

        public Domain.Player LocalPlayer => _localPlayer;

        public void Respawn()
        {
            if (_localPlayer == null)
                return;

            _localPlayer.Respawn();
            _network.SendRespawn(_localPlayer.Id);
            _eventBus.Publish(new PlayerRespawnedEvent(
                _localPlayer.Id,
                _localPlayer.CurrentHp,
                _localPlayer.MaxHp
            ));
            _eventBus.Publish(new PlayerManaChangedEvent(
                _localPlayer.Id,
                _localPlayer.CurrentMana,
                _localPlayer.MaxMana
            ));
            _network.SyncMana(_localPlayer.Id, _localPlayer.CurrentMana, _localPlayer.MaxMana);
        }
    }
}
