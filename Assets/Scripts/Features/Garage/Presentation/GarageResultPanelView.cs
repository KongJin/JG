using System.Collections.Generic;
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

        // Toast 큐 시스템 — 연속 호출 시 마지막 메시지 소실 방지
        private readonly Queue<string> _toastQueue = new();
        private bool _isShowingToast;
        private CanvasGroup _toastCanvasGroup;

        // Loading
        [Header("Loading")]
        [SerializeField] private GameObject _loadingIndicator;

        public event System.Action SaveClicked;

        private void Awake()
        {
            // Toast 패널에 CanvasGroup 추가 (페이드 애니메이션용)
            if (_toastPanel != null && !_toastPanel.TryGetComponent(out _toastCanvasGroup))
            {
                _toastCanvasGroup = _toastPanel.AddComponent<CanvasGroup>();
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

            // ToastPanel이 3D 미리보기보다 먼저 렌더링되도록 hierarchy 순서 조정
            // Unity UI는 hierarchy에서 앞쪽일수록 뒤에 렌더링됨
            if (_toastPanel != null && _toastPanel.transform.parent != null)
            {
                _toastPanel.transform.SetAsFirstSibling();
            }

            // 로딩 인디케이터 초기 상태 확인
            if (_loadingIndicator != null)
                _loadingIndicator.SetActive(false);
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
            if (_toastPanel == null || _toastText == null) return;

            _toastQueue.Enqueue(message);

            if (!_isShowingToast)
            {
                // 큐에서 바로 처리 (isError 정보는 첫 메시지에 적용)
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
            var message = _toastQueue.Dequeue();

            _toastText.text = message;

            // 배경과 텍스트 색상 동시 변경 — 가시성 향상
            UpdateToastColors(message.Contains("Error") || message.Contains("failed") || message.Contains("오류"));

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
                panelImage.color = isError
                    ? new Color(0.35f, 0.08f, 0.08f, 0.95f)
                    : new Color(0.08f, 0.30f, 0.12f, 0.95f);
            }

            // 텍스트색: 순색 대신 부드러운 톤
            _toastText.color = isError
                ? new Color(1f, 0.7f, 0.7f, 1f)
                : new Color(0.7f, 1f, 0.7f, 1f);
        }

        private void HideToast()
        {
            if (_toastPanel == null) return;

            // 페이드 아웃
            if (_toastCanvasGroup != null)
            {
                StartCoroutine(FadeTo(0f, 0.15f, () =>
                {
                    _toastPanel.SetActive(false);
                    // 다음 큐 처리
                    ShowNextToastFromQueue();
                }));
            }
            else
            {
                _toastPanel.SetActive(false);
                ShowNextToastFromQueue();
            }
        }

        private System.Collections.IEnumerator FadeTo(float targetAlpha, float duration, System.Action onComplete = null)
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
            if (_loadingIndicator != null)
                _loadingIndicator.SetActive(isLoading);

            if (_saveButton != null)
                _saveButton.interactable = !isLoading;
        }

        public void Render(GarageResultViewModel viewModel)
        {
            if (viewModel == null)
                return;

            if (_rosterStatusText != null)
                _rosterStatusText.text = viewModel.RosterStatusText;

            if (_validationText != null)
            {
                _validationText.text = viewModel.ValidationText;
                // Validation 텍스트도 에러 시 색상 변경
                bool hasError = !string.IsNullOrEmpty(viewModel.ValidationText)
                    && (viewModel.ValidationText.Contains("Error") || viewModel.ValidationText.Contains("실패"));
                _validationText.color = hasError
                    ? new Color(1f, 0.5f, 0.5f, 1f)
                    : Color.white;
            }

            if (_statsText != null)
                _statsText.text = viewModel.StatsText;

            // Save 버튼 가시성 개선 — 준비 완료 시 강조
            if (_saveButton != null && _saveButton.TryGetComponent<UnityEngine.UI.Image>(out var saveBtnImage))
            {
                bool isReady = viewModel.RosterStatusText != null
                    && viewModel.RosterStatusText.Contains("Ready");
                saveBtnImage.color = isReady
                    ? new Color(0.2f, 0.6f, 0.3f, 1f)    // 준비 완료 시 초록 강조
                    : new Color(0.2f, 0.4f, 0.9f, 1f);   // 기본 블루
            }
        }
    }
}
