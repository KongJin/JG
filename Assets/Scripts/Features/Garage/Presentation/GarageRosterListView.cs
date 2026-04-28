using System;
using System.Collections.Generic;
using Shared.Attributes;
using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GarageRosterListView : MonoBehaviour
    {
        [Required, SerializeField] private GarageSlotItemView[] _slotViews = new GarageSlotItemView[6];

        private bool _callbacksHooked;

        public event Action<int> SlotSelected;

        public void Bind()
        {
            if (_callbacksHooked || _slotViews == null)
                return;

            _callbacksHooked = true;
        }

        public void SelectSlot(int slotIndex)
        {
            SlotSelected?.Invoke(slotIndex);
        }

        public void Render(IReadOnlyList<GarageSlotViewModel> slotViewModels)
        {
            if (_slotViews == null || slotViewModels == null)
                return;

            int renderCount = Mathf.Min(_slotViews.Length, slotViewModels.Count);
            for (int i = 0; i < renderCount; i++)
                _slotViews[i].Render(slotViewModels[i]);
        }
    }
}
