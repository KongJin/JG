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
        [SerializeField] private Button _logoutButton;
        [SerializeField] private Button _deleteAccountButton;

        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject _confirmDialog;
        [SerializeField] private TMP_Text _confirmMessage;
        [SerializeField] private Button _confirmYesButton;
        [SerializeField] private Button _confirmNoButton;

        private AccountSetup _setup;
        private EventBus _eventBus;
        private System.Action _onLogout;
        private System.Action _onDeleteAccount;

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
            if (_uidText != null) _uidText.text = $"UID: {profile.uid}";
            if (_authTypeText != null) _authTypeText.text = $"인증: {profile.authType}";
            if (_displayNameText != null) _displayNameText.text = $"닉네임: {profile.displayName}";
            if (_nicknameInput != null) _nicknameInput.text = profile.displayName;
        }

        private void HookButtons()
        {
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
                if (_nicknameMessage != null)
                    _nicknameMessage.text = $"계정 삭제 실패: {ex.Message}";
            }
        }

        private void OnConfirmNoClicked()
        {
            if (_confirmDialog != null) _confirmDialog.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_nicknameApplyButton != null) _nicknameApplyButton.onClick.RemoveAllListeners();
            if (_logoutButton != null) _logoutButton.onClick.RemoveAllListeners();
            if (_deleteAccountButton != null) _deleteAccountButton.onClick.RemoveAllListeners();
            if (_confirmYesButton != null) _confirmYesButton.onClick.RemoveAllListeners();
            if (_confirmNoButton != null) _confirmNoButton.onClick.RemoveAllListeners();
        }
    }
}

