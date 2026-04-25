using System;
using Features.Garage.Presentation.Theme;
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
        [Required, SerializeField] private TMP_Text _titleText;
        [Required, SerializeField] private TMP_Text _valueText;
        [Required, SerializeField] private TMP_Text _hintText;

        private bool _callbacksHooked;

        public event Action<int> CycleRequested;

        private void Awake()
        {
            ApplyButtonStyles();
        }

        public void Bind()
        {
            if (_callbacksHooked)
                return;

            _callbacksHooked = true;
            _prevButton.onClick.AddListener(() => CycleRequested?.Invoke(-1));
            _nextButton.onClick.AddListener(() => CycleRequested?.Invoke(1));
        }

        public void Render(string valueText, string hintText)
        {
            _valueText.text = valueText;
            bool isEmpty = string.IsNullOrEmpty(valueText) || valueText.StartsWith("< ");
            _valueText.color = isEmpty ? ThemeColors.TextMuted : ThemeColors.TextPrimary;

            _hintText.text = hintText;
            _hintText.color = ThemeColors.TextSecondary;
            _titleText.color = ThemeColors.TextPrimary;
        }

        /// <summary>
        /// < > 버튼에 Secondary 버튼 스타일 적용.
        /// </summary>
        private void ApplyButtonStyles()
        {
            _prevButton.Apply(ButtonStyles.Secondary);
            _nextButton.Apply(ButtonStyles.Secondary);
        }
    }

}
