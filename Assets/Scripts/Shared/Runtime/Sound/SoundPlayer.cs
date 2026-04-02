using Shared.Attributes;
using System;
using System.Collections.Generic;
using Shared.EventBus;
using Shared.Lifecycle;
using Shared.Math;
using Shared.Runtime.Pooling;
using Shared.Sound;
using UnityEngine;

namespace Shared.Runtime.Sound
{
    public sealed class SoundPlayer : MonoBehaviour
    {
        [Required, SerializeField] private GameObject audioSourcePrefab;
        [Required, SerializeField] private SoundCatalog catalog;
        [SerializeField] private int initialPoolSize = 8;

        private IEventSubscriber _eventBus;
        private string _localPlayerId;
        private GameObjectPool _pool;
        private DisposableScope _disposables;

        private readonly Dictionary<string, float> _lastPlayTime = new Dictionary<string, float>();
        private readonly Dictionary<GameObject, PooledAudioSource> _cache =
            new Dictionary<GameObject, PooledAudioSource>();

        public void Initialize(IEventSubscriber eventBus, string localPlayerId)
        {
            _eventBus = eventBus;
            _localPlayerId = localPlayerId;

            _disposables?.Dispose();
            _disposables = null;

            _pool = new GameObjectPool(audioSourcePrefab, transform, initialPoolSize);

            _disposables = new DisposableScope();
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, this));
            _eventBus.Subscribe(this, new Action<SoundRequestEvent>(OnSoundRequested));
        }

        private void OnDestroy()
        {
            _disposables?.Dispose();
        }

        private void OnSoundRequested(SoundRequestEvent e)
        {
            var req = e.Request;

            if (!ShouldPlay(req.Policy, req.OwnerId))
                return;

            var entry = catalog.Get(req.SoundKey);
            if (entry == null || entry.Clip == null)
                return;

            var cooldown = req.CooldownHint > 0f ? req.CooldownHint : entry.Cooldown;
            if (_lastPlayTime.TryGetValue(req.SoundKey, out var last)
                && UnityEngine.Time.time - last < cooldown)
                return;

            Play(req.SoundKey, entry, req.Position);
        }

        private bool ShouldPlay(PlaybackPolicy policy, string ownerId)
        {
            switch (policy)
            {
                case PlaybackPolicy.All:
                    return true;
                case PlaybackPolicy.LocalOnly:
                    return string.Equals(ownerId, _localPlayerId, StringComparison.Ordinal);
                case PlaybackPolicy.OwnerExcluded:
                    return !string.Equals(ownerId, _localPlayerId, StringComparison.Ordinal);
                default:
                    return true;
            }
        }

        private void Play(string soundKey, SoundEntry entry, Float3 position)
        {
            var pos = position.ToVector3();
            var go = _pool.Rent(pos, Quaternion.identity);

            var pooled = GetCachedAudioSource(go);
            pooled.AudioSource.clip = entry.Clip;
            pooled.AudioSource.volume = entry.Volume;
            pooled.AudioSource.spatialBlend = entry.SpatialBlend;
            pooled.AudioSource.Play();

            pooled.LifetimeRelease.Arm(entry.Clip.length + 0.1f);

            _lastPlayTime[soundKey] = UnityEngine.Time.time;
        }

        private PooledAudioSource GetCachedAudioSource(GameObject go)
        {
            if (_cache.TryGetValue(go, out var cached))
                return cached;

            var pooled = go.GetComponent<PooledAudioSource>();
            _cache[go] = pooled;
            return pooled;
        }
    }
}
