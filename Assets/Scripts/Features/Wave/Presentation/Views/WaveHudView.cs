using System;
using Features.Wave.Application.Events;
using Features.Wave.Domain;
using Shared.EventBus;
using Shared.Localization;
using UnityEngine;

namespace Features.Wave.Presentation
{
    public sealed class WaveHudView : MonoBehaviour
    {
        [SerializeField] private float statusDisplayDuration = 2f;

        private float _statusTimer;
        private float _countdownRemaining;
        private bool _isCountingDown;

        public string WaveText { get; private set; } = string.Empty;
        public string CountdownText { get; private set; } = string.Empty;
        public string StatusText { get; private set; } = string.Empty;
        public string FirstWaveDeckHintText { get; private set; } = string.Empty;
        public bool IsCountdownVisible { get; private set; }
        public bool IsStatusVisible { get; private set; }
        public bool IsFirstWaveDeckHintVisible { get; private set; }

        public void Initialize(IEventSubscriber subscriber)
        {
            subscriber.Subscribe(this, new Action<WaveCountdownStartedEvent>(OnCountdownStarted));
            subscriber.Subscribe(this, new Action<WaveStartedEvent>(OnWaveStarted));
            subscriber.Subscribe(this, new Action<WaveClearedEvent>(OnWaveCleared));
            subscriber.Subscribe(this, new Action<WaveHydratedEvent>(OnWaveHydrated));
            IsCountdownVisible = false;
            IsStatusVisible = false;
            SetFirstWaveDeckHintVisible(false);
        }

        private void Update()
        {
            if (_isCountingDown)
            {
                _countdownRemaining = Mathf.Max(0f, _countdownRemaining - Time.deltaTime);
                CountdownText = $"{Mathf.CeilToInt(_countdownRemaining)}";
            }

            if (_statusTimer > 0f)
            {
                _statusTimer -= Time.deltaTime;
                if (_statusTimer <= 0f)
                    IsStatusVisible = false;
            }
        }

        private void OnCountdownStarted(WaveCountdownStartedEvent e)
        {
            WaveText = $"{GameText.Get("battle.wave")} {e.WaveIndex + 1}/{e.TotalWaves}";
            _countdownRemaining = e.Duration;
            _isCountingDown = true;
            IsCountdownVisible = true;
            CountdownText = $"{Mathf.CeilToInt(e.Duration)}";
            IsStatusVisible = false;
            SetFirstWaveDeckHintVisible(e.WaveIndex == 0);
        }

        private void OnWaveStarted(WaveStartedEvent e)
        {
            _isCountingDown = false;
            WaveText = $"{GameText.Get("battle.wave")} {e.WaveIndex + 1}/{e.TotalWaves}";
            IsCountdownVisible = false;
            SetFirstWaveDeckHintVisible(false);
        }

        private void OnWaveCleared(WaveClearedEvent e)
        {
            ShowStatus("웨이브 완료!");
        }

        private void OnWaveHydrated(WaveHydratedEvent e)
        {
            WaveText = $"{GameText.Get("battle.wave")} {e.WaveIndex + 1}/{e.TotalWaves}";
            if (e.State == WaveState.Countdown && e.CountdownRemaining > 0f)
            {
                _countdownRemaining = e.CountdownRemaining;
                _isCountingDown = true;
                IsCountdownVisible = true;
                CountdownText = $"{Mathf.CeilToInt(e.CountdownRemaining)}";
                SetFirstWaveDeckHintVisible(e.WaveIndex == 0);
                return;
            }

            _isCountingDown = false;
            IsCountdownVisible = false;
            SetFirstWaveDeckHintVisible(false);
        }

        private void ShowStatus(string message)
        {
            StatusText = message;
            IsStatusVisible = true;
            _statusTimer = statusDisplayDuration;
        }

        private void SetFirstWaveDeckHintVisible(bool visible)
        {
            IsFirstWaveDeckHintVisible = visible;
            FirstWaveDeckHintText = visible ? GameText.Get("battle.tap_field_to_place") : string.Empty;
        }
    }
}
