using System;
using System.Collections.Generic;
using Shared.Kernel;
using Shared.Runtime;
using UnityEngine;

namespace Features.Player
{
    public sealed class PlayerSceneRegistry : MonoBehaviour
    {
        private readonly Dictionary<string, PlayerSetup> _players = new();
        private readonly PendingArrivalBuffer<PlayerSetup> _pendingArrivals = new();

        public event System.Action<PlayerSetup> PlayerArrived;

        public IReadOnlyCollection<PlayerSetup> All => _players.Values;

        public void NotifyArrived(PlayerSetup setup)
        {
            if (setup == null)
            {
                Debug.LogError("[PlayerSceneRegistry] Arrived PlayerSetup is missing.", this);
                return;
            }

            _pendingArrivals.Notify(setup, PlayerArrived);
        }

        public void DrainPendingArrivals(System.Action<PlayerSetup> handler)
        {
            if (handler == null)
                return;

            _pendingArrivals.Drain(handler);
        }

        public bool TryRegister(PlayerSetup setup)
        {
            if (setup == null)
            {
                Debug.LogError("[PlayerSceneRegistry] PlayerSetup is missing.", this);
                return false;
            }

            var key = setup.PlayerId.Value;
            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogError("[PlayerSceneRegistry] PlayerId is missing.", setup);
                return false;
            }

            if (_players.ContainsKey(key))
            {
                Debug.LogWarning($"[PlayerSceneRegistry] Already registered: {key}", setup);
                return false;
            }

            _players[key] = setup;
            return true;
        }

        public bool IsRegistered(DomainEntityId playerId)
        {
            return !string.IsNullOrWhiteSpace(playerId.Value)
                && _players.ContainsKey(playerId.Value);
        }

        public bool TryGet(DomainEntityId playerId, out PlayerSetup setup)
        {
            if (string.IsNullOrWhiteSpace(playerId.Value))
            {
                setup = null;
                return false;
            }

            return _players.TryGetValue(playerId.Value, out setup);
        }
    }
}

namespace Shared.Runtime
{
    internal sealed class PendingArrivalBuffer<T> where T : class
    {
        private readonly Queue<T> _pending = new();

        public void Notify(T item, Action<T> handler)
        {
            if (handler == null)
            {
                _pending.Enqueue(item);
                return;
            }

            handler.Invoke(item);
        }

        public void Drain(Action<T> handler)
        {
            if (handler == null)
                return;

            while (_pending.Count > 0)
                handler(_pending.Dequeue());
        }
    }
}
