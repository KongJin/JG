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
        [Required, SerializeField] private GameObject _arrowIndicator;
        [Required, SerializeField] private Image _borderImage;

        [Header("Layout")]
        [SerializeField] private float _slotNumberFontSize = 8f;
        [SerializeField] private float _titleFontSize = 12f;
        [SerializeField] private float _summaryFontSize = 8f;

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

            _slotNumberText.text = $"{viewModel.SlotLabel}  {viewModel.StatusBadgeText}";
            _slotNumberText.color = GetStatusTextColor(viewModel);
            _titleText.text = viewModel.Title;
            _titleText.color = viewModel.IsSelected ? ThemeColors.TextPrimary : new Color(0.95f, 0.96f, 0.98f, 0.92f);
            _summaryText.text = viewModel.Summary;
            _summaryText.color = viewModel.IsSelected ? ThemeColors.TextPrimary : ThemeColors.TextSecondary;

            // 배경색 — 페이드 애니메이션 적용
            Color targetColor = GetSlotColor(viewModel);
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

            _arrowIndicator.SetActive(viewModel.ShowArrow);
            _borderImage.gameObject.SetActive(viewModel.IsSelected);

            if (viewModel.IsSelected)
            {
                _borderImage.color = viewModel.HasDraftChanges ? ThemeColors.AccentOrange : ThemeColors.StateSelected;
                _canvasGroup.alpha = 1f;
            }
            else
            {
                if (!viewModel.HasCommittedLoadout)
                {
                    Color c = _isHovered ? ThemeColors.SlotEmptyHover : ThemeColors.SlotEmpty;
                    _background.color = new Color(c.r, c.g, c.b, 0.78f);
                }

                _borderImage.gameObject.SetActive(false);
                _canvasGroup.alpha = viewModel.HasCommittedLoadout ? 0.94f : 0.90f;
            }
        }

        // IPointerEnterHandler / IPointerExitHandler — 빈 슬롯 호버 피드백
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            if (_currentViewModel != null && !_currentViewModel.HasCommittedLoadout && _background != null)
            {
                _background.color = new Color(ThemeColors.SlotEmptyHover.r, ThemeColors.SlotEmptyHover.g, ThemeColors.SlotEmptyHover.b, 0.88f);
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
            if (viewModel.HasDraftChanges) return ThemeColors.SlotDirty;
            if (viewModel.HasCommittedLoadout) return ThemeColors.SlotFilled;
            return ThemeColors.SlotEmpty;
        }

        private static Color GetStatusTextColor(GarageSlotViewModel viewModel)
        {
            if (viewModel.IsSelected)
                return ThemeColors.TextPrimary;

            if (viewModel.HasDraftChanges)
                return ThemeColors.AccentAmber;

            if (viewModel.HasCommittedLoadout)
                return ThemeColors.AccentGreen;

            return ThemeColors.TextMuted;
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
            ConfigureText(_slotNumberText, _slotNumberFontSize, false, TextAlignmentOptions.TopLeft);
            ConfigureText(_titleText, _titleFontSize, false, TextAlignmentOptions.TopLeft);
            ConfigureText(_summaryText, _summaryFontSize, false, TextAlignmentOptions.BottomLeft);
        }

        private static void ConfigureText(TMP_Text text, float fontSize, bool enableAutoSizing, TextAlignmentOptions alignment)
        {
            text.fontSize = fontSize;
            text.enableAutoSizing = enableAutoSizing;
            text.fontSizeMin = Mathf.Max(7f, fontSize - 2f);
            text.fontSizeMax = fontSize;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.PreserveWhitespaceNoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }
    }
}
