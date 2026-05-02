using System;
using System.Threading.Tasks;
using Features.Account.Application;
using Features.Account.Application.Ports;
using Features.Account.Domain;
using Shared.Kernel;

namespace Features.Account.Presentation
{
    public sealed class AccountSettingsInputHandler
    {
        private readonly SignInWithGoogleUseCase _signInWithGoogle;
        private readonly ChangeDisplayNameUseCase _changeDisplayName;
        private readonly DeleteAccountUseCase _deleteAccount;
        private readonly IGoogleSignInRequestPort _googleSignInRequestPort;
        private readonly Action _onLogout;
        private readonly Action _onDeleteAccount;

        public AccountSettingsInputHandler(
            SignInWithGoogleUseCase signInWithGoogle,
            ChangeDisplayNameUseCase changeDisplayName,
            DeleteAccountUseCase deleteAccount,
            IGoogleSignInRequestPort googleSignInRequestPort,
            Action onLogout,
            Action onDeleteAccount)
        {
            _signInWithGoogle = signInWithGoogle;
            _changeDisplayName = changeDisplayName;
            _deleteAccount = deleteAccount;
            _googleSignInRequestPort = googleSignInRequestPort;
            _onLogout = onLogout;
            _onDeleteAccount = onDeleteAccount;
        }

        public bool CanRequestGoogleSignIn(out string unavailableMessage)
        {
            if (!_googleSignInRequestPort.HasClientId)
            {
                unavailableMessage = "googleWebClientId is empty.";
                return false;
            }

            if (!_googleSignInRequestPort.IsAvailable)
            {
                unavailableMessage = "Google sign-in is only available in WebGL builds.";
                return false;
            }

            unavailableMessage = string.Empty;
            return true;
        }

        public void RequestGoogleIdToken(
            string callbackObjectName,
            string successMethodName,
            string errorMethodName)
        {
            _googleSignInRequestPort.RequestIdToken(
                callbackObjectName,
                successMethodName,
                errorMethodName);
        }

        public Task<Result<AccountProfile>> CompleteGoogleSignInAsync(string googleIdToken)
        {
            return _signInWithGoogle.Execute(googleIdToken);
        }

        public Task<Result> ChangeDisplayNameAsync(string newName)
        {
            return _changeDisplayName.Execute(newName);
        }

        public async Task DeleteAccountAsync()
        {
            await _deleteAccount.Execute();
            _onDeleteAccount?.Invoke();
        }

        public void Logout()
        {
            _onLogout?.Invoke();
        }
    }
}
