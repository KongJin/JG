using System;
using Shared.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Garage.Presentation
{
    public sealed class GaragePartSelectorView : MonoBehaviour
    {
        [Required, SerializeField] private Button _prevButton;
        [Required, SerializeField] private Button _nextButton;
        [Required, SerializeField] private TMP_Text _valueText;
        [SerializeField] private TMP_Text _hintText;

        [Header("Layout")]
        [SerializeField] private float _preferredHeight = 144f;
        [SerializeField] private float _valueFontSize = 17f;
        [SerializeField] private float _hintFontSize = 12f;

        private bool _callbacksHooked;

        public event Action<int> CycleRequested;

        public void Bind()
        {
            NormalizeLayout();

            if (_callbacksHooked)
                return;

            _callbacksHooked = true;
            _prevButton.onClick.AddListener(() => CycleRequested?.Invoke(-1));
            _nextButton.onClick.AddListener(() => CycleRequested?.Invoke(1));
        }

        public void Render(string valueText, string hintText)
        {
            if (_valueText != null)
                _valueText.text = valueText;

            if (_hintText != null)
                _hintText.text = hintText;
        }

        private void NormalizeLayout()
        {
            if (TryGetComponent<LayoutElement>(out var layoutElement))
                layoutElement.preferredHeight = _preferredHeight;

            ConfigureButtonRect(_prevButton, new Vector2(0.03f, 0.18f), new Vector2(0.23f, 0.82f));
            ConfigureButtonRect(_nextButton, new Vector2(0.77f, 0.18f), new Vector2(0.97f, 0.82f));

            if (_valueText != null)
            {
                ConfigureRect(_valueText.rectTransform, new Vector2(0.28f, 0.18f), new Vector2(0.72f, 0.82f));
                _valueText.fontSize = _valueFontSize;
                _valueText.enableAutoSizing = true;
                _valueText.fontSizeMin = _valueFontSize - 3f;
                _valueText.fontSizeMax = _valueFontSize;
                _valueText.enableWordWrapping = false;
                _valueText.overflowMode = TextOverflowModes.Ellipsis;
            }

            if (_hintText != null)
            {
                ConfigureRect(_hintText.rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.24f));
                _hintText.fontSize = _hintFontSize;
                _hintText.enableWordWrapping = false;
                _hintText.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        private static void ConfigureButtonRect(Button button, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (button == null)
                return;

            if (button.transform is RectTransform rectTransform)
                ConfigureRect(rectTransform, anchorMin, anchorMax);
        }

        private static void ConfigureRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }
    }
}
