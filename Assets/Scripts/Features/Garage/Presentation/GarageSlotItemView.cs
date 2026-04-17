using Features.Garage.Presentation.Theme;
using Shared.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Features.Garage.Presentation
{
    public sealed class GarageSlotItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Required, SerializeField] private Button _button;
        [Required, SerializeField] private Image _background;
        [Required, SerializeField] private TMP_Text _slotNumberText;
        [Required, SerializeField] private TMP_Text _titleText;
        [Required, SerializeField] private TMP_Text _summaryText;

        [Header("Selection Feedback")]
        [SerializeField] private GameObject _arrowIndicator;
        [SerializeField] private Image _borderImage;

        [Header("Layout")]
        [SerializeField] private float _slotNumberFontSize = 15f; // 13f → 15f (가독성 개선)
        [SerializeField] private float _titleFontSize = 18f;
        [SerializeField] private float _summaryFontSize = 11f;

        [Header("Animation")]
        [Required, SerializeField] private CanvasGroup _canvasGroup;

        public Button Button => _button;

        // 선택 애니메이션 — 부드럽게 전환
        private bool _isTransitioning;

        // 호버 상태 — 빈 슬롯 클릭 가능 시각 피드백
        private bool _isHovered;
        private GarageSlotViewModel _currentViewModel;

        private void Awake()
        {
            ApplyTypography();

            // CanvasGroup 초기화 (inspector에서 연결됨)
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;
        }

        private void OnEnable()
        {
            ApplyTypography();
        }

        public void Render(GarageSlotViewModel viewModel)
        {
            if (viewModel == null)
                return;

            _currentViewModel = viewModel;

            if (_slotNumberText != null)
                _slotNumberText.text = viewModel.SlotLabel;

            if (_titleText != null)
                _titleText.text = viewModel.Title;

            if (_summaryText != null)
                _summaryText.text = viewModel.Summary;

            // 배경색 — 페이드 애니메이션 적용
            Color targetColor = GetSlotColor(viewModel);
            if (_background != null)
            {
                if (!gameObject.activeInHierarchy)
                {
                    _background.color = targetColor;
                }
                else if (_isTransitioning)
                {
                    StopAllCoroutines();
                    _isTransitioning = false;
                    StartCoroutine(FadeBackgroundColor(targetColor, 0.15f));
                }
                else
                {
                    StartCoroutine(FadeBackgroundColor(targetColor, 0.15f));
                }
            }

            // 선택 상태 시각 피드백
            if (_arrowIndicator != null)
                _arrowIndicator.SetActive(viewModel.ShowArrow);

            if (_borderImage != null)
                _borderImage.gameObject.SetActive(viewModel.IsSelected);

            if (viewModel.IsSelected)
            {
                if (_borderImage != null)
                    _borderImage.color = ThemeColors.StateSelected;

                if (_canvasGroup != null)
                    _canvasGroup.alpha = 1f;
            }
            else
            {
                // 빈 슬롯은 약간 더 어둡게 (클릭 가능 표시 유지)
                if (_background != null && !viewModel.HasCommittedLoadout)
                {
                    Color c = _isHovered ? ThemeColors.SlotEmptyHover : ThemeColors.SlotEmpty;
                    _background.color = new Color(c.r, c.g, c.b, 0.6f);
                }
                if (_borderImage != null)
                    _borderImage.gameObject.SetActive(false);

                if (_canvasGroup != null)
                    _canvasGroup.alpha = 0.85f;
            }
        }

        // IPointerEnterHandler / IPointerExitHandler — 빈 슬롯 호버 피드백
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            if (_currentViewModel != null && !_currentViewModel.HasCommittedLoadout && _background != null)
            {
                _background.color = new Color(ThemeColors.SlotEmptyHover.r, ThemeColors.SlotEmptyHover.g, ThemeColors.SlotEmptyHover.b, 0.7f);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            if (_currentViewModel != null && _background != null)
            {
                Render(_currentViewModel);
            }
        }

        private Color GetSlotColor(GarageSlotViewModel viewModel)
        {
            if (viewModel.IsSelected) return ThemeColors.SlotSelected;
            if (viewModel.HasCommittedLoadout) return ThemeColors.SlotFilled;
            return ThemeColors.SlotEmpty;
        }

        private System.Collections.IEnumerator FadeBackgroundColor(Color target, float duration)
        {
            _isTransitioning = true;
            Color start = _background.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                _background.color = Color.Lerp(start, target, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _background.color = target;
            _isTransitioning = false;
        }

        private void ApplyTypography()
        {
            ConfigureText(_slotNumberText, _slotNumberFontSize, false);
            ConfigureText(_titleText, _titleFontSize, true);
            ConfigureText(_summaryText, _summaryFontSize, true);
        }

        private static void ConfigureText(TMP_Text text, float fontSize, bool enableAutoSizing)
        {
            if (text == null)
                return;

            text.fontSize = fontSize;
            text.enableAutoSizing = enableAutoSizing;
            text.fontSizeMin = Mathf.Max(8f, fontSize - 2f);
            text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

    }
}
