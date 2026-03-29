using Shared.Attributes;
using System;
using Features.Wave.Application.Events;
using Shared.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Wave.Presentation
{
    public sealed class WaveHudView : MonoBehaviour
    {
        [Required, SerializeField] private Text waveText;
        [Required, SerializeField] private Text countdownText;
        [Required, SerializeField] private Text statusText;
        [SerializeField] private float statusDisplayDuration = 2f;

        private float _statusTimer;
        private float _countdownRemaining;
        private bool _isCountingDown;

        public void Initialize(IEventSubscriber subscriber)
        {
            subscriber.Subscribe(this, new Action<WaveCountdownStartedEvent>(OnCountdownStarted));
            subscriber.Subscribe(this, new Action<WaveStartedEvent>(OnWaveStarted));
            subscriber.Subscribe(this, new Action<WaveClearedEvent>(OnWaveCleared));

            if (countdownText != null) countdownText.gameObject.SetActive(false);
            if (statusText != null) statusText.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_isCountingDown)
            {
                _countdownRemaining -= Time.deltaTime;
                if (_countdownRemaining < 0f) _countdownRemaining = 0f;

                if (countdownText != null)
                    countdownText.text = $"{Mathf.CeilToInt(_countdownRemaining)}";
            }

            if (_statusTimer > 0f)
            {
                _statusTimer -= Time.deltaTime;
                if (_statusTimer <= 0f && statusText != null)
                    statusText.gameObject.SetActive(false);
            }
        }

        private void OnCountdownStarted(WaveCountdownStartedEvent e)
        {
            if (waveText != null)
                waveText.text = $"Wave {e.WaveIndex + 1}/{e.TotalWaves}";

            _countdownRemaining = e.Duration;
            _isCountingDown = true;

            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(true);
                countdownText.text = $"{Mathf.CeilToInt(e.Duration)}";
            }

            if (statusText != null)
                statusText.gameObject.SetActive(false);
        }

        private void OnWaveStarted(WaveStartedEvent e)
        {
            _isCountingDown = false;

            if (waveText != null)
                waveText.text = $"Wave {e.WaveIndex + 1}/{e.TotalWaves}";

            if (countdownText != null)
                countdownText.gameObject.SetActive(false);
        }

        private void OnWaveCleared(WaveClearedEvent e)
        {
            ShowStatus("Wave Cleared!");
        }

        private void ShowStatus(string message)
        {
            if (statusText == null) return;
            statusText.text = message;
            statusText.gameObject.SetActive(true);
            _statusTimer = statusDisplayDuration;
        }

    }
}
