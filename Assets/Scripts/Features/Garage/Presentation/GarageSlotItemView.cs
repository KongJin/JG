using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GarageSlotItemView : MonoBehaviour
    {
        public GarageSlotViewModel CurrentViewModel { get; private set; }

        public void Render(GarageSlotViewModel viewModel)
        {
            CurrentViewModel = viewModel;
        }
    }
}
