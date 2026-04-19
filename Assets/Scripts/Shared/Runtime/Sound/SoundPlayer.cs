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
        public const string LobbyOwnerId = "lobby";

        [Required, SerializeField] private GameObject audioSourcePrefab;
        [Required, SerializeField] private SoundCatalog catalog;
        [SerializeField] private int initialPoolSize = 8;

        private IEventSubscriber _eventBus;
        private string _localPlayerId;
        private GameObjectPool _pool;
        private DisposableScope _disposables;
        private float _masterVolume = 1f;

        private readonly Dictionary<string, float> _lastPlayTime = new Dictionary<string, float>();
        private readonly Dictionary<GameObject, PooledAudioSource> _cache =
            new Dictionary<GameObject, PooledAudioSource>();

        public bool HasRuntimeDependencies => audioSourcePrefab != null && catalog != null;

        private void Awake()
        {
            var players = UnityEngine.Object.FindObjectsByType<SoundPlayer>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                var existing = players[i];
                if (existing == null || existing == this)
                    continue;

                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }

        public void ApplyRuntimeConfig(SoundPlayerRuntimeConfig config)
        {
            if (config == null)
            {
                Debug.LogError("[SoundPlayer] Runtime config is missing.", this);
                return;
            }

            audioSourcePrefab = config.AudioSourcePrefab;
            catalog = config.Catalog;
            initialPoolSize = config.InitialPoolSize;
        }

        public void Initialize(IEventSubscriber eventBus, string localPlayerId)
        {
            if (!HasRuntimeDependencies)
            {
                Debug.LogError("[SoundPlayer] Audio dependencies are missing. Assign serialized references or apply a runtime config before Initialize.", this);
                return;
            }

            ReleasePooledChildrenAndClearState();

            _eventBus = eventBus;
            _localPlayerId = localPlayerId;

            _disposables = new DisposableScope();
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, this));
            _eventBus.Subscribe(this, new Action<SoundRequestEvent>(OnSoundRequested));

            _pool = new GameObjectPool(audioSourcePrefab, transform, initialPoolSize);
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
        }

        private void ReleasePooledChildrenAndClearState()
        {
            _disposables?.Dispose();
            _disposables = null;
            _masterVolume = 1f;

            var pooledObjects = GetComponentsInChildren<PooledObject>(true);
            for (var i = pooledObjects.Length - 1; i >= 0; i--)
            {
                var po = pooledObjects[i];
                if (po == null)
                    continue;
                if (po.gameObject == gameObject)
                    continue;
                Destroy(po.gameObject);
            }

            _cache.Clear();
            _lastPlayTime.Clear();
            _pool = null;
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
            pooled.AudioSource.volume = entry.Volume * _masterVolume;
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
