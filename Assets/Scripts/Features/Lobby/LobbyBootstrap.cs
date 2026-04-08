using Shared.Attributes;
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
using Shared.EventBus;
using Shared.Runtime.Sound;
using Shared.Time;
using Shared.Ui;
using UnityEngine;
using DomainLobby = Features.Lobby.Domain.Lobby;

public sealed class LobbyBootstrap : MonoBehaviour
{
    [Required, SerializeField]
    private LobbyView _view;

    [Required, SerializeField]
    private LobbyPhotonAdapter _photonAdapter;

    [Required, SerializeField]
    private SceneErrorPresenter _sceneErrorPresenter;

    [Required, SerializeField]
    private SoundPlayer _soundPlayer;

    [Required, SerializeField]
    private UnitBootstrap _unitBootstrap;

    [Required, SerializeField]
    private GarageBootstrap _garageBootstrap;

    private LobbyNetworkEventHandler _syncHandler;
    private readonly EventBus _eventBus = new EventBus();
    private SceneLoaderAdapter _sceneLoader;
    private IAnalyticsPort _analytics;
    private float _sessionStartTime;

    private void Awake()
    {
        _sceneLoader = new SceneLoaderAdapter();
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

        _soundPlayer.Initialize(_eventBus, SoundPlayer.LobbyOwnerId);

        _view.Initialize(_eventBus, _eventBus, useCases);
        _eventBus.Publish(new LobbyUpdatedEvent(repository.LoadLobby() ?? new DomainLobby()));

        // Unit Feature 초기화 (Garage보다 먼저)
        _unitBootstrap.Initialize(_eventBus);

        // Garage Feature 초기화 (Unit이 제공하는 compositionPort 사용)
        _garageBootstrap.Initialize(
            _eventBus,
            _unitBootstrap.CompositionPort,
            _unitBootstrap.Catalog);
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
        _garageBootstrap?.Cleanup();
        _unitBootstrap?.Cleanup();
    }
}

