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
        [Required, SerializeField] private AudioSource bgmAudioSource;
        [Required, SerializeField] private AudioSource[] sfxAudioSources = Array.Empty<AudioSource>();
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
        private bool _loggedSfxPoolExhausted;

        private readonly Dictionary<string, float> _lastPlayTime = new Dictionary<string, float>();
        private readonly Queue<AudioSource> _availableSfxSources = new Queue<AudioSource>();
        private readonly List<AudioSource> _sfxSources = new List<AudioSource>();
        private readonly HashSet<string> _reportedUnavailableSoundKeys = new HashSet<string>();

// csharp-guardrails: allow-null-defense
        public bool HasRuntimeDependencies => catalog != null && bgmAudioSource != null && HasConfiguredSfxSources;
        public int RecentSfxPlaybackKeyCount => _lastPlayTime.Count;

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
// csharp-guardrails: allow-null-defense
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
        }

        public void Initialize(IEventSubscriber eventBus, string localPlayerId)
        {
            if (!HasRuntimeDependencies)
            {
                Debug.LogError("[SoundPlayer] Missing scene-owned audio sources.", this);
                return;
            }

            ResetSceneOwnedSourcesAndClearState();

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
// csharp-guardrails: allow-null-defense
            var entry = catalog != null ? catalog.Get(key) : null;
// csharp-guardrails: allow-null-defense
            if (entry == null || entry.Clip == null || entry.Channel != SoundChannel.Bgm)
                return false;

            if (!EnsureBgmSource())
                return false;

            if (_bgmSource.isPlaying && string.Equals(_currentBgmKey, key, StringComparison.Ordinal))
            {
                RefreshBgmVolume();
                return true;
            }

// csharp-guardrails: allow-null-defense
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
// csharp-guardrails: allow-null-defense
            if (_bgmSource == null)
                return false;

// csharp-guardrails: allow-null-defense
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

        private bool HasConfiguredSfxSources
        {
            get
            {
// csharp-guardrails: allow-null-defense
                if (sfxAudioSources == null)
                    return false;

                for (var i = 0; i < sfxAudioSources.Length; i++)
                {
                    if (sfxAudioSources[i] != null)
                        return true;
                }

                return false;
            }
        }

        private void ResetSceneOwnedSourcesAndClearState()
        {
// csharp-guardrails: allow-null-defense
            _disposables?.Dispose();
            _disposables = null;
            StopAllCoroutines();
            _bgmFadeRoutine = null;

            _availableSfxSources.Clear();
            _sfxSources.Clear();
            _lastPlayTime.Clear();
            _reportedUnavailableSoundKeys.Clear();
            _loggedSfxPoolExhausted = false;
            _currentBgmKey = null;
            _bgmSource = bgmAudioSource;

            PrepareBgmSource();
            PrepareSfxSources();
        }

        private void OnDestroy()
        {
            // csharp-guardrails: allow-null-defense
            _disposables?.Dispose();

            if (_active == this)
                _active = null;
        }

        private void OnSoundRequested(SoundRequestEvent e)
        {
            var req = e.Request;

            if (!ShouldPlay(req.Policy, req.OwnerId))
                return;

// csharp-guardrails: allow-null-defense
            var entry = catalog != null ? catalog.Get(req.SoundKey) : null;
// csharp-guardrails: allow-null-defense
            if (entry == null)
            {
                LogUnavailableSoundKeyOnce(req.SoundKey, "not registered in SoundCatalog");
                return;
            }

// csharp-guardrails: allow-null-defense
            if (entry.Clip == null)
            {
                LogUnavailableSoundKeyOnce(req.SoundKey, "registered without an AudioClip");
                return;
            }

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

        private void LogUnavailableSoundKeyOnce(string soundKey, string reason)
        {
            var safeKey = string.IsNullOrEmpty(soundKey) ? "<empty>" : soundKey;
            var reportKey = $"{safeKey}:{reason}";
            if (!_reportedUnavailableSoundKeys.Add(reportKey))
                return;

            Debug.LogWarning($"[SoundPlayer] Sound '{safeKey}' is unavailable: {reason}.", this);
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
// csharp-guardrails: allow-null-defense
            if (source == null)
                return;

            if (source.transform != transform)
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
            for (var i = 0; i < _sfxSources.Count; i++)
            {
                ReturnSfxSource(_sfxSources[i]);
            }
        }

        private AudioSource RentSfxSource()
        {
            if (_availableSfxSources.Count == 0)
            {
                if (!_loggedSfxPoolExhausted)
                {
                    Debug.LogWarning("[SoundPlayer] No configured SFX AudioSource is available.", this);
                    _loggedSfxPoolExhausted = true;
                }

                return null;
            }

            var source = _availableSfxSources.Dequeue();
            source.enabled = true;
            return source;
        }

        private void PrepareBgmSource()
        {
// csharp-guardrails: allow-null-defense
            if (_bgmSource == null)
                return;

            _bgmSource.Stop();
            _bgmSource.clip = null;
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.spatialBlend = 0f;
            _bgmSource.enabled = true;
        }

        private void PrepareSfxSources()
        {
// csharp-guardrails: allow-null-defense
            if (sfxAudioSources == null)
                return;

            for (var i = 0; i < sfxAudioSources.Length; i++)
            {
                var source = sfxAudioSources[i];
// csharp-guardrails: allow-null-defense
                if (source == null || _sfxSources.Contains(source))
                    continue;

                source.Stop();
                source.clip = null;
                source.playOnAwake = false;
                source.loop = false;
                _sfxSources.Add(source);
            }
        }

        private void ReturnSfxSource(AudioSource source)
        {
            if (source == null)
                return;

            source.Stop();
            source.clip = null;
            source.enabled = false;
            _availableSfxSources.Enqueue(source);
        }

        private IEnumerator ReturnSfxSourceWhenDone(AudioSource source, float seconds)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, seconds));
            ReturnSfxSource(source);
        }

        private bool EnsureBgmSource()
        {
// csharp-guardrails: allow-null-defense
            if (_bgmSource != null)
                return true;

            _bgmSource = bgmAudioSource;
// csharp-guardrails: allow-null-defense
            if (_bgmSource != null)
                return true;

            Debug.LogError("[SoundPlayer] BGM AudioSource is missing.", this);
            return false;
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
// csharp-guardrails: allow-null-defense
            if (_bgmSource == null || string.IsNullOrEmpty(_currentBgmKey))
                return;

// csharp-guardrails: allow-null-defense
            var entry = catalog != null ? catalog.Get(_currentBgmKey) : null;
// csharp-guardrails: allow-null-defense
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
