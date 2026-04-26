using Features.Account.Domain;
using Shared.Attributes;
using Shared.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Account.Presentation
{
    /// <summary>
    /// 로그인 로딩 화면. LobbySetup 내부 오버레이로 동작.
    /// </summary>
    public sealed class LoginLoadingView : MonoBehaviour
    {
        [Header("References")]
        [Required, SerializeField] private GameObject _loadingPanel;
        [Required, SerializeField] private TMP_Text _statusText;
        [Required, SerializeField] private GameObject _errorPanel;
        [Required, SerializeField] private TMP_Text _errorText;
        [Required, SerializeField] private Button _retryButton;

        private System.Action _onLoginSuccess;
        private System.Action _onRetryRequested;
        private int _retryCount;
        private const int MaxRetries = 3;

        private void Awake()
        {
            if (_retryButton != null)
                _retryButton.onClick.AddListener(OnRetryClicked);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (_loadingPanel != null)
                _loadingPanel.SetActive(true);
            if (_errorPanel != null)
                _errorPanel.SetActive(false);
            if (_statusText != null)
                _statusText.text = "Signing in...";
            _retryCount = 0;
        }

        public void Hide()
        {
            if (_loadingPanel != null)
                _loadingPanel.SetActive(false);
            if (_errorPanel != null)
                _errorPanel.SetActive(false);
            gameObject.SetActive(false);
        }

        public void ShowError(string message)
        {
            gameObject.SetActive(true);
            if (_loadingPanel != null)
                _loadingPanel.SetActive(false);
            if (_errorPanel != null)
                _errorPanel.SetActive(true);
            if (_errorText != null)
                _errorText.text = message;
        }

        public void SetOnLoginSuccess(System.Action callback)
        {
            _onLoginSuccess = callback;
        }

        public void SetOnRetryRequested(System.Action callback)
        {
            _onRetryRequested = callback;
        }

        public void OnLoginSuccess()
        {
            Hide();
            _onLoginSuccess?.Invoke();
        }

        public void OnLoginFailed(string error)
        {
            _retryCount++;
            if (_retryCount >= MaxRetries)
            {
                ShowError("Please check your network connection.\nTry again in a moment.");
            }
            else
            {
                if (_statusText != null)
                    _statusText.text = $"Sign-in failed ({_retryCount}/{MaxRetries})\nRetrying...";
            }
        }

        private void OnRetryClicked()
        {
            _retryCount = 0;
            Show();
            _onRetryRequested?.Invoke();
        }

        private void OnDestroy()
        {
            if (_retryButton != null)
                _retryButton.onClick.RemoveListener(OnRetryClicked);
        }
    }
}
