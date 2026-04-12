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
        [SerializeField] private TMP_Text _titleText;
        [Required, SerializeField] private TMP_Text _valueText;
        [SerializeField] private TMP_Text _hintText;

        [Header("Layout")]
        [SerializeField] private float _preferredHeight = 144f;
        [SerializeField] private float _titleFontSize = 16f;
        [SerializeField] private float _valueFontSize = 17f;
        [SerializeField] private float _hintFontSize = 11f;

        private bool _callbacksHooked;

        public event Action<int> CycleRequested;

        private void Awake()
        {
            CacheOptionalReferences();
            NormalizeLayout();
        }

        private void OnEnable()
        {
            NormalizeLayout();
        }

        private void OnRectTransformDimensionsChange()
        {
            NormalizeLayout();
        }

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

            float cardHeight = 0f;
            if (transform is RectTransform rootRect)
                cardHeight = Mathf.Max(rootRect.rect.height, 88f);

            ConfigureTitle();
            ConfigureValuePanel(cardHeight);
            ConfigureHint(cardHeight);
        }

        private void CacheOptionalReferences()
        {
            _titleText ??= FindTextChild("Title");
        }

        private void ConfigureTitle()
        {
            if (_titleText == null)
                return;

            ConfigureTopStretchRect(_titleText.rectTransform, 14f, 16f, 22f, 22f);
            _titleText.fontSize = _titleFontSize - 1f;
            _titleText.alignment = TextAlignmentOptions.MidlineLeft;
            _titleText.textWrappingMode = TextWrappingModes.NoWrap;
            _titleText.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void ConfigureValuePanel(float cardHeight)
        {
            var valuePanel = _valueText != null ? _valueText.rectTransform.parent as RectTransform : null;
            if (valuePanel == null)
                return;

            float valueTop = Mathf.Clamp(cardHeight * 0.34f, 30f, 34f);
            float valueHeight = Mathf.Clamp(cardHeight * 0.26f, 24f, 28f);
            float buttonWidth = Mathf.Clamp(valueHeight * 1.8f, 40f, 48f);
            ConfigureTopStretchRect(valuePanel, valueTop, valueHeight, 22f, 22f);

            ConfigureButtonRect(_prevButton, new Vector2(0f, 0f), new Vector2(0f, 1f), 12f, 0f, buttonWidth);
            ConfigureButtonRect(_nextButton, new Vector2(1f, 0f), new Vector2(1f, 1f), -12f, 0f, buttonWidth);

            if (_valueText != null)
            {
                float textInset = buttonWidth + 26f;
                ConfigureStretchRect(_valueText.rectTransform, textInset, textInset, 0f, 0f);
                _valueText.fontSize = _valueFontSize - 1f;
                _valueText.enableAutoSizing = true;
                _valueText.fontSizeMin = _valueFontSize - 5f;
                _valueText.fontSizeMax = _valueFontSize - 1f;
                _valueText.alignment = TextAlignmentOptions.Midline;
                _valueText.textWrappingMode = TextWrappingModes.NoWrap;
                _valueText.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        private void ConfigureHint(float cardHeight)
        {
            if (_hintText != null)
            {
                float hintBottom = Mathf.Clamp(cardHeight * 0.10f, 8f, 10f);
                float hintHeight = Mathf.Clamp(cardHeight * 0.12f, 10f, 12f);
                ConfigureBottomStretchRect(_hintText.rectTransform, hintBottom, hintHeight, 22f, 22f);
                _hintText.fontSize = _hintFontSize;
                _hintText.enableAutoSizing = true;
                _hintText.fontSizeMin = _hintFontSize - 1f;
                _hintText.fontSizeMax = _hintFontSize;
                _hintText.alignment = TextAlignmentOptions.MidlineLeft;
                _hintText.textWrappingMode = TextWrappingModes.NoWrap;
                _hintText.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        private TMP_Text FindTextChild(string nameFragment)
        {
            var texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name.IndexOf(nameFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    return texts[i];
            }

            return null;
        }

        private static void ConfigureButtonRect(
            Button button,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float anchoredX,
            float anchoredY,
            float width)
        {
            if (button == null)
                return;

            if (button.transform is RectTransform rectTransform)
            {
                rectTransform.anchorMin = anchorMin;
                rectTransform.anchorMax = anchorMax;
                rectTransform.pivot = new Vector2(anchorMin.x, 0.5f);
                rectTransform.anchoredPosition = new Vector2(anchoredX, anchoredY);
                rectTransform.sizeDelta = new Vector2(width, 0f);
            }
        }

        private static void ConfigureTopStretchRect(
            RectTransform rectTransform,
            float top,
            float height,
            float left,
            float right)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -top);
            rectTransform.sizeDelta = new Vector2(-(left + right), height);
        }

        private static void ConfigureBottomStretchRect(
            RectTransform rectTransform,
            float bottom,
            float height,
            float left,
            float right)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, bottom);
            rectTransform.sizeDelta = new Vector2(-(left + right), height);
        }

        private static void ConfigureStretchRect(
            RectTransform rectTransform,
            float left,
            float right,
            float top,
            float bottom)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }
    }
}
