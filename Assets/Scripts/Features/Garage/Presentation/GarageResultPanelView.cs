using TMPro;
using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GarageResultPanelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _rosterStatusText;
        [SerializeField] private TMP_Text _validationText;
        [SerializeField] private TMP_Text _statsText;

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
