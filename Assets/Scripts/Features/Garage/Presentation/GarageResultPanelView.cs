using System.Collections.Generic;
using Features.Garage.Presentation.Theme;
using Shared.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Garage.Presentation
{
    public sealed class GarageResultPanelView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _rosterStatusText;

        [SerializeField]
        private TMP_Text _validationText;

        [SerializeField]
        private TMP_Text _statsText;

        [Header("Save")]
        [Required, SerializeField]
        private Button _saveButton;

        [Required, SerializeField]
        private TMP_Text _saveButtonText;

        [Required, SerializeField]
        private Image _saveButtonImage;

        [Header("Toast")]
        [Required, SerializeField]
        private GameObject _toastPanel;

        [Required, SerializeField]
        private CanvasGroup _toastCanvasGroup;

        [Required, SerializeField]
        private TMP_Text _toastText;

        [SerializeField]
        private float _toastDuration = 2f;

        // Toast 큐 시스템 — 메시지와 상태를 함께 보존해 연속 호출 시에도 스타일이 유지되게 한다.
        private readonly Queue<ToastRequest> _toastQueue = new();
        private bool _isShowingToast;
        private bool _isLoading;
        private bool _isReadyToSave;

        // Loading
        [Header("Loading")]
        [SerializeField]
        private GameObject _loadingIndicator;

        private readonly struct ToastRequest
        {
            public ToastRequest(string message, bool isError)
            {
                Message = message;
                IsError = isError;
            }

            public string Message { get; }
            public bool IsError { get; }
        }

        public event System.Action SaveClicked;

        private void Awake()
        {
            // Toast CanvasGroup 초기화
            if (_toastCanvasGroup != null)
            {
                _toastCanvasGroup.alpha = 0f;
                _toastCanvasGroup.blocksRaycasts = false;
                _toastCanvasGroup.interactable = false;
            }

            // ToastPanel Image 배경을 완전히 투명하게 — 하얀 사각형 방지
            if (_toastPanel != null && _toastPanel.TryGetComponent<Image>(out var toastImage))
            {
                toastImage.color = Color.clear;
            }

            // 초기 상태: 토스트 숨김
            if (_toastPanel != null)
                _toastPanel.SetActive(false);

            // 로딩 인디케이터 초기 상태 확인
            if (_loadingIndicator != null)
                _loadingIndicator.SetActive(false);

            // 저장 버튼 초기화 — ThemeColors 기반 Primary 스타일
            InitializeSaveButton();
        }

        private void InitializeSaveButton()
        {
            if (_saveButton == null)
                return;

            // ButtonStyles 적용
            _saveButton.Apply(ButtonStyles.Primary, _saveButtonText);

            if (_saveButtonText != null)
                _saveButtonText.text = "Save Roster";
        }

        private void OnEnable()
        {
            if (_saveButton != null)
                _saveButton.onClick.AddListener(() => SaveClicked?.Invoke());
        }

        private void OnDisable()
        {
            if (_saveButton != null)
                _saveButton.onClick.RemoveAllListeners();

            // 생명주기 안전성: 비활성화 시 토스트 정리
            CancelInvoke(nameof(HideToast));
            _toastQueue.Clear();
            _isShowingToast = false;
            if (_toastPanel != null)
                _toastPanel.SetActive(false);
        }

        public void ShowToast(string message, bool isError = false)
        {
            if (_toastPanel == null || _toastText == null)
                return;

            _toastQueue.Enqueue(new ToastRequest(message, isError));

            if (!_isShowingToast)
            {
                ShowNextToastFromQueue();
            }
        }

        private void ShowNextToastFromQueue()
        {
            if (_toastQueue.Count == 0)
            {
                _isShowingToast = false;
                return;
            }

            _isShowingToast = true;
            var request = _toastQueue.Dequeue();

            _toastText.text = request.Message;
            UpdateToastColors(request.IsError);

            _toastPanel.SetActive(true);

            // 페이드 인
            if (_toastCanvasGroup != null)
            {
                _toastCanvasGroup.alpha = 0f;
                StartCoroutine(FadeTo(1f, 0.15f));
            }

            CancelInvoke(nameof(HideToast));
            Invoke(nameof(HideToast), _toastDuration);
        }

        private void UpdateToastColors(bool isError)
        {
            // 배경색: 어두운 톤으로 가시성 확보
            if (_toastPanel.TryGetComponent<Image>(out var panelImage))
            {
                panelImage.color = isError ? ThemeColors.ToastErrorBg : ThemeColors.ToastSuccessBg;
            }

            // 텍스트색: 순색 대신 부드러운 톤
            _toastText.color = isError ? ThemeColors.ToastErrorText : ThemeColors.ToastSuccessText;
        }

        private void HideToast()
        {
            if (_toastPanel == null)
                return;

            // 페이드 아웃
            if (_toastCanvasGroup != null)
            {
                StartCoroutine(
                    FadeTo(
                        0f,
                        0.15f,
                        () =>
                        {
                            _toastPanel.SetActive(false);
                            // 다음 큐 처리
                            ShowNextToastFromQueue();
                        }
                    )
                );
            }
            else
            {
                _toastPanel.SetActive(false);
                ShowNextToastFromQueue();
            }
        }

        private System.Collections.IEnumerator FadeTo(
            float targetAlpha,
            float duration,
            System.Action onComplete = null
        )
        {
            if (_toastCanvasGroup == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float startAlpha = _toastCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                _toastCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _toastCanvasGroup.alpha = targetAlpha;
            onComplete?.Invoke();
        }

        // 로딩 표시 — 저장/처리 중 중복 클릭 방지
        public void ShowLoading(bool isLoading)
        {
            _isLoading = isLoading;

            if (_loadingIndicator != null)
                _loadingIndicator.SetActive(isLoading);

            RefreshSaveButtonState();
        }

        public void Render(GarageResultViewModel viewModel)
        {
            if (viewModel == null)
                return;

            _isReadyToSave = viewModel.IsReady;

            if (_rosterStatusText != null)
            {
                _rosterStatusText.text = viewModel.RosterStatusText;
                _rosterStatusText.color = ThemeColors.TextPrimary;
                _rosterStatusText.fontSize = 15;
                _rosterStatusText.enableAutoSizing = false;
            }

            if (_validationText != null)
            {
                _validationText.text = viewModel.ValidationText;
                bool hasError = !string.IsNullOrWhiteSpace(viewModel.ValidationText) &&
                                (viewModel.ValidationText.Contains("Unknown", System.StringComparison.OrdinalIgnoreCase) ||
                                 viewModel.ValidationText.Contains("failed", System.StringComparison.OrdinalIgnoreCase) ||
                                 viewModel.ValidationText.Contains("실패", System.StringComparison.OrdinalIgnoreCase));
                bool hasWarning = viewModel.IsDirty && !viewModel.CanSave;
                _validationText.color = hasError
                    ? ThemeColors.AccentRed
                    : hasWarning
                        ? ThemeColors.AccentAmber
                        : ThemeColors.TextSecondary;
                _validationText.fontSize = 13;
                _validationText.enableAutoSizing = false;
            }

            if (_statsText != null)
            {
                _statsText.text = viewModel.StatsText;
                _statsText.color = ThemeColors.TextSecondary;
                _statsText.fontSize = 13;
                _statsText.enableAutoSizing = false;
            }

            RefreshSaveButtonState();

            if (_saveButtonText != null)
                _saveButtonText.text = _isLoading ? "Saving..." : viewModel.PrimaryActionLabel;

            if (_saveButton != null)
                _saveButton.interactable = !_isLoading && viewModel.CanSave;

            if (_saveButtonImage != null)
            {
                var targetColor = viewModel.CanSave
                    ? ThemeColors.AccentGreen
                    : viewModel.IsDirty
                        ? ThemeColors.AccentOrange
                        : ThemeColors.StateDisabled;
                _saveButtonImage.color = targetColor;

                var feedback = _saveButton.GetComponent<ButtonFeedback>();
                if (feedback != null)
                    feedback.UpdateBaseColor(targetColor);
            }
        }

        private void RefreshSaveButtonState()
        {
            if (_saveButtonText != null)
                _saveButtonText.color = ThemeColors.TextPrimary;
        }
    }
}
