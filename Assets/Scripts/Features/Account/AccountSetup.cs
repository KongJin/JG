using Features.Account.Application;
using Features.Account.Application.Ports;
using Features.Account.Infrastructure;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Lifecycle;
using UnityEngine;

namespace Features.Account
{
    /// <summary>
    /// Account Feature Composition Root.
    /// </summary>
    public sealed class AccountSetup : MonoBehaviour
    {
        [Required, SerializeField] private AccountConfig _config;

        private DisposableScope _disposables;

        // Ports
        public IAuthPort AuthPort { get; private set; }
        public IAccountDataPort DataPort { get; private set; }
        public string GoogleWebClientId => _config.googleWebClientId;

        // UseCases
        public SignInAnonymouslyUseCase SignInAnonymously { get; private set; }
        public SignInWithGoogleUseCase SignInWithGoogle { get; private set; }
        public LoadAccountUseCase LoadAccount { get; private set; }
        public SaveAccountUseCase SaveAccount { get; private set; }
        public ChangeDisplayNameUseCase ChangeDisplayName { get; private set; }
        public DeleteAccountUseCase DeleteAccount { get; private set; }

        /// <summary>
        /// Account Feature 초기화.
        /// </summary>
        public void Initialize(EventBus eventBus)
        {
            _disposables?.Dispose();
            _disposables = new DisposableScope();

            // Infrastructure
            var authAdapter = new FirebaseAuthRestAdapter(_config.firebaseApiKey);
            var firestorePort = new FirestoreRestPort(_config.firebaseApiKey, _config.projectId, authAdapter);

            AuthPort = authAdapter;
            DataPort = firestorePort;

            // UseCases
            SignInAnonymously = new SignInAnonymouslyUseCase(AuthPort, DataPort);
            SignInWithGoogle = new SignInWithGoogleUseCase(AuthPort, DataPort);
            LoadAccount = new LoadAccountUseCase(AuthPort, DataPort);
            SaveAccount = new SaveAccountUseCase(AuthPort, DataPort);
            ChangeDisplayName = new ChangeDisplayNameUseCase(AuthPort, DataPort);
            DeleteAccount = new DeleteAccountUseCase(AuthPort, DataPort);
        }

        public void Cleanup()
        {
            _disposables?.Dispose();
            _disposables = null;
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
