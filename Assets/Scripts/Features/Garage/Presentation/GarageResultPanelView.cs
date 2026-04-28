using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GarageResultPanelView : MonoBehaviour
    {
        public GarageResultViewModel CurrentViewModel { get; private set; }
        public bool IsInlineSaveVisible { get; private set; }
        public bool IsLoading { get; private set; }
        public string LastToastMessage { get; private set; } = string.Empty;
        public bool LastToastWasError { get; private set; }

        public event System.Action SaveClicked;

        public void SetInlineSaveVisible(bool isVisible)
        {
            IsInlineSaveVisible = isVisible;
        }

        public void ShowToast(string message, bool isError = false)
        {
            LastToastMessage = message ?? string.Empty;
            LastToastWasError = isError;
        }

        public void ShowLoading(bool isLoading)
        {
            IsLoading = isLoading;
        }

        public void Render(GarageResultViewModel viewModel)
        {
            CurrentViewModel = viewModel;
        }

        public void RequestSave()
        {
            SaveClicked?.Invoke();
        }
    }
}
