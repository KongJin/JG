using System;
using Shared.Attributes;
using UnityEngine;

namespace Features.Garage.Presentation
{
    public enum GarageEditorFocus
    {
        Frame,
        Firepower,
        Mobility,
    }

    public sealed class GarageUnitEditorView : MonoBehaviour
    {
        [Required, SerializeField] private GaragePartSelectorView _frameSelectorView;
        [Required, SerializeField] private GaragePartSelectorView _firepowerSelectorView;
        [Required, SerializeField] private GaragePartSelectorView _mobilitySelectorView;

        private bool _callbacksHooked;

        public GarageEditorViewModel CurrentViewModel { get; private set; }
        public GarageEditorFocus FocusedPart { get; private set; } = GarageEditorFocus.Frame;

        public event Action<int> FrameCycleRequested;
        public event Action<int> FirepowerCycleRequested;
        public event Action<int> MobilityCycleRequested;
        public event Action ClearRequested;

        public void Bind()
        {
            if (_callbacksHooked)
                return;

            _callbacksHooked = true;

            _frameSelectorView?.Bind();
            _firepowerSelectorView?.Bind();
            _mobilitySelectorView?.Bind();

            if (_frameSelectorView != null)
                _frameSelectorView.CycleRequested += delta => FrameCycleRequested?.Invoke(delta);
            if (_firepowerSelectorView != null)
                _firepowerSelectorView.CycleRequested += delta => FirepowerCycleRequested?.Invoke(delta);
            if (_mobilitySelectorView != null)
                _mobilitySelectorView.CycleRequested += delta => MobilityCycleRequested?.Invoke(delta);
        }

        public void Render(GarageEditorViewModel viewModel)
        {
            CurrentViewModel = viewModel;
            if (viewModel == null)
                return;

            _frameSelectorView?.Render(viewModel.FrameValueText, viewModel.FrameHintText);
            _firepowerSelectorView?.Render(viewModel.FirepowerValueText, viewModel.FirepowerHintText);
            _mobilitySelectorView?.Render(viewModel.MobilityValueText, viewModel.MobilityHintText);
        }

        public void SetFocusedPart(GarageEditorFocus focusedPart)
        {
            FocusedPart = focusedPart;
        }

        public void Clear()
        {
            ClearRequested?.Invoke();
        }
    }
}
