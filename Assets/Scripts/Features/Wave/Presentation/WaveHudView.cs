using System;
using Features.Wave.Application.Events;
using Shared.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Wave.Presentation
{
    public sealed class WaveHudView : MonoBehaviour
    {
        [SerializeField] private Text waveText;
        [SerializeField] private Text countdownText;
        [SerializeField] private Text statusText;
        [SerializeField] private float statusDisplayDuration = 2f;

        private float _statusTimer;

        public static WaveHudView CreateDefault()
        {
            var canvasGo = new GameObject(
                "WaveHudCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var viewGo = new GameObject("WaveHud", typeof(RectTransform), typeof(WaveHudView));
            viewGo.transform.SetParent(canvasGo.transform, false);
            var rect = (RectTransform)viewGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var view = viewGo.GetComponent<WaveHudView>();
            view.waveText = CreateText(viewGo.transform, "WaveText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), 36, TextAnchor.UpperCenter);
            view.countdownText = CreateText(viewGo.transform, "CountdownText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), 42, TextAnchor.UpperCenter);
            view.statusText = CreateText(viewGo.transform, "StatusText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -148f), 30, TextAnchor.UpperCenter);

            return view;
        }

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

        public void UpdateCountdown(float remaining)
        {
            if (countdownText != null && countdownText.gameObject.activeSelf)
                countdownText.text = $"{Mathf.CeilToInt(remaining)}";
        }

        private static Text CreateText(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            int fontSize,
            TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(800f, 80f);
            rect.anchoredPosition = anchoredPosition;

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            return text;
        }
    }
}
