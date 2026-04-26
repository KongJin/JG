using System;
using Features.Garage.Presentation.Theme;
using Shared.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Garage.Presentation
{
    public sealed class GarageNovaPartsPanelRowView : MonoBehaviour
    {
        [Required, SerializeField] private Button _button;
        [Required, SerializeField] private Image _background;
        [Required, SerializeField] private TMP_Text _nameText;
        [Required, SerializeField] private TMP_Text _detailText;
        [Required, SerializeField] private TMP_Text _badgeText;

        private GarageNovaPartSelection _selection;
        private bool _callbacksHooked;

        public event Action<GarageNovaPartSelection> Clicked;

        public void Bind()
        {
            if (_callbacksHooked)
                return;

            _callbacksHooked = true;
            _button.onClick.AddListener(() => Clicked?.Invoke(_selection));
        }

        public void Render(GarageNovaPartOptionViewModel option)
        {
            if (option == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            _selection = new GarageNovaPartSelection(option.Slot, option.Id);
            _nameText.text = option.DisplayName;
            _detailText.text = option.DetailText;
            _badgeText.text = option.NeedsNameReview ? "review" : SlotLabel(option.Slot);

            _nameText.color = option.IsSelected ? ThemeColors.TextPrimary : ThemeColors.TextSecondary;
            _detailText.color = ThemeColors.TextMuted;
            _badgeText.color = option.NeedsNameReview ? ThemeColors.AccentAmber : ThemeColors.AccentBlue;
            _background.color = option.IsSelected ? ThemeColors.SlotSelected : ThemeColors.BackgroundCard;
        }

        private static string SlotLabel(GarageNovaPartPanelSlot slot)
        {
            return slot switch
            {
                GarageNovaPartPanelSlot.Frame => "frame",
                GarageNovaPartPanelSlot.Firepower => "fire",
                _ => "mob",
            };
        }
    }
}
