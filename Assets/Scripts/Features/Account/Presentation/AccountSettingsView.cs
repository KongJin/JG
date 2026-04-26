using Features.Account.Domain;
using Shared.Attributes;
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
        [Required, SerializeField] private TMP_Text _authTypeText;
        [Required, SerializeField] private TMP_Text _displayNameText;

        [Header("Nickname Change")]
        [SerializeField] private TMP_InputField _nicknameInput;
        [SerializeField] private Button _nicknameApplyButton;
        [SerializeField] private TMP_Text _nicknameMessage;

        [Header("Actions")]
        [Required, SerializeField] private Button _googleSignInButton;
        [Required, SerializeField] private Button _logoutButton;
        [Required, SerializeField] private Button _deleteAccountButton;
        [Required, SerializeField] private TMP_Text _deleteAccountButtonText;
        [Required, SerializeField] private TMP_Text _statusMessageText;

        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject _confirmDialog;
        [SerializeField] private TMP_Text _confirmMessage;
        [SerializeField] private Button _confirmYesButton;
        [SerializeField] private Button _confirmNoButton;

        private AccountSettingsInputHandler _inputHandler;
        private AccountProfile _currentProfile;
        private bool _buttonsHooked;
        private bool _deleteConfirmationPending;

        public void Initialize(AccountSettingsInputHandler inputHandler)
        {
            _inputHandler = inputHandler;

            HookButtons();
        }

        public void Render(Domain.AccountProfile profile)
        {
            _currentProfile = profile;
            _deleteConfirmationPending = false;

            _authTypeText.text = BuildAuthStatusText(profile);
            _displayNameText.text = BuildDisplayNameText(profile);

            if (HasNicknameSection)
                _nicknameInput.text = profile.displayName;

            SetStatusMessage(BuildDefaultStatusMessage(profile));
            RefreshDeleteButtonState();
            RefreshGoogleButtonState();
        }

        private void HookButtons()
        {
            if (_buttonsHooked)
                return;

            _buttonsHooked = true;

            _googleSignInButton.onClick.AddListener(OnGoogleSignInClicked);
            _logoutButton.onClick.AddListener(OnLogoutClicked);
            _deleteAccountButton.onClick.AddListener(OnDeleteAccountClicked);

            if (HasNicknameSection)
                _nicknameApplyButton.onClick.AddListener(OnNicknameApplyClicked);

            if (HasConfirmDialog)
            {
                _confirmYesButton.onClick.AddListener(OnConfirmYesClicked);
                _confirmNoButton.onClick.AddListener(OnConfirmNoClicked);
            }
        }

        private void OnGoogleSignInClicked()
        {
            EnsureInitialized();

            if (_currentProfile != null && _currentProfile.authType == "google")
            {
                SetStatusMessage("Already linked with Google account.");
                RefreshGoogleButtonState();
                return;
            }

            if (!_inputHandler.CanRequestGoogleSignIn(out string unavailableMessage))
            {
                SetStatusMessage(unavailableMessage);
                return;
            }

            _googleSignInButton.interactable = false;
            SetStatusMessage("Opening Google sign-in...");
            _inputHandler.RequestGoogleIdToken(
                gameObject.name,
                nameof(OnGoogleIdTokenReceived),
                nameof(OnGoogleIdTokenFailed));
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

                var result = await _inputHandler.CompleteGoogleSignInAsync(googleIdToken);
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

        private void OnNicknameApplyClicked()
        {
            _ = RunNicknameApplyAsync();
        }

        private async System.Threading.Tasks.Task RunNicknameApplyAsync()
        {
            EnsureInitialized();

            if (!HasNicknameSection)
                return;

            string newName = _nicknameInput.text;
            var result = await _inputHandler.ChangeDisplayNameAsync(newName);

            SetNicknameMessage(result.IsSuccess ? "Nickname changed." : result.Error);

            if (result.IsSuccess)
                _displayNameText.text = newName;
        }

        private void OnLogoutClicked()
        {
            _inputHandler?.Logout();
        }

        private void OnDeleteAccountClicked()
        {
            if (HasConfirmDialog)
            {
                _confirmDialog.SetActive(true);
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

        private void OnConfirmYesClicked()
        {
            _ = RunConfirmYesAsync();
        }

        private async System.Threading.Tasks.Task RunConfirmYesAsync()
        {
            if (HasConfirmDialog)
                _confirmDialog.SetActive(false);

            EnsureInitialized();

            try
            {
                SetStatusMessage("Deleting account...");
                await _inputHandler.DeleteAccountAsync();
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
            if (HasConfirmDialog)
                _confirmDialog.SetActive(false);
            _deleteConfirmationPending = false;
            RefreshDeleteButtonState();
        }

        private void RefreshGoogleButtonState()
        {
            bool isGoogleLinked = _currentProfile != null && _currentProfile.authType == "google";
            _googleSignInButton.gameObject.SetActive(!isGoogleLinked);
            _googleSignInButton.interactable = !isGoogleLinked;
        }

        private void SetStatusMessage(string message)
        {
            _statusMessageText.text = message;
            SetNicknameMessage(message);
        }

        private void RefreshDeleteButtonState()
        {
            _deleteAccountButtonText.text = _deleteConfirmationPending ? "Confirm Delete" : "Delete Account";
        }

        private static string BuildDisplayNameText(AccountProfile profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.displayName))
                return "Pilot profile";

            return profile.displayName;
        }

        private static string BuildAuthStatusText(AccountProfile profile)
        {
            if (profile == null)
                return "Anonymous account";

            return profile.authType == "google"
                ? "Google linked"
                : "Anonymous account";
        }

        private static string BuildDefaultStatusMessage(AccountProfile profile)
        {
            if (profile == null)
                return string.Empty;

            return profile.authType == "google"
                ? "Linked and ready. Sign out keeps the linked account available."
                : "Link Google to keep this roster across devices.";
        }

        private bool HasNicknameSection =>
            _nicknameInput != null &&
            _nicknameApplyButton != null &&
            _nicknameMessage != null;

        private bool HasConfirmDialog =>
            _confirmDialog != null &&
            _confirmMessage != null &&
            _confirmYesButton != null &&
            _confirmNoButton != null;

        private void EnsureInitialized()
        {
            if (_inputHandler == null)
                throw new System.InvalidOperationException("AccountSettingsView.Initialize must be called before interaction.");
        }

        private void SetNicknameMessage(string message)
        {
            if (!HasNicknameSection)
                return;

            _nicknameMessage.text = message;
        }

        private void OnDestroy()
        {
            _googleSignInButton.onClick.RemoveAllListeners();
            _logoutButton.onClick.RemoveAllListeners();
            _deleteAccountButton.onClick.RemoveAllListeners();

            if (HasNicknameSection)
                _nicknameApplyButton.onClick.RemoveAllListeners();

            if (HasConfirmDialog)
            {
                _confirmYesButton.onClick.RemoveAllListeners();
                _confirmNoButton.onClick.RemoveAllListeners();
            }
        }
    }
}
