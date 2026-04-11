using System;
using Shared.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Garage.Presentation
{
    public sealed class GarageUnitEditorView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _selectionTitleText;
        [SerializeField] private TMP_Text _selectionSubtitleText;
        [Required, SerializeField] private GaragePartSelectorView _frameSelectorView;
        [Required, SerializeField] private GaragePartSelectorView _firepowerSelectorView;
        [Required, SerializeField] private GaragePartSelectorView _mobilitySelectorView;
        [Required, SerializeField] private Button _clearButton;

        private bool _callbacksHooked;

        public event Action<int> FrameCycleRequested;
        public event Action<int> FirepowerCycleRequested;
        public event Action<int> MobilityCycleRequested;
        public event Action ClearRequested;

        public void Bind()
        {
            if (_callbacksHooked)
                return;

            _callbacksHooked = true;

            _frameSelectorView.Bind();
            _firepowerSelectorView.Bind();
            _mobilitySelectorView.Bind();

            _frameSelectorView.CycleRequested += delta => FrameCycleRequested?.Invoke(delta);
            _firepowerSelectorView.CycleRequested += delta => FirepowerCycleRequested?.Invoke(delta);
            _mobilitySelectorView.CycleRequested += delta => MobilityCycleRequested?.Invoke(delta);
            _clearButton.onClick.AddListener(() => ClearRequested?.Invoke());
        }

        public void Render(GarageEditorViewModel viewModel)
        {
            if (viewModel == null)
                return;

            if (_selectionTitleText != null)
                _selectionTitleText.text = viewModel.Title;

            if (_selectionSubtitleText != null)
                _selectionSubtitleText.text = viewModel.Subtitle;

            _frameSelectorView.Render(viewModel.FrameValueText, viewModel.FrameHintText);
            _firepowerSelectorView.Render(viewModel.FirepowerValueText, viewModel.FirepowerHintText);
            _mobilitySelectorView.Render(viewModel.MobilityValueText, viewModel.MobilityHintText);
            _clearButton.interactable = viewModel.IsClearInteractable;
        }
    }
}
