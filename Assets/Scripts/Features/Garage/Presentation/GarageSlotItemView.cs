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
        [SerializeField] private float _summaryFontSize = 12f;

        public Button Button => _button;

        private void Awake()
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

            ConfigureText(_slotNumberText, _slotNumberFontSize);
            ConfigureText(_titleText, _titleFontSize);
            ConfigureText(_summaryText, _summaryFontSize);
        }

        private static void ConfigureText(TMP_Text text, float fontSize)
        {
            if (text == null)
                return;

            text.fontSize = fontSize;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }
    }
}
