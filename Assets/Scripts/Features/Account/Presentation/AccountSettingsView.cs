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

            if (_uidText != null) _uidText.text = $"UID: {profile.uid}";
            if (_authTypeText != null) _authTypeText.text = $"인증: {profile.authType}";
            if (_displayNameText != null) _displayNameText.text = $"닉네임: {profile.displayName}";
            if (_nicknameInput != null) _nicknameInput.text = profile.displayName;

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
                SetStatusMessage("AccountSetup이 연결되지 않았습니다.");
                return;
            }

            if (_currentProfile != null && _currentProfile.authType == "google")
            {
                SetStatusMessage("이미 Google 계정으로 연결되어 있습니다.");
                RefreshGoogleButtonState();
                return;
            }

            if (string.IsNullOrWhiteSpace(_setup.GoogleWebClientId))
            {
                SetStatusMessage("googleWebClientId가 비어 있습니다.");
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            if (_googleSignInButton != null)
                _googleSignInButton.interactable = false;

            SetStatusMessage("Google 로그인 창을 여는 중...");
            AccountGoogleSignIn_RequestIdToken(
                _setup.GoogleWebClientId,
                gameObject.name,
                nameof(OnGoogleIdTokenReceived),
                nameof(OnGoogleIdTokenFailed));
#else
            SetStatusMessage("Google 로그인은 WebGL 빌드에서만 사용할 수 있습니다.");
#endif
        }

        public void OnGoogleIdTokenReceived(string googleIdToken)
        {
            _ = CompleteGoogleSignInAsync(googleIdToken);
        }

        public void OnGoogleIdTokenFailed(string errorMessage)
        {
            SetStatusMessage(string.IsNullOrWhiteSpace(errorMessage)
                ? "Google 로그인에 실패했습니다."
                : $"Google 로그인 실패: {errorMessage}");
            RefreshGoogleButtonState();
        }

        private async System.Threading.Tasks.Task CompleteGoogleSignInAsync(string googleIdToken)
        {
            try
            {
                SetStatusMessage("Google 계정을 연결하는 중...");

                var result = await _setup.SignInWithGoogle.Execute(googleIdToken);
                if (result.IsFailure)
                {
                    SetStatusMessage(result.Error);
                    return;
                }

                Render(result.Value);
                SetStatusMessage("Google 계정 연결이 완료되었습니다.");
            }
            catch (System.Exception ex)
            {
                SetStatusMessage($"Google 로그인 실패: {ex.Message}");
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
                _nicknameMessage.text = result.IsSuccess ? "닉네임이 변경되었습니다." : result.Error;

            if (result.IsSuccess && _displayNameText != null)
                _displayNameText.text = $"닉네임: {newName}";
        }

        private void OnLogoutClicked()
        {
            _onLogout?.Invoke();
        }

        private void OnDeleteAccountClicked()
        {
            if (_confirmDialog != null) _confirmDialog.SetActive(true);
            if (_confirmMessage != null) _confirmMessage.text = "정말 계정을 삭제하시겠습니까?\n모든 데이터가 영구 삭제됩니다.";
        }

        private async void OnConfirmYesClicked()
        {
            if (_confirmDialog != null) _confirmDialog.SetActive(false);
            if (_setup == null) return;

            try
            {
                await _setup.DeleteAccount.Execute();
                _onDeleteAccount?.Invoke();
            }
            catch (System.Exception ex)
            {
                SetStatusMessage($"계정 삭제 실패: {ex.Message}");
            }
        }

        private void OnConfirmNoClicked()
        {
            if (_confirmDialog != null) _confirmDialog.SetActive(false);
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
