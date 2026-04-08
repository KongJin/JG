using Shared.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Unit.Presentation
{
    /// <summary>
    /// 유닛 슬롯 로테이션 컨트롤 (이전/다음 버튼).
    /// </summary>
    public sealed class UnitRotationControls : MonoBehaviour
    {
        [Required, SerializeField] private Button _previousButton;
        [Required, SerializeField] private Button _nextButton;

        private UnitSlotsContainer _slotsContainer;

        public void Initialize(UnitSlotsContainer slotsContainer)
        {
            _slotsContainer = slotsContainer;

            _previousButton.onClick.AddListener(OnPreviousClicked);
            _nextButton.onClick.AddListener(OnNextClicked);

            UpdateButtonVisibility();
        }

        private void OnDestroy()
        {
            _previousButton.onClick.RemoveListener(OnPreviousClicked);
            _nextButton.onClick.RemoveListener(OnNextClicked);
        }

        private void OnPreviousClicked()
        {
            _slotsContainer.RotatePrevious();
            UpdateButtonVisibility();
        }

        private void OnNextClicked()
        {
            _slotsContainer.RotateNext();
            UpdateButtonVisibility();
        }

        private void UpdateButtonVisibility()
        {
            // 6개 이하여야 로테이션 버튼 표시
            var shouldShow = _slotsContainer.TotalUnits > 3;
            _previousButton.gameObject.SetActive(shouldShow);
            _nextButton.gameObject.SetActive(shouldShow);
        }
    }
}
