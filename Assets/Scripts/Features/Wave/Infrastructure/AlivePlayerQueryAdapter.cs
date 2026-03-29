using System;
using System.Collections.Generic;
using Features.Player.Application.Events;
using Features.Wave.Application.Ports;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Wave.Infrastructure
{
    public sealed class AlivePlayerQueryAdapter : IAlivePlayerQuery
    {
        private readonly HashSet<string> _deadPlayerIds = new HashSet<string>();
        private readonly int _totalPlayerCount;

        public AlivePlayerQueryAdapter(IEventSubscriber subscriber, int totalPlayerCount)
        {
            _totalPlayerCount = totalPlayerCount;
            subscriber.Subscribe(this, new Action<PlayerDiedEvent>(OnPlayerDied));
            subscriber.Subscribe(this, new Action<PlayerRespawnedEvent>(OnPlayerRespawned));
        }

        public bool AnyPlayerAlive()
        {
            return _deadPlayerIds.Count < _totalPlayerCount;
        }

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            _deadPlayerIds.Add(e.PlayerId.Value);
        }

        private void OnPlayerRespawned(PlayerRespawnedEvent e)
        {
            _deadPlayerIds.Remove(e.PlayerId.Value);
        }
    }
}
