using System;
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
        private readonly ISpeedModifierPort _speedModifier;
        private readonly IPlayerLookupPort _playerLookup;

        private Domain.Player _localPlayer;

        public PlayerUseCases(
            IPlayerMotorPort motor,
            IPlayerNetworkCommandPort network,
            IEventPublisher eventBus,
            IClockPort clock,
            ISpeedModifierPort speedModifier = null,
            IEventSubscriber subscriber = null,
            IPlayerLookupPort playerLookup = null
        )
        {
            _motor = motor;
            _network = network;
            _eventBus = eventBus;
            _clock = clock;
            _speedModifier = speedModifier;
            _playerLookup = playerLookup;

            if (subscriber != null)
            {
                subscriber.Subscribe(this, new Action<PlayerDownedEvent>(OnPlayerDowned));
                subscriber.Subscribe(this, new Action<PlayerDiedEvent>(OnPlayerDied));
                subscriber.Subscribe(this, new Action<PlayerRescuedEvent>(OnPlayerRescued));
                subscriber.Subscribe(this, new Action<PlayerRespawnedEvent>(OnPlayerRespawned));
            }
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
            var modifiedSpeed = _speedModifier != null
                ? _speedModifier.GetModifiedSpeed(player.Id, player.Spec.WalkSpeed)
                : -1f;
            var delta = player.CalculateMovement(moveInput, deltaTime, modifiedSpeed);
            var result = _motor.Move(delta);

            _motor.Rotate(new Float3(moveInput.X, 0f, moveInput.Y), player.Spec.RotationSpeed, deltaTime);

            player.ApplyMovement(result.Position, result.IsGrounded);
            return Result.Success();
        }

        public Result Jump(Domain.Player player)
        {
            if (!player.TryJump())
                return Result.Failure("Cannot jump while airborne.");

            _network.SendJump(player.Id);
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

        public Result StartRescueChannel(DomainEntityId rescuerId, DomainEntityId targetId)
        {
            if (_localPlayer == null || !_localPlayer.IsAlive)
                return Result.Failure("Rescuer is not alive.");

            _network.SendRescueChannelStart(rescuerId, targetId);
            return Result.Success();
        }

        public Result CancelRescueChannel(DomainEntityId targetId)
        {
            _network.SendRescueChannelCancel(targetId);
            return Result.Success();
        }

        public Result CompleteRescue(DomainEntityId rescuerId, DomainEntityId targetId)
        {
            if (_localPlayer == null || !_localPlayer.IsAlive)
                return Result.Failure("Rescuer is not alive.");

            var target = _playerLookup?.Resolve(targetId);
            if (target == null || !target.IsDowned)
                return Result.Failure("Target is not downed.");

            _network.SendRescue(rescuerId, targetId);
            return Result.Success();
        }

        public DomainEntityId FindRescueTarget(Float3 rescuerPosition)
        {
            if (_localPlayer == null || _playerLookup == null)
                return default;

            var bestId = default(DomainEntityId);
            var bestDistSq = float.MaxValue;

            foreach (var entry in _playerLookup.AllEntries())
            {
                if (entry.Id.Equals(_localPlayer.Id))
                    continue;

                if (entry.Player == null || !entry.Player.IsDowned)
                    continue;

                if (!RescueRule.IsInRange(rescuerPosition, entry.Position))
                    continue;

                var dx = rescuerPosition.X - entry.Position.X;
                var dy = rescuerPosition.Y - entry.Position.Y;
                var dz = rescuerPosition.Z - entry.Position.Z;
                var distSq = dx * dx + dy * dy + dz * dz;

                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestId = entry.Id;
                }
            }

            return bestId;
        }

        private void OnPlayerDowned(PlayerDownedEvent e)
        {
            if (_localPlayer == null || !_localPlayer.Id.Equals(e.PlayerId))
                return;

            _network.SyncLifeState(_localPlayer.Id, LifeState.Downed);
        }

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            if (_localPlayer == null || !_localPlayer.Id.Equals(e.PlayerId))
                return;

            _network.SyncLifeState(_localPlayer.Id, LifeState.Dead);
        }

        private void OnPlayerRescued(PlayerRescuedEvent e)
        {
            if (_localPlayer == null || !_localPlayer.Id.Equals(e.RescuedId))
                return;

            _network.SyncLifeState(_localPlayer.Id, LifeState.Alive);
        }

        private void OnPlayerRespawned(PlayerRespawnedEvent e)
        {
            if (_localPlayer == null || !_localPlayer.Id.Equals(e.PlayerId))
                return;

            _network.SyncLifeState(_localPlayer.Id, LifeState.Alive);
        }
    }
}
