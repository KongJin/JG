using System;
using Features.Garage.Presentation.Theme;
using Shared.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Garage.Presentation
{
    public sealed class GarageNovaPartsPanelView : MonoBehaviour
    {
        [Required, SerializeField] private TMP_Text _titleText;
        [Required, SerializeField] private TMP_Text _countText;
        [Required, SerializeField] private TMP_InputField _searchInput;
        [Required, SerializeField] private Button _frameFilterButton;
        [Required, SerializeField] private TMP_Text _frameFilterLabel;
        [Required, SerializeField] private Button _firepowerFilterButton;
        [Required, SerializeField] private TMP_Text _firepowerFilterLabel;
        [Required, SerializeField] private Button _mobilityFilterButton;
        [Required, SerializeField] private TMP_Text _mobilityFilterLabel;
        [Required, SerializeField] private GarageNovaPartsPanelRowView[] _rowViews;
        [Required, SerializeField] private TMP_Text _selectedNameText;
        [Required, SerializeField] private TMP_Text _selectedDetailText;
        [Required, SerializeField] private Button _applyButton;
        [Required, SerializeField] private TMP_Text _applyButtonLabel;

        private bool _callbacksHooked;

        public event Action<GarageNovaPartPanelSlot> SlotFilterRequested;
        public event Action<string> SearchChanged;
        public event Action<GarageNovaPartSelection> OptionSelected;
        public event Action ApplyRequested;

        public void Bind()
        {
            if (_callbacksHooked)
                return;

            _callbacksHooked = true;
            _searchInput.onValueChanged.AddListener(value => SearchChanged?.Invoke(value));
            _frameFilterButton.onClick.AddListener(() => SlotFilterRequested?.Invoke(GarageNovaPartPanelSlot.Frame));
            _firepowerFilterButton.onClick.AddListener(() => SlotFilterRequested?.Invoke(GarageNovaPartPanelSlot.Firepower));
            _mobilityFilterButton.onClick.AddListener(() => SlotFilterRequested?.Invoke(GarageNovaPartPanelSlot.Mobility));
            _applyButton.onClick.AddListener(() => ApplyRequested?.Invoke());

            if (_rowViews == null)
                return;

            for (int i = 0; i < _rowViews.Length; i++)
            {
                var row = _rowViews[i];
                if (row == null)
                    continue;

                row.Bind();
                row.Clicked += selection => OptionSelected?.Invoke(selection);
            }
        }

        public void Render(GarageNovaPartsPanelViewModel viewModel)
        {
            if (viewModel == null)
                return;

            _titleText.text = "Nova Parts";
            _countText.text = viewModel.CountText;
            _titleText.color = ThemeColors.TextPrimary;
            _countText.color = ThemeColors.TextSecondary;

            if (_searchInput.text != viewModel.SearchText)
                _searchInput.SetTextWithoutNotify(viewModel.SearchText);

            ConfigureFilter(_frameFilterButton, _frameFilterLabel, "Frame", viewModel.ActiveSlot == GarageNovaPartPanelSlot.Frame);
            ConfigureFilter(_firepowerFilterButton, _firepowerFilterLabel, "Fire", viewModel.ActiveSlot == GarageNovaPartPanelSlot.Firepower);
            ConfigureFilter(_mobilityFilterButton, _mobilityFilterLabel, "Mob", viewModel.ActiveSlot == GarageNovaPartPanelSlot.Mobility);

            int rowCount = _rowViews != null ? _rowViews.Length : 0;
            for (int i = 0; i < rowCount; i++)
            {
                var option = viewModel.Options != null && i < viewModel.Options.Count
                    ? viewModel.Options[i]
                    : null;
                _rowViews[i].Render(option);
            }

            _selectedNameText.text = viewModel.SelectedNameText;
            _selectedDetailText.text = viewModel.SelectedDetailText;
            _selectedNameText.color = ThemeColors.TextPrimary;
            _selectedDetailText.color = ThemeColors.TextSecondary;

            _applyButton.interactable = viewModel.CanApply;
            _applyButton.Apply(viewModel.CanApply ? ButtonStyles.Primary : ButtonStyles.Secondary, _applyButtonLabel);
            _applyButtonLabel.text = "Apply";
        }

        private static void ConfigureFilter(Button button, TMP_Text label, string text, bool isActive)
        {
            button.Apply(isActive ? ButtonStyles.Primary : ButtonStyles.Secondary, label);
            label.text = text;
        }
    }
}
