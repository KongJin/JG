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

        private bool _callbacksHooked;

        public event Action<int> CycleRequested;

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
            if (_valueText != null)
                _valueText.text = valueText;

            if (_hintText != null)
                _hintText.text = hintText;
        }
    }
}
