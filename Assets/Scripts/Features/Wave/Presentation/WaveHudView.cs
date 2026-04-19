using Shared.Attributes;
using System;
using Features.Wave.Application.Events;
using Features.Wave.Domain;
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

        [Tooltip("선택. 웨이브1 카운트다운 시 덱 순환 안내 한 줄 (MVP ①·리텐션). 비우면 표시 안 함.")]
        [SerializeField]
        private Text firstWaveDeckHintText;

        [SerializeField] private float statusDisplayDuration = 2f;

        private float _statusTimer;
        private float _countdownRemaining;
        private bool _isCountingDown;

        public void Initialize(IEventSubscriber subscriber)
        {
            subscriber.Subscribe(this, new Action<WaveCountdownStartedEvent>(OnCountdownStarted));
            subscriber.Subscribe(this, new Action<WaveStartedEvent>(OnWaveStarted));
            subscriber.Subscribe(this, new Action<WaveClearedEvent>(OnWaveCleared));
            subscriber.Subscribe(this, new Action<WaveHydratedEvent>(OnWaveHydrated));

            if (countdownText != null) countdownText.gameObject.SetActive(false);
            if (statusText != null) statusText.gameObject.SetActive(false);
            SetFirstWaveDeckHintVisible(false);
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

            SetFirstWaveDeckHintVisible(e.WaveIndex == 0);
        }

        private void OnWaveStarted(WaveStartedEvent e)
        {
            _isCountingDown = false;

            if (waveText != null)
                waveText.text = $"Wave {e.WaveIndex + 1}/{e.TotalWaves}";

            if (countdownText != null)
                countdownText.gameObject.SetActive(false);

            SetFirstWaveDeckHintVisible(false);
        }

        private void OnWaveCleared(WaveClearedEvent e)
        {
            ShowStatus("Wave Cleared!");
        }

        private void OnWaveHydrated(WaveHydratedEvent e)
        {
            if (waveText != null)
                waveText.text = $"Wave {e.WaveIndex + 1}/{e.TotalWaves}";

            if (e.State == WaveState.Countdown && e.CountdownRemaining > 0f)
            {
                _countdownRemaining = e.CountdownRemaining;
                _isCountingDown = true;
                if (countdownText != null)
                {
                    countdownText.gameObject.SetActive(true);
                    countdownText.text = $"{Mathf.CeilToInt(e.CountdownRemaining)}";
                }

                SetFirstWaveDeckHintVisible(e.WaveIndex == 0);
            }
            else
            {
                _isCountingDown = false;
                if (countdownText != null)
                    countdownText.gameObject.SetActive(false);
                SetFirstWaveDeckHintVisible(false);
            }
        }

        private void ShowStatus(string message)
        {
            if (statusText == null) return;
            statusText.text = message;
            statusText.gameObject.SetActive(true);
            _statusTimer = statusDisplayDuration;
        }

        private void SetFirstWaveDeckHintVisible(bool visible)
        {
            if (firstWaveDeckHintText == null)
                return;

            if (!visible)
            {
                firstWaveDeckHintText.gameObject.SetActive(false);
                return;
            }

            firstWaveDeckHintText.gameObject.SetActive(true);
            if (string.IsNullOrEmpty(firstWaveDeckHintText.text))
                firstWaveDeckHintText.text = "슬롯을 눌러 즉시 소환하고, 드래그로 정확히 배치하세요.";
        }

    }
}
