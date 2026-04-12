using Shared.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Garage.Presentation
{
    public sealed class GarageSlotItemView : MonoBehaviour
    {
        [Required, SerializeField] private Button _button;
        [Required, SerializeField] private Image _background;
        [Required, SerializeField] private TMP_Text _slotNumberText;
        [Required, SerializeField] private TMP_Text _titleText;
        [Required, SerializeField] private TMP_Text _summaryText;

        [Header("Colors")]
        [SerializeField] private Color _selectedColor = new(0.24f, 0.47f, 0.89f, 1f);
        [SerializeField] private Color _filledColor = new(0.17f, 0.21f, 0.32f, 1f);
        [SerializeField] private Color _emptyColor = new(0.10f, 0.12f, 0.18f, 0.92f);

        [Header("Layout")]
        [SerializeField] private float _preferredHeight = 92f;
        [SerializeField] private float _slotNumberFontSize = 13f;
        [SerializeField] private float _titleFontSize = 18f;
        [SerializeField] private float _summaryFontSize = 11f;

        public Button Button => _button;

        private void Awake()
        {
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

        public void Render(GarageSlotViewModel viewModel)
        {
            if (viewModel == null)
                return;

            if (_slotNumberText != null)
                _slotNumberText.text = viewModel.SlotLabel;

            if (_titleText != null)
                _titleText.text = viewModel.Title;

            if (_summaryText != null)
                _summaryText.text = viewModel.Summary;

            if (_background != null)
            {
                _background.color = viewModel.IsSelected
                    ? _selectedColor
                    : (viewModel.HasCommittedLoadout ? _filledColor : _emptyColor);
            }
        }

        private void NormalizeLayout()
        {
            if (TryGetComponent<LayoutElement>(out var layoutElement))
                layoutElement.preferredHeight = _preferredHeight;

            float slotHeight = 0f;
            if (transform is RectTransform rootRect)
                slotHeight = Mathf.Max(rootRect.rect.height, 60f);

            float topInset = Mathf.Clamp(slotHeight * 0.12f, 7f, 9f);
            float slotNumberHeight = Mathf.Clamp(slotHeight * 0.14f, 9f, 11f);
            float titleTop = topInset + slotNumberHeight + 4f;
            float titleHeight = Mathf.Clamp(slotHeight * 0.26f, 15f, 18f);
            float summaryBottom = Mathf.Clamp(slotHeight * 0.10f, 6f, 8f);
            float summaryHeight = Mathf.Clamp(slotHeight * 0.16f, 9f, 11f);

            ConfigureTopStretchRect(_slotNumberText?.rectTransform, topInset, slotNumberHeight, 18f, 18f);
            ConfigureTopStretchRect(_titleText?.rectTransform, titleTop, titleHeight, 18f, 18f);
            ConfigureBottomStretchRect(_summaryText?.rectTransform, summaryBottom, summaryHeight, 18f, 18f);

            ConfigureText(_slotNumberText, Mathf.Min(_slotNumberFontSize, slotNumberHeight + 1f), false);
            ConfigureText(_titleText, Mathf.Min(_titleFontSize, titleHeight + 1f), true);
            ConfigureText(_summaryText, Mathf.Min(_summaryFontSize, summaryHeight + 0.5f), true);
        }

        private static void ConfigureText(TMP_Text text, float fontSize, bool enableAutoSizing)
        {
            if (text == null)
                return;

            text.fontSize = fontSize;
            text.enableAutoSizing = enableAutoSizing;
            text.fontSizeMin = Mathf.Max(8f, fontSize - 2f);
            text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
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
    }
}
