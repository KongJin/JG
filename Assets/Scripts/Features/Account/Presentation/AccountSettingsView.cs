using System.Runtime.InteropServices;
using Features.Account.Application;
using Features.Account.Domain;
using Shared.Attributes;
using Shared.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Account.Presentation
{
    /// <summary>
    /// 계정 설정 화면. 단일 View 내 섹션 구분.
    /// </summary>
    public sealed class AccountSettingsView : MonoBehaviour
    {
        [Header("Account Info")]
        [SerializeField] private TMP_Text _uidText;
        [SerializeField] private TMP_Text _authTypeText;
        [SerializeField] private TMP_Text _displayNameText;

        [Header("Nickname Change")]
        [SerializeField] private TMP_InputField _nicknameInput;
        [SerializeField] private Button _nicknameApplyButton;
        [SerializeField] private TMP_Text _nicknameMessage;

        [Header("Actions")]
        [SerializeField] private Button _googleSignInButton;
        [SerializeField] private Button _logoutButton;
        [SerializeField] private Button _deleteAccountButton;
        [SerializeField] private TMP_Text _deleteAccountButtonText;
        [SerializeField] private TMP_Text _statusMessageText;

        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject _confirmDialog;
        [SerializeField] private TMP_Text _confirmMessage;
        [SerializeField] private Button _confirmYesButton;
        [SerializeField] private Button _confirmNoButton;

        private AccountSetup _setup;
        private EventBus _eventBus;
        private System.Action _onLogout;
        private System.Action _onDeleteAccount;
        private AccountProfile _currentProfile;
        private bool _buttonsHooked;
        private bool _deleteConfirmationPending;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void AccountGoogleSignIn_RequestIdToken(
            string clientId,
            string callbackObjectName,
            string successMethodName,
            string errorMethodName);
#endif

        public void Initialize(AccountSetup setup, EventBus eventBus, System.Action onLogout, System.Action onDeleteAccount)
        {
            _setup = setup;
            _eventBus = eventBus;
            _onLogout = onLogout;
            _onDeleteAccount = onDeleteAccount;

            HookButtons();
        }

        public void Render(Domain.AccountProfile profile)
        {
            _currentProfile = profile;
            _deleteConfirmationPending = false;

            if (_uidText != null) _uidText.text = $"UID: {profile.uid}";
            if (_authTypeText != null) _authTypeText.text = $"Auth: {profile.authType}";
            if (_displayNameText != null) _displayNameText.text = profile.displayName;
            if (_nicknameInput != null) _nicknameInput.text = profile.displayName;

            RefreshDeleteButtonState();
            RefreshGoogleButtonState();
        }

        private void HookButtons()
        {
            if (_buttonsHooked)
                return;

            _buttonsHooked = true;

            if (_googleSignInButton != null)
                _googleSignInButton.onClick.AddListener(OnGoogleSignInClicked);

            if (_nicknameApplyButton != null)
                _nicknameApplyButton.onClick.AddListener(OnNicknameApplyClicked);

            if (_logoutButton != null)
                _logoutButton.onClick.AddListener(OnLogoutClicked);

            if (_deleteAccountButton != null)
                _deleteAccountButton.onClick.AddListener(OnDeleteAccountClicked);

            if (_confirmYesButton != null)
                _confirmYesButton.onClick.AddListener(OnConfirmYesClicked);

            if (_confirmNoButton != null)
                _confirmNoButton.onClick.AddListener(OnConfirmNoClicked);
        }

        private void OnGoogleSignInClicked()
        {
            if (_setup == null)
            {
                SetStatusMessage("AccountSetup is not connected.");
                return;
            }

            if (_currentProfile != null && _currentProfile.authType == "google")
            {
                SetStatusMessage("Already linked with Google account.");
                RefreshGoogleButtonState();
                return;
            }

            if (string.IsNullOrWhiteSpace(_setup.GoogleWebClientId))
            {
                SetStatusMessage("googleWebClientId is empty.");
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            if (_googleSignInButton != null)
                _googleSignInButton.interactable = false;

            SetStatusMessage("Opening Google sign-in...");
            AccountGoogleSignIn_RequestIdToken(
                _setup.GoogleWebClientId,
                gameObject.name,
                nameof(OnGoogleIdTokenReceived),
                nameof(OnGoogleIdTokenFailed));
#else
            SetStatusMessage("Google sign-in is only available in WebGL builds.");
#endif
        }

        public void OnGoogleIdTokenReceived(string googleIdToken)
        {
            _ = CompleteGoogleSignInAsync(googleIdToken);
        }

        public void OnGoogleIdTokenFailed(string errorMessage)
        {
            SetStatusMessage(string.IsNullOrWhiteSpace(errorMessage)
                ? "Google sign-in failed."
                : $"Google sign-in failed: {errorMessage}");
            RefreshGoogleButtonState();
        }

        private async System.Threading.Tasks.Task CompleteGoogleSignInAsync(string googleIdToken)
        {
            try
            {
                SetStatusMessage("Linking Google account...");

                var result = await _setup.SignInWithGoogle.Execute(googleIdToken);
                if (result.IsFailure)
                {
                    SetStatusMessage(result.Error);
                    return;
                }

                Render(result.Value);
                SetStatusMessage("Google account linked successfully.");
            }
            catch (System.Exception ex)
            {
                SetStatusMessage($"Google sign-in failed: {ex.Message}");
            }
            finally
            {
                RefreshGoogleButtonState();
            }
        }

        private async void OnNicknameApplyClicked()
        {
            if (_setup == null || _nicknameInput == null) return;

            string newName = _nicknameInput.text;
            var result = await _setup.ChangeDisplayName.Execute(newName);

            if (_nicknameMessage != null)
                _nicknameMessage.text = result.IsSuccess ? "Nickname changed." : result.Error;

            if (result.IsSuccess && _displayNameText != null)
                _displayNameText.text = newName;
        }

        private void OnLogoutClicked()
        {
            _onLogout?.Invoke();
        }

        private void OnDeleteAccountClicked()
        {
            if (_confirmDialog != null)
            {
                _confirmDialog.SetActive(true);
                if (_confirmMessage != null)
                    _confirmMessage.text = "Are you sure you want to delete your account?\nAll data will be permanently deleted.";
                return;
            }

            if (!_deleteConfirmationPending)
            {
                _deleteConfirmationPending = true;
                RefreshDeleteButtonState();
                SetStatusMessage("Press delete once more to permanently remove this account.");
                return;
            }

            OnConfirmYesClicked();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void WebglSmokeDeleteAccountClick()
        {
            OnDeleteAccountClicked();
        }

        public void WebglSmokeDeleteAccountConfirm()
        {
            OnConfirmYesClicked();
        }

        public void WebglSmokeDeleteAccountCancel()
        {
            OnConfirmNoClicked();
        }
#endif

        private async void OnConfirmYesClicked()
        {
            if (_confirmDialog != null) _confirmDialog.SetActive(false);
            if (_setup == null) return;

            try
            {
                SetStatusMessage("Deleting account...");
                await _setup.DeleteAccount.Execute();
                _onDeleteAccount?.Invoke();
            }
            catch (System.Exception ex)
            {
                _deleteConfirmationPending = false;
                RefreshDeleteButtonState();
                SetStatusMessage($"Account deletion failed: {ex.Message}");
            }
        }

        private void OnConfirmNoClicked()
        {
            if (_confirmDialog != null) _confirmDialog.SetActive(false);
            _deleteConfirmationPending = false;
            RefreshDeleteButtonState();
        }

        private void RefreshGoogleButtonState()
        {
            if (_googleSignInButton == null)
                return;

            bool isGoogleLinked = _currentProfile != null && _currentProfile.authType == "google";
            _googleSignInButton.gameObject.SetActive(!isGoogleLinked);
            _googleSignInButton.interactable = !isGoogleLinked;
        }

        private void SetStatusMessage(string message)
        {
            var target = _statusMessageText != null ? _statusMessageText : _nicknameMessage;
            if (target != null)
                target.text = message;
        }

        private void RefreshDeleteButtonState()
        {
            if (_deleteAccountButton == null || _deleteAccountButtonText == null)
                return;

            _deleteAccountButtonText.text = _deleteConfirmationPending ? "Confirm Delete" : "Delete Account";
        }

        private void OnDestroy()
        {
            if (_googleSignInButton != null) _googleSignInButton.onClick.RemoveAllListeners();
            if (_nicknameApplyButton != null) _nicknameApplyButton.onClick.RemoveAllListeners();
            if (_logoutButton != null) _logoutButton.onClick.RemoveAllListeners();
            if (_deleteAccountButton != null) _deleteAccountButton.onClick.RemoveAllListeners();
            if (_confirmYesButton != null) _confirmYesButton.onClick.RemoveAllListeners();
            if (_confirmNoButton != null) _confirmNoButton.onClick.RemoveAllListeners();
        }
    }
}
