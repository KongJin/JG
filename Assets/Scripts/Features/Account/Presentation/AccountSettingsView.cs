using Features.Account.Domain;
using UnityEngine;

namespace Features.Account.Presentation
{
    /// <summary>
    /// Account settings state holder for the UI Toolkit shell.
    /// </summary>
    public sealed class AccountSettingsView : MonoBehaviour
    {
        private AccountSettingsInputHandler _inputHandler;
        private AccountProfile _currentProfile;
        private bool _deleteConfirmationPending;

        public string AuthStatusText { get; private set; } = string.Empty;
        public string DisplayNameText { get; private set; } = string.Empty;
        public string StatusMessage { get; private set; } = string.Empty;
        public string DeleteActionLabel { get; private set; } = "Delete Account";
        public bool GoogleSignInAvailable { get; private set; }

        public void Initialize(AccountSettingsInputHandler inputHandler)
        {
            _inputHandler = inputHandler;
        }

        public void Render(AccountProfile profile)
        {
            _currentProfile = profile;
            _deleteConfirmationPending = false;
            AuthStatusText = BuildAuthStatusText(profile);
            DisplayNameText = BuildDisplayNameText(profile);
            StatusMessage = BuildDefaultStatusMessage(profile);
            RefreshDeleteButtonState();
            RefreshGoogleButtonState();
        }

        public void RequestGoogleSignIn()
        {
            EnsureInitialized();

            if (_currentProfile != null && _currentProfile.authType == "google")
            {
                StatusMessage = "Already linked with Google account.";
                RefreshGoogleButtonState();
                return;
            }

            if (!_inputHandler.CanRequestGoogleSignIn(out var unavailableMessage))
            {
                StatusMessage = unavailableMessage;
                return;
            }

            StatusMessage = "Opening Google sign-in...";
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
            StatusMessage = string.IsNullOrWhiteSpace(errorMessage)
                ? "Google sign-in failed."
                : $"Google sign-in failed: {errorMessage}";
            RefreshGoogleButtonState();
        }

        public void ApplyNickname(string newName)
        {
            _ = RunNicknameApplyAsync(newName);
        }

        public void Logout()
        {
            _inputHandler?.Logout();
        }

        public void RequestDeleteAccount()
        {
            if (!_deleteConfirmationPending)
            {
                _deleteConfirmationPending = true;
                RefreshDeleteButtonState();
                StatusMessage = "Press delete once more to permanently remove this account.";
                return;
            }

            ConfirmDeleteAccount();
        }

        public void ConfirmDeleteAccount()
        {
            _ = RunDeleteAccountAsync();
        }

        public void CancelDeleteAccount()
        {
            _deleteConfirmationPending = false;
            RefreshDeleteButtonState();
        }

        private async System.Threading.Tasks.Task CompleteGoogleSignInAsync(string googleIdToken)
        {
            try
            {
                StatusMessage = "Linking Google account...";
                var result = await _inputHandler.CompleteGoogleSignInAsync(googleIdToken);
                if (result.IsFailure)
                {
                    StatusMessage = result.Error;
                    return;
                }

                Render(result.Value);
                StatusMessage = "Google account linked successfully.";
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Google sign-in failed: {ex.Message}";
            }
            finally
            {
                RefreshGoogleButtonState();
            }
        }

        private async System.Threading.Tasks.Task RunNicknameApplyAsync(string newName)
        {
            EnsureInitialized();
            var result = await _inputHandler.ChangeDisplayNameAsync(newName);
            StatusMessage = result.IsSuccess ? "Nickname changed." : result.Error;

            if (result.IsSuccess)
                DisplayNameText = newName;
        }

        private async System.Threading.Tasks.Task RunDeleteAccountAsync()
        {
            EnsureInitialized();

            try
            {
                StatusMessage = "Deleting account...";
                await _inputHandler.DeleteAccountAsync();
            }
            catch (System.Exception ex)
            {
                _deleteConfirmationPending = false;
                RefreshDeleteButtonState();
                StatusMessage = $"Account deletion failed: {ex.Message}";
            }
        }

        private void RefreshGoogleButtonState()
        {
            GoogleSignInAvailable = _currentProfile == null || _currentProfile.authType != "google";
        }

        private void RefreshDeleteButtonState()
        {
            DeleteActionLabel = _deleteConfirmationPending ? "Confirm Delete" : "Delete Account";
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

        private void EnsureInitialized()
        {
            if (_inputHandler == null)
                throw new System.InvalidOperationException("AccountSettingsView.Initialize must be called before interaction.");
        }
    }
}
