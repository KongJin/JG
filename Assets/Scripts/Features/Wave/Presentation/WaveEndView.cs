using System;
using Features.Wave.Application.Events;
using Shared.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Wave.Presentation
{
    public sealed class WaveEndView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text resultText;

        public static WaveEndView CreateDefault()
        {
            var canvasGo = new GameObject(
                "WaveEndCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 210;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var viewGo = new GameObject("WaveEndView", typeof(RectTransform), typeof(WaveEndView));
            viewGo.transform.SetParent(canvasGo.transform, false);
            var viewRect = (RectTransform)viewGo.transform;
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = Vector2.zero;
            viewRect.offsetMax = Vector2.zero;

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(viewGo.transform, false);
            var panelRect = (RectTransform)panelGo.transform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = panelGo.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.72f);

            var textGo = new GameObject("ResultText", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panelGo.transform, false);
            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(700f, 120f);
            textRect.anchoredPosition = Vector2.zero;

            var result = textGo.GetComponent<Text>();
            result.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            result.fontSize = 56;
            result.alignment = TextAnchor.MiddleCenter;
            result.color = Color.white;

            var view = viewGo.GetComponent<WaveEndView>();
            view.panel = panelGo;
            view.resultText = result;
            return view;
        }

        public void Initialize(IEventSubscriber subscriber)
        {
            subscriber.Subscribe(this, new Action<WaveVictoryEvent>(OnVictory));
            subscriber.Subscribe(this, new Action<WaveDefeatEvent>(OnDefeat));

            if (panel != null) panel.SetActive(false);
        }

        private void OnVictory(WaveVictoryEvent e)
        {
            Show("Victory!");
        }

        private void OnDefeat(WaveDefeatEvent e)
        {
            Show("Defeat!");
        }

        private void Show(string message)
        {
            if (panel != null) panel.SetActive(true);
            if (resultText != null) resultText.text = message;
        }
    }
}
