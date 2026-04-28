using System;
using Shared.Attributes;
using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GarageNovaPartsPanelView : MonoBehaviour
    {
        [Required, SerializeField] private GarageNovaPartsPanelRowView[] _rowViews;

        private bool _callbacksHooked;

        public GarageNovaPartsPanelViewModel CurrentViewModel { get; private set; }

        public event Action<GarageNovaPartPanelSlot> SlotFilterRequested;
        public event Action<string> SearchChanged;
        public event Action<GarageNovaPartSelection> OptionSelected;
        public event Action ApplyRequested;

        public void Bind()
        {
            if (_callbacksHooked)
                return;

            _callbacksHooked = true;
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
            CurrentViewModel = viewModel;
            if (viewModel == null || _rowViews == null)
                return;

            for (int i = 0; i < _rowViews.Length; i++)
            {
                var option = viewModel.Options != null && i < viewModel.Options.Count
                    ? viewModel.Options[i]
                    : null;
                _rowViews[i]?.Render(option);
            }
        }

        public void RequestSlotFilter(GarageNovaPartPanelSlot slot)
        {
            SlotFilterRequested?.Invoke(slot);
        }

        public void SetSearchText(string value)
        {
            SearchChanged?.Invoke(value ?? string.Empty);
        }

        public void RequestApply()
        {
            ApplyRequested?.Invoke();
        }
    }
}
