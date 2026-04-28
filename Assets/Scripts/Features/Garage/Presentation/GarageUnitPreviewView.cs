using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GarageUnitPreviewView : MonoBehaviour
    {
        public GarageSlotViewModel CurrentViewModel { get; private set; }
        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            IsInitialized = true;
        }

        public void Render(GarageSlotViewModel viewModel)
        {
            CurrentViewModel = viewModel;
        }
    }
}
