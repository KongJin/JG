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
        private int _retryCount;
        private const int MaxRetries = 3;

        private void Awake()
        {
            _retryButton.onClick.AddListener(OnRetryClicked);
        }

        public void Show()
        {
            if (_loadingPanel != null) _loadingPanel.SetActive(true);
            if (_errorPanel != null) _errorPanel.SetActive(false);
            if (_statusText != null) _statusText.text = "로그인 중...";
            _retryCount = 0;
        }

        public void Hide()
        {
            if (_loadingPanel != null) _loadingPanel.SetActive(false);
        }

        public void ShowError(string message)
        {
            if (_loadingPanel != null) _loadingPanel.SetActive(false);
            if (_errorPanel != null) _errorPanel.SetActive(true);
            if (_errorText != null) _errorText.text = message;
        }

        public void SetOnLoginSuccess(System.Action callback)
        {
            _onLoginSuccess = callback;
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
                ShowError("네트워크 연결을 확인해주세요.\n잠시 후 다시 시도해주세요.");
            }
            else
            {
                if (_statusText != null)
                    _statusText.text = $"로그인 실패 ({_retryCount}/{MaxRetries})\n재시도 중...";
            }
        }

        private void OnRetryClicked()
        {
            _retryCount = 0;
            Show();
        }
    }
}
