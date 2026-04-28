using System;
using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GarageNovaPartsPanelRowView : MonoBehaviour
    {
        private GarageNovaPartSelection _selection;

        public GarageNovaPartOptionViewModel CurrentOption { get; private set; }

        public event Action<GarageNovaPartSelection> Clicked;

        public void Bind()
        {
        }

        public void Render(GarageNovaPartOptionViewModel option)
        {
            CurrentOption = option;
            if (option != null)
                _selection = new GarageNovaPartSelection(option.Slot, option.Id);
        }

        public void Select()
        {
            Clicked?.Invoke(_selection);
        }
    }
}
