using System;
using Features.Garage.Presentation.Theme;
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
        [Required, SerializeField] private TMP_Text _clearButtonText;

        private bool _callbacksHooked;

        public event Action<int> FrameCycleRequested;
        public event Action<int> FirepowerCycleRequested;
        public event Action<int> MobilityCycleRequested;
        public event Action ClearRequested;

        /// <summary>부품 비교 툴팁용 호버 이벤트 (partType, delta)</summary>
        public event Action<string, int> PartHoverRequested;

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

            // 호버 툴팁 이벤트 전달
            _frameSelectorView.PartHoverRequested += delta => PartHoverRequested?.Invoke("frame", delta);
            _firepowerSelectorView.PartHoverRequested += delta => PartHoverRequested?.Invoke("firepower", delta);
            _mobilitySelectorView.PartHoverRequested += delta => PartHoverRequested?.Invoke("mobility", delta);
        }

        public void Render(GarageEditorViewModel viewModel)
        {
            if (viewModel == null)
                return;

            if (_selectionTitleText != null)
            {
                _selectionTitleText.text = viewModel.Title;
                _selectionTitleText.color = ThemeColors.TextPrimary;
                _selectionTitleText.fontSize = 16;
            }

            if (_selectionSubtitleText != null)
            {
                _selectionSubtitleText.text = viewModel.Subtitle;
                _selectionSubtitleText.color = ThemeColors.TextSecondary;
                _selectionSubtitleText.fontSize = 12;
            }

            _frameSelectorView.Render(viewModel.FrameValueText, viewModel.FrameHintText);
            _firepowerSelectorView.Render(viewModel.FirepowerValueText, viewModel.FirepowerHintText);
            _mobilitySelectorView.Render(viewModel.MobilityValueText, viewModel.MobilityHintText);
            _clearButton.interactable = viewModel.IsClearInteractable;

            // Clear 버튼 — 파괴적 액션 스타일
            _clearButton.Apply(ButtonStyles.Danger, _clearButtonText);

            // Clear 버튼 텍스트 명시화
            if (_clearButtonText != null)
                _clearButtonText.text = "Clear Draft";
        }
    }
}
