using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Garage.Presentation
{
    public sealed class GarageResultPanelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _rosterStatusText;
        [SerializeField] private TMP_Text _validationText;
        [SerializeField] private TMP_Text _statsText;

        [Header("Save")]
        [SerializeField] private Button _saveButton;

        [Header("Toast")]
        [SerializeField] private GameObject _toastPanel;
        [SerializeField] private TMP_Text _toastText;
        [SerializeField] private float _toastDuration = 2f;

        public event System.Action SaveClicked;

        private void OnEnable()
        {
            if (_saveButton != null)
                _saveButton.onClick.AddListener(() => SaveClicked?.Invoke());
        }

        private void OnDisable()
        {
            if (_saveButton != null)
                _saveButton.onClick.RemoveAllListeners();
        }

        public void ShowToast(string message, bool isError = false)
        {
            if (_toastPanel == null || _toastText == null) return;

            _toastText.text = message;
            _toastText.color = isError ? Color.red : Color.green;
            _toastPanel.SetActive(true);
            CancelInvoke(nameof(HideToast));
            Invoke(nameof(HideToast), _toastDuration);
        }

        private void HideToast()
        {
            if (_toastPanel != null)
                _toastPanel.SetActive(false);
        }

        public void Render(GarageResultViewModel viewModel)
        {
            if (viewModel == null)
                return;

            if (_rosterStatusText != null)
                _rosterStatusText.text = viewModel.RosterStatusText;

            if (_validationText != null)
                _validationText.text = viewModel.ValidationText;

            if (_statsText != null)
                _statsText.text = viewModel.StatsText;
        }
    }
}
