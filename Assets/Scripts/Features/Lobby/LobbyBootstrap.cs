using Features.Lobby.Application;
using Features.Lobby.Application.Events;
using Features.Lobby.Infrastructure;
using Features.Lobby.Infrastructure.Persistence;
using Features.Lobby.Infrastructure.Photon;
using Features.Lobby.Presentation;
using Shared.Analytics;
using Shared.EventBus;
using Shared.Time;
using Shared.Ui;
using UnityEngine;
using DomainLobby = Features.Lobby.Domain.Lobby;

public sealed class LobbyBootstrap : MonoBehaviour
{
    [SerializeField]
    private LobbyView _view;

    [SerializeField]
    private LobbyPhotonAdapter _photonAdapter;

    [SerializeField]
    private SceneLoaderAdapter _sceneLoader;

    [SerializeField]
    private SceneErrorPresenter _sceneErrorPresenter;

    private LobbyNetworkEventHandler _syncHandler;
    private readonly EventBus _eventBus = new EventBus();
    private IAnalyticsPort _analytics;
    private float _sessionStartTime;

    private void Awake()
    {
        if (_view == null)
        {
            Debug.LogError("[Lobby] LobbyView reference is missing.");
            return;
        }

        if (_photonAdapter == null)
        {
            _photonAdapter = GetComponent<LobbyPhotonAdapter>();
            if (_photonAdapter == null)
            {
                Debug.LogError("[Lobby] LobbyPhotonAdapter reference is missing.");
                return;
            }
        }

        if (_sceneLoader == null)
        {
            _sceneLoader = new SceneLoaderAdapter();
        }

        if (_sceneErrorPresenter == null)
        {
            Debug.LogError("[Lobby] SceneErrorPresenter reference is missing.");
            return;
        }

        _sceneErrorPresenter.Initialize(_eventBus);
        _eventBus.Subscribe(this, new System.Action<SceneLoadRequestedEvent>(OnSceneLoadRequested));

        _analytics = new FirebaseAnalyticsAdapter();
        _sessionStartTime = Time.realtimeSinceStartup;
        _analytics.LogSessionStart();
        RoundCounter.Reset();

        var repository = new LobbyRepository();
        var network = _photonAdapter;
        var clock = new ClockAdapter();

        _syncHandler = new LobbyNetworkEventHandler(repository, _eventBus, network);

        var useCases = new LobbyUseCases(repository, network, clock);

        _view.Initialize(_eventBus, _eventBus, useCases);
        _eventBus.Publish(new LobbyUpdatedEvent(repository.LoadLobby() ?? new DomainLobby()));
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
}
