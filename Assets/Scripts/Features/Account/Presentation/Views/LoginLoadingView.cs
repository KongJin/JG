using Features.Account.Domain;
using Shared.EventBus;
using UnityEngine;

namespace Features.Account.Presentation
{
    /// <summary>
    /// 로그인 로딩 화면. LobbySetup 내부 오버레이로 동작.
    /// </summary>
    public sealed class LoginLoadingView : MonoBehaviour
    {
        private System.Action _onLoginSuccess;
        private System.Action _onRetryRequested;
        private int _retryCount;
        private string _statusMessage = string.Empty;
        private string _errorMessage = string.Empty;
        private const int MaxRetries = 3;

        public bool IsShowing { get; private set; }
        public string StatusMessage => _statusMessage;
        public string ErrorMessage => _errorMessage;

        public void Show()
        {
            IsShowing = true;
            _errorMessage = string.Empty;
            _statusMessage = "Signing in...";
            _retryCount = 0;
        }

        public void Hide()
        {
            IsShowing = false;
            _statusMessage = string.Empty;
            _errorMessage = string.Empty;
        }

        public void ShowError(string message)
        {
            IsShowing = true;
            _statusMessage = string.Empty;
            _errorMessage = message;
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
                _statusMessage = $"Sign-in failed ({_retryCount}/{MaxRetries})\nRetrying...";
            }
        }

        public void Retry()
        {
            _retryCount = 0;
            Show();
            _onRetryRequested?.Invoke();
        }
    }
}
