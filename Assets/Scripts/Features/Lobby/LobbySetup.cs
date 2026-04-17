using Features.Account;
using Features.Account.Presentation;
using Features.Garage;
using Features.Garage.Application.Ports;
using Features.Lobby.Application;
using Features.Lobby.Application.Events;
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

    [Required, SerializeField]
    private LobbyView _view;

    [Required, SerializeField]
    private LobbyPhotonAdapter _photonAdapter;

    [Required, SerializeField]
    private SceneErrorPresenter _sceneErrorPresenter;

    [Required, SerializeField]
    private SoundPlayer _soundPlayer;

    [Header("Account")]
    [SerializeField]
    private AccountSetup _accountSetup;

    [SerializeField]
    private LoginLoadingView _loginLoadingView;

    [SerializeField]
    private AccountSettingsView _accountSettingsView;

    [SerializeField]
    private UnitSetup _unitSetup;

    [SerializeField]
    private GarageSetup _garageSetup;

    private LobbyNetworkEventHandler _syncHandler;
    private readonly EventBus _eventBus = new EventBus();
    private SceneLoaderAdapter _sceneLoader;
    private IAnalyticsPort _analytics;
    private float _sessionStartTime;
    private Features.Account.Domain.AccountProfile _currentAccountProfile;
    private bool _isSigningIn;

    private void Awake()
    {
        _sceneLoader = new SceneLoaderAdapter();
        _sceneErrorPresenter.Initialize(_eventBus);
        _eventBus.Subscribe(this, new System.Action<SceneLoadRequestedEvent>(OnSceneLoadRequested));

        _analytics = new FirebaseAnalyticsAdapter();
        _sessionStartTime = Time.realtimeSinceStartup;
        _analytics.LogSessionStart();
        RoundCounter.Reset();

        // Account 초기화 (익명 로그인)
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
            for (int attempt = 1; attempt <= MaxAutoSignInAttempts; attempt++)
            {
                try
                {
                    var result = await _accountSetup.SignInAnonymously.Execute();
                    if (result.IsSuccess)
                    {
                        _currentAccountProfile = result.Value;
                        await LoadSignedInAccount();
                        _loginLoadingView.OnLoginSuccess();
                        return;
                    }

                    _loginLoadingView.OnLoginFailed(result.Error ?? "Unknown error");
                }

                catch (System.Exception ex)
                {
                    Debug.LogError($"[LobbySetup] Anonymous sign-in failed: {ex.Message}");
                    _loginLoadingView.OnLoginFailed(ex.Message);
                }

                if (attempt < MaxAutoSignInAttempts)
                    await System.Threading.Tasks.Task.Delay(1000);
            }
        }
        finally
        {
            _isSigningIn = false;
        }
    }

    private async System.Threading.Tasks.Task LoadSignedInAccount()
    {
        if (_accountSetup?.LoadAccount == null)
            return;

        try
        {
            var accountData = await _accountSetup.LoadAccount.Execute();
            if (accountData?.Profile != null)
                _currentAccountProfile = accountData.Profile;
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
        var repository = new LobbyRepository();
        var network = _photonAdapter;
        var clock = new ClockAdapter();

        _syncHandler = new LobbyNetworkEventHandler(repository, _eventBus, network);

        var useCases = new LobbyUseCases(repository, network, clock);

        _soundPlayer.Initialize(_eventBus, SoundPlayer.LobbyOwnerId);

        _view.Initialize(_eventBus, _eventBus, useCases);
        _eventBus.Publish(new LobbyUpdatedEvent(repository.LoadLobby() ?? new DomainLobby()));

        if (_unitSetup != null)
            _unitSetup.Initialize(_eventBus);

        if (_garageSetup != null)
        {
            if (_unitSetup == null)
            {
                Debug.LogWarning(
                    "[LobbySetup] GarageSetup is assigned but UnitSetup is missing. Garage initialization is skipped.",
                    this
                );
            }
            else
            {
                _garageSetup.Initialize(
                    _eventBus,
                    _unitSetup.CompositionPort,
                    _unitSetup.Catalog,
                    _accountSetup?.DataPort
                );
            }
        }

        InitializeAccountSettingsView();
    }

    private void InitializeAccountSettingsView()
    {
        if (_accountSettingsView == null || _accountSetup == null || _currentAccountProfile == null)
            return;

        _accountSettingsView.Initialize(
            _accountSetup,
            _eventBus,
            OnAccountLogoutRequested,
            OnAccountDeleted
        );
        _accountSettingsView.Render(_currentAccountProfile);
    }

    private void OnAccountLogoutRequested()
    {
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
        _analytics?.LogSessionEnd(elapsed);
        _analytics?.LogDropOff("lobby", elapsed);
    }

    private void OnSceneLoadRequested(SceneLoadRequestedEvent e)
    {
        _sceneLoader.LoadScene(e.SceneName);
    }

    private void OnDestroy()
    {
        _accountSetup?.Cleanup();
        _garageSetup?.Cleanup();
        _unitSetup?.Cleanup();
    }
}
