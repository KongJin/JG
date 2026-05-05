using Features.Account;
using Features.Account.Application;
using Features.Account.Application.Ports;
using Features.Account.Presentation;
using Features.Garage;
using Features.Garage.Application.Ports;
using Features.Lobby.Application;
using Features.Lobby.Infrastructure;
using Features.Lobby.Infrastructure.Persistence;
using Features.Lobby.Infrastructure.Photon;
using Features.Lobby.Presentation;
using Features.Unit;
using Features.Unit.Infrastructure;
using Shared.Analytics;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Runtime.Sound;
using Shared.Time;
using Shared.Ui;
using UnityEngine;
using UnityEngine.SceneManagement;
using DomainLobby = Features.Lobby.Domain.Lobby;

public sealed class LobbySetup : MonoBehaviour
{
    private const int MaxAutoSignInAttempts = 3;
    private const string BattleSceneName = "BattleScene";

    [Required, SerializeField]
    private LobbyPageController _view;

    [Required, SerializeField]
    private LobbyPhotonAdapter _photonAdapter;

    [Required, SerializeField]
    private SceneErrorPresenter _sceneErrorPresenter;

    [Required, SerializeField]
    private SoundPlayer _soundPlayer;

    [Header("Account")]
    [SerializeField]
// csharp-guardrails: allow-serialized-field-without-required
    private AccountSetup _accountSetup;

    [SerializeField]
// csharp-guardrails: allow-serialized-field-without-required
    private LoginLoadingView _loginLoadingView;

    [SerializeField]
// csharp-guardrails: allow-serialized-field-without-required
    private AccountSettingsView _accountSettingsView;

    [SerializeField]
// csharp-guardrails: allow-serialized-field-without-required
    private UnitSetup _unitSetup;

    [SerializeField]
// csharp-guardrails: allow-serialized-field-without-required
    private GarageSetup _garageSetup;

    private LobbyNetworkEventHandler _syncHandler;
    private readonly EventBus _eventBus = new EventBus();
    private SceneLoaderAdapter _sceneLoader;
    private IAnalyticsPort _analytics;
    private float _sessionStartTime;
    private Features.Account.Domain.AccountProfile _currentAccountProfile;
    private AccountData _loadedAccountData;
    private bool _isSigningIn;
    private readonly LobbyAccountBootstrapFlow _accountBootstrapFlow = new();
    private readonly LobbySceneInitializationFlow _sceneInitializationFlow = new();

    private void Awake()
    {
        _sceneLoader = new SceneLoaderAdapter();
        _sceneErrorPresenter.Initialize(_eventBus);

        _analytics = new FirebaseAnalyticsAdapter();
        _sessionStartTime = Time.realtimeSinceStartup;
        _analytics.LogSessionStart();
        RoundCounter.Reset();

        // Account 초기화 (익명 로그인)
// csharp-guardrails: allow-null-defense
        if (_accountSetup != null && _loginLoadingView != null)
        {
            _accountSetup.Initialize(_eventBus);
            _loginLoadingView.SetOnLoginSuccess(OnAccountLoginSuccess);
            _loginLoadingView.SetOnRetryRequested(OnLoginRetryRequested);
            _loginLoadingView.Show();
            _ = RunAnonymousSignIn();
            return; // 로그인 성공 후 나머지는 OnAccountLoginSuccess에서 계속
        }

        InitializeLobby();
    }

    private async System.Threading.Tasks.Task RunAnonymousSignIn()
    {
        if (_isSigningIn)
            return;

        _isSigningIn = true;

        try
        {
            var signInResult = await _accountBootstrapFlow.RunAnonymousSignInAsync(
                _accountSetup,
                _loginLoadingView,
                MaxAutoSignInAttempts,
                async profile =>
                {
                    _currentAccountProfile = profile;
                    await LoadSignedInAccount();
// csharp-guardrails: allow-null-defense
                    if (_loginLoadingView != null)
                        _loginLoadingView.OnLoginSuccess();
                });

            if (signInResult.HasProfile)
                _currentAccountProfile = signInResult.Profile;
        }
        finally
        {
            _isSigningIn = false;
        }
    }

    private async System.Threading.Tasks.Task LoadSignedInAccount()
    {
// csharp-guardrails: allow-null-defense
        if (_accountSetup?.LoadAccount == null)
            return;

        try
        {
            var accountData = await _accountSetup.LoadAccount.Execute();
// csharp-guardrails: allow-null-defense
            if (accountData != null)
            {
                _loadedAccountData = accountData;
// csharp-guardrails: allow-null-defense
                if (accountData.Profile != null)
                    _currentAccountProfile = accountData.Profile;
// csharp-guardrails: allow-null-defense
                _view?.RenderAccountState(_currentAccountProfile, _loadedAccountData);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[LobbySetup] Account data load failed after sign-in: {ex.Message}");
        }
    }

    private void OnAccountLoginSuccess()
    {
        InitializeLobby();
    }

    private void OnLoginRetryRequested()
    {
        _ = RunAnonymousSignIn();
    }

    private void InitializeLobby()
    {
        var initializedScene = _sceneInitializationFlow.Initialize(
            _eventBus,
            _view,
                _photonAdapter,
                _soundPlayer,
                _unitSetup,
                _garageSetup,
                _sceneLoader,
                BattleSceneName,
// csharp-guardrails: allow-null-defense
                _accountSetup?.DataPort,
                ApplyLoadedAccountSettings);

        _syncHandler = initializedScene.SyncHandler;

        InitializeAccountSettingsView();
    }

    private void InitializeAccountSettingsView()
    {
// csharp-guardrails: allow-null-defense
        if (_accountSettingsView == null || _accountSetup == null || _currentAccountProfile == null)
            return;

        var accountSettingsInputHandler = new AccountSettingsInputHandler(
            _accountSetup.SignInWithGoogle,
            _accountSetup.ChangeDisplayName,
            _accountSetup.DeleteAccount,
            _accountSetup.GoogleSignInRequestPort,
            OnAccountLogoutRequested,
            OnAccountDeleted
        );
        _accountSettingsView.Initialize(accountSettingsInputHandler);
        _accountSettingsView.Render(_currentAccountProfile);
// csharp-guardrails: allow-null-defense
        _view?.RenderAccountState(_currentAccountProfile, _loadedAccountData);
    }

    private void ApplyLoadedAccountSettings()
    {
// csharp-guardrails: allow-null-defense
        if (_soundPlayer == null)
            return;

// csharp-guardrails: allow-null-defense
        float masterVolume = _loadedAccountData?.Settings?.masterVolume ?? 1f;
// csharp-guardrails: allow-null-defense
        float bgmVolume = _loadedAccountData?.Settings?.bgmVolume ?? 0.8f;
// csharp-guardrails: allow-null-defense
        float sfxVolume = _loadedAccountData?.Settings?.sfxVolume ?? 1f;
        _soundPlayer.SetMasterVolume(masterVolume);
        _soundPlayer.SetChannelVolumes(bgmVolume, sfxVolume);
    }

    private void OnAccountLogoutRequested()
    {
// csharp-guardrails: allow-null-defense
        _accountSetup.AuthPort?.SignOut();
        ReloadCurrentScene();
    }

    private void OnAccountDeleted()
    {
        ReloadCurrentScene();
    }

    private void ReloadCurrentScene()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrWhiteSpace(sceneName))
            _sceneLoader.LoadScene(sceneName);
    }

    private void OnApplicationQuit()
    {
        var elapsed = Time.realtimeSinceStartup - _sessionStartTime;
// csharp-guardrails: allow-null-defense
        _analytics?.LogSessionEnd(elapsed);
// csharp-guardrails: allow-null-defense
        _analytics?.LogDropOff("lobby", elapsed);
    }

    private void OnDestroy()
    {
        // csharp-guardrails: allow-null-defense
        _accountSetup?.Cleanup();
        // csharp-guardrails: allow-null-defense
        _unitSetup?.Cleanup();
    }
}
