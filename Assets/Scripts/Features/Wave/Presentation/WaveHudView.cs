using Shared.Attributes;
using System;
using Features.Wave.Application.Events;
using Features.Wave.Domain;
using Shared.EventBus;
using Shared.Runtime;
using Shared.Runtime.Pooling;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Wave.Presentation
{
    public sealed class WaveHudView : MonoBehaviour
    {
        [Required, SerializeField] private Text waveText;
        [Required, SerializeField] private Text countdownText;
        [Required, SerializeField] private Text statusText;
        [SerializeField] private Image backgroundImage;

        [Tooltip("선택. 웨이브1 카운트다운 시 덱 순환 안내 한 줄 (MVP ①·리텐션). 비우면 표시 안 함.")]
        [SerializeField]
        private Text firstWaveDeckHintText;

        [SerializeField] private float statusDisplayDuration = 2f;

        private float _statusTimer;
        private float _countdownRemaining;
        private bool _isCountingDown;

        private void Awake()
        {
            ApplyPresentationDefaults();
        }

        public void Initialize(IEventSubscriber subscriber)
        {
            ApplyPresentationDefaults();
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
                firstWaveDeckHintText.text = "슬롯 선택 후 전장을 탭해 배치하세요.";
        }

        private void ApplyPresentationDefaults()
        {
            if (backgroundImage == null)
            {
                backgroundImage = ComponentAccess.Get<Image>(gameObject);
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(0.05f, 0.09f, 0.14f, 0.9f);
                backgroundImage.raycastTarget = false;
            }

            ConfigureText(waveText, new Vector2(0.05f, 0.46f), new Vector2(0.60f, 0.96f), 30, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.95f, 0.97f, 1f, 1f));
            ConfigureText(countdownText, new Vector2(0.73f, 0.24f), new Vector2(0.94f, 0.9f), 28, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.5f, 0.85f, 1f, 1f));
            ConfigureText(statusText, new Vector2(0.05f, 0.08f), new Vector2(0.68f, 0.44f), 18, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.73f, 0.83f, 0.94f, 1f));
            ConfigureText(firstWaveDeckHintText, new Vector2(0.05f, -0.28f), new Vector2(0.95f, 0.04f), 16, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.77f, 0.88f, 1f, 0.95f));
        }

        private static void ConfigureText(Text text, Vector2 anchorMin, Vector2 anchorMax, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            if (text == null)
                return;

            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
        }

    }
}
