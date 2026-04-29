using Shared.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using Shared.EventBus;
using Shared.Lifecycle;
using Shared.Math;
using Shared.Sound;
using UnityEngine;

namespace Shared.Runtime.Sound
{
    public sealed class SoundPlayer : MonoBehaviour
    {
        public const string LobbyOwnerId = "lobby";

        private static SoundPlayer _active;

        [Required, SerializeField] private SoundCatalog catalog;
        [SerializeField] private int initialPoolSize = 8;
        [SerializeField] private float defaultBgmFadeSeconds = 0.35f;

        private IEventSubscriber _eventBus;
        private string _localPlayerId;
        private DisposableScope _disposables;
        private float _masterVolume = 1f;
        private float _bgmVolume = 0.8f;
        private float _sfxVolume = 1f;
        private AudioSource _bgmSource;
        private string _currentBgmKey;
        private Coroutine _bgmFadeRoutine;

        private readonly Dictionary<string, float> _lastPlayTime = new Dictionary<string, float>();
        private readonly Queue<AudioSource> _availableSfxSources = new Queue<AudioSource>();
        private readonly List<AudioSource> _sfxSources = new List<AudioSource>();

        public bool HasRuntimeDependencies => catalog != null;

        public static bool TryGetActive(out SoundPlayer soundPlayer)
        {
            soundPlayer = _active;
            return soundPlayer != null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _active = null;
        }

        private void Awake()
        {
            if (_active != null && _active != this)
            {
                Destroy(gameObject);
                return;
            }

            _active = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ApplyRuntimeConfig(SoundPlayerRuntimeConfig config)
        {
            if (config == null)
            {
                Debug.LogError("[SoundPlayer] Runtime config is missing.", this);
                return;
            }

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

            WarmSfxPool();
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            RefreshBgmVolume();
        }

        public void SetChannelVolumes(float bgmVolume, float sfxVolume)
        {
            _bgmVolume = Mathf.Clamp01(bgmVolume);
            _sfxVolume = Mathf.Clamp01(sfxVolume);
            RefreshBgmVolume();
        }

        public bool PlayBgm(string key, float fadeSeconds)
        {
            var entry = catalog.Get(key);
            if (entry == null || entry.Clip == null || entry.Channel != SoundChannel.Bgm)
                return false;

            if (_bgmSource != null && _bgmSource.isPlaying && string.Equals(_currentBgmKey, key, StringComparison.Ordinal))
            {
                RefreshBgmVolume();
                return true;
            }

            EnsureBgmSource();
            if (_bgmFadeRoutine != null)
                StopCoroutine(_bgmFadeRoutine);

            var safeFadeSeconds = Mathf.Max(0f, fadeSeconds);
            if (safeFadeSeconds <= 0f)
            {
                ApplyBgmEntry(entry, key);
                _bgmSource.volume = GetEffectiveVolume(entry);
                _bgmSource.Play();
                return true;
            }

            _bgmFadeRoutine = StartCoroutine(FadeToBgm(entry, key, safeFadeSeconds));
            return true;
        }

        public bool StopBgm(float fadeSeconds)
        {
            if (_bgmSource == null)
                return false;

            if (_bgmFadeRoutine != null)
                StopCoroutine(_bgmFadeRoutine);

            var safeFadeSeconds = Mathf.Max(0f, fadeSeconds);
            if (safeFadeSeconds <= 0f)
            {
                _bgmSource.Stop();
                _bgmSource.clip = null;
                _currentBgmKey = null;
                return true;
            }

            _bgmFadeRoutine = StartCoroutine(FadeOutBgm(safeFadeSeconds));
            return true;
        }

        public float GetEffectiveVolume(SoundEntry entry)
        {
            if (entry == null)
                return 0f;

            var channelVolume = entry.Channel == SoundChannel.Bgm ? _bgmVolume : _sfxVolume;
            return entry.Volume * _masterVolume * channelVolume;
        }

        private void ReleasePooledChildrenAndClearState()
        {
            _disposables?.Dispose();
            _disposables = null;
            StopAllCoroutines();
            _bgmFadeRoutine = null;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            _availableSfxSources.Clear();
            _sfxSources.Clear();
            _lastPlayTime.Clear();
            _bgmSource = null;
            _currentBgmKey = null;
        }

        private void OnDestroy()
        {
            _disposables?.Dispose();

            if (_active == this)
                _active = null;
        }

        private void OnSoundRequested(SoundRequestEvent e)
        {
            var req = e.Request;

            if (!ShouldPlay(req.Policy, req.OwnerId))
                return;

            var entry = catalog.Get(req.SoundKey);
            if (entry == null || entry.Clip == null)
                return;

            if (entry.Channel == SoundChannel.Bgm)
            {
                PlayBgm(req.SoundKey, defaultBgmFadeSeconds);
                return;
            }

            var cooldown = req.CooldownHint > 0f ? req.CooldownHint : entry.Cooldown;
            if (_lastPlayTime.TryGetValue(req.SoundKey, out var last)
                && UnityEngine.Time.time - last < cooldown)
                return;

            PlaySfx(req.SoundKey, entry, req.Position);
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

        private void PlaySfx(string soundKey, SoundEntry entry, Float3 position)
        {
            var source = RentSfxSource();
            source.transform.position = position.ToVector3();
            source.clip = entry.Clip;
            source.loop = false;
            source.volume = GetEffectiveVolume(entry);
            source.spatialBlend = entry.SpatialBlend;
            source.Play();

            StartCoroutine(ReturnSfxSourceWhenDone(source, entry.Clip.length + 0.1f));

            _lastPlayTime[soundKey] = UnityEngine.Time.time;
        }

        private void WarmSfxPool()
        {
            var count = Mathf.Max(0, initialPoolSize);
            for (var i = 0; i < count; i++)
            {
                var source = CreateSfxSource();
                ReturnSfxSource(source);
            }
        }

        private AudioSource RentSfxSource()
        {
            var source = _availableSfxSources.Count > 0
                ? _availableSfxSources.Dequeue()
                : CreateSfxSource();

            source.gameObject.SetActive(true);
            return source;
        }

        private AudioSource CreateSfxSource()
        {
            var go = new GameObject("SfxAudioSource");
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            _sfxSources.Add(source);
            return source;
        }

        private void ReturnSfxSource(AudioSource source)
        {
            if (source == null)
                return;

            source.Stop();
            source.clip = null;
            source.gameObject.SetActive(false);
            _availableSfxSources.Enqueue(source);
        }

        private IEnumerator ReturnSfxSourceWhenDone(AudioSource source, float seconds)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, seconds));
            ReturnSfxSource(source);
        }

        private void EnsureBgmSource()
        {
            if (_bgmSource != null)
                return;

            var go = new GameObject("BgmAudioSource");
            go.transform.SetParent(transform, false);
            _bgmSource = go.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.spatialBlend = 0f;
        }

        private void ApplyBgmEntry(SoundEntry entry, string key)
        {
            _bgmSource.clip = entry.Clip;
            _bgmSource.loop = entry.Loop;
            _bgmSource.spatialBlend = 0f;
            _currentBgmKey = key;
        }

        private void RefreshBgmVolume()
        {
            if (_bgmSource == null || string.IsNullOrEmpty(_currentBgmKey))
                return;

            var entry = catalog.Get(_currentBgmKey);
            if (entry != null)
                _bgmSource.volume = GetEffectiveVolume(entry);
        }

        private IEnumerator FadeToBgm(SoundEntry entry, string key, float fadeSeconds)
        {
            if (_bgmSource.isPlaying)
                yield return FadeBgmVolume(_bgmSource.volume, 0f, fadeSeconds * 0.5f);

            ApplyBgmEntry(entry, key);
            _bgmSource.volume = 0f;
            _bgmSource.Play();

            yield return FadeBgmVolume(0f, GetEffectiveVolume(entry), fadeSeconds * 0.5f);
            _bgmFadeRoutine = null;
        }

        private IEnumerator FadeOutBgm(float fadeSeconds)
        {
            yield return FadeBgmVolume(_bgmSource.volume, 0f, fadeSeconds);
            _bgmSource.Stop();
            _bgmSource.clip = null;
            _currentBgmKey = null;
            _bgmFadeRoutine = null;
        }

        private IEnumerator FadeBgmVolume(float from, float to, float seconds)
        {
            if (seconds <= 0f)
            {
                _bgmSource.volume = to;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += UnityEngine.Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / seconds);
                _bgmSource.volume = Mathf.Lerp(from, to, t);
                yield return null;
            }

            _bgmSource.volume = to;
        }
    }
}
