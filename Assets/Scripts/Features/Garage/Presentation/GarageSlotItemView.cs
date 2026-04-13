using Shared.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Garage.Presentation
{
    public sealed class GarageSlotItemView : MonoBehaviour
    {
        [Required, SerializeField] private Button _button;
        [Required, SerializeField] private Image _background;
        [Required, SerializeField] private TMP_Text _slotNumberText;
        [Required, SerializeField] private TMP_Text _titleText;
        [Required, SerializeField] private TMP_Text _summaryText;

        [Header("Colors")]
        [SerializeField] private Color _selectedColor = new(0.24f, 0.47f, 0.89f, 1f);
        [SerializeField] private Color _filledColor = new(0.17f, 0.21f, 0.32f, 1f);
        [SerializeField] private Color _emptyColor = new(0.10f, 0.12f, 0.18f, 0.92f);

        [Header("Selection Feedback")]
        [SerializeField] private GameObject _arrowIndicator;
        [SerializeField] private Image _borderImage;
        [SerializeField] private Color _glowColor = new(0.24f, 0.47f, 0.89f, 0.8f);

        [Header("Layout")]
        [SerializeField] private float _preferredHeight = 92f;
        [SerializeField] private float _slotNumberFontSize = 15f; // 13f → 15f (가독성 개선)
        [SerializeField] private float _titleFontSize = 18f;
        [SerializeField] private float _summaryFontSize = 11f;

        public Button Button => _button;

        // OnRectTransformDimensionsChange 과다 호출 방지 — 크기 변경 시에만 레이아웃 갱신
        private Vector2 _lastSizeDelta;

        // 선택 애니메이션 — 부드럽게 전환
        private CanvasGroup _canvasGroup;
        private bool _isTransitioning;

        private void Awake()
        {
            NormalizeLayout();
            if (transform is RectTransform rt)
                _lastSizeDelta = rt.sizeDelta;

            // CanvasGroup 추가 (페이드 애니메이션용)
            if (!TryGetComponent(out _canvasGroup))
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                _canvasGroup.alpha = 1f;
            }
        }

        private void OnEnable()
        {
            NormalizeLayout();
            if (transform is RectTransform rt)
                _lastSizeDelta = rt.sizeDelta;
        }

        private void OnRectTransformDimensionsChange()
        {
            // 실제 크기 변경 시에만 NormalizeLayout 호출 — 매 프레임 중복 호출 방지
            if (transform is RectTransform rt && rt.sizeDelta != _lastSizeDelta)
            {
                _lastSizeDelta = rt.sizeDelta;
                NormalizeLayout();
            }
        }

        public void Render(GarageSlotViewModel viewModel)
        {
            if (viewModel == null)
                return;

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
                    // inactive면 애니메이션 건너뛰고 즉시 적용 (다음 active 시 올바른 색상)
                    _background.color = targetColor;
                }
                else if (_isTransitioning)
                {
                    // 이미 애니메이션 중이면 즉시 중단 후 재시작
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
                    _borderImage.color = _glowColor;

                // 선택된 슬롯 강조 — 약간의 스케일 업 + 불투명도 증가
                if (_canvasGroup != null)
                    _canvasGroup.alpha = 1f;
            }
            else
            {
                // 비선택 슬롯 — 채워진 슬롯은 유지, 빈 슬롯은 반투명
                if (_background != null && !viewModel.HasCommittedLoadout)
                {
                    // 빈 슬롯은 약간 더 어둡게 (클릭 가능 표시 유지)
                    Color c = _emptyColor;
                    _background.color = new Color(c.r, c.g, c.b, 0.6f);
                }
                if (_borderImage != null)
                    _borderImage.gameObject.SetActive(false);

                if (_canvasGroup != null)
                    _canvasGroup.alpha = 0.85f;
            }
        }

        private Color GetSlotColor(GarageSlotViewModel viewModel)
        {
            if (viewModel.IsSelected) return _selectedColor;
            if (viewModel.HasCommittedLoadout) return _filledColor;
            return _emptyColor;
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

        private void NormalizeLayout()
        {
            if (TryGetComponent<LayoutElement>(out var layoutElement))
                layoutElement.preferredHeight = _preferredHeight;

            float slotHeight = 0f;
            if (transform is RectTransform rootRect)
                slotHeight = Mathf.Max(rootRect.rect.height, 60f);

            // 비율 기반 레이아웃 — 하드코딩 마진 제거
            float topInset = Mathf.Clamp(slotHeight * 0.12f, 7f, 9f);
            float slotNumberHeight = Mathf.Clamp(slotHeight * 0.14f, 9f, 11f);
            float margin = Mathf.Clamp(slotHeight * 0.05f, 3f, 6f); // 비율 기반 마진
            float titleTop = topInset + slotNumberHeight + margin;
            float titleHeight = Mathf.Clamp(slotHeight * 0.26f, 15f, 18f);
            float summaryBottom = Mathf.Clamp(slotHeight * 0.10f, 6f, 8f);
            float summaryHeight = Mathf.Clamp(slotHeight * 0.16f, 9f, 11f);

            ConfigureTopStretchRect(_slotNumberText?.rectTransform, topInset, slotNumberHeight, 18f, 18f);
            ConfigureTopStretchRect(_titleText?.rectTransform, titleTop, titleHeight, 18f, 18f);
            ConfigureBottomStretchRect(_summaryText?.rectTransform, summaryBottom, summaryHeight, 18f, 18f);

            ConfigureText(_slotNumberText, Mathf.Min(_slotNumberFontSize, slotNumberHeight + 1f), false);
            ConfigureText(_titleText, Mathf.Min(_titleFontSize, titleHeight + 1f), true);
            ConfigureText(_summaryText, Mathf.Min(_summaryFontSize, summaryHeight + 0.5f), true);
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

        private static void ConfigureTopStretchRect(
            RectTransform rectTransform,
            float top,
            float height,
            float left,
            float right)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -top);
            rectTransform.sizeDelta = new Vector2(-(left + right), height);
        }

        private static void ConfigureBottomStretchRect(
            RectTransform rectTransform,
            float bottom,
            float height,
            float left,
            float right)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, bottom);
            rectTransform.sizeDelta = new Vector2(-(left + right), height);
        }
    }
}
