using System.Collections.Generic;
using Features.Combat;
using Features.Combat.Presentation;
using Features.Garage;
using Features.Player.Application.Ports;
using Features.Player.Infrastructure;
using Features.Player.Presentation;
using Features.Projectile;
using Features.Status;
using Features.Unit;
using Features.Unit.Presentation;
using Features.Wave;
using Features.Zone;
using Photon.Pun;
using Shared.Attributes;
using Shared.ErrorHandling;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Lifecycle;
using Shared.Runtime;
using Shared.Ui;
using UnityEngine;
using UnityEngine.Serialization;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Features.Player
{
    public sealed class GameSceneRoot : MonoBehaviourPunCallbacks
    {
        [Header("Player")]
        [SerializeField] private string _playerPrefabName = "PlayerCharacter";
        [SerializeField] private float _spawnRadius = 3f;
        [Required, SerializeField] private Camera _camera;
        [Required, SerializeField] private CameraFollower _cameraFollower;
        [SerializeField] private GameObject _healthHudPrefab;
        [Required, SerializeField] private PlayerSpecConfig _playerSpecConfig;
        [FormerlySerializedAs("_projectileSpawner")]
        [Required, SerializeField] private ProjectileSetup _projectileSetup;
        [Required, SerializeField] private CombatSetup _combatSetup;
        [Required, SerializeField] private ZoneSetup _zoneSetup;
        [Required, SerializeField] private SceneErrorPresenter _sceneErrorPresenter;
        [Required, SerializeField] private PlayerSceneRegistry _playerSceneRegistry;
        [Required, SerializeField] private EnergyBarView _energyBarView;
        // Runtime-spawned by PhotonNetwork.Instantiate and assigned via PlayerSceneRegistry arrival.
        [SerializeField] private PlayerSetup _localPlayerSetup;

        [Header("Unit & Garage")]
        [Required, SerializeField] private UnitSetup _unitSetup;
        [Required, SerializeField] private GarageSetup _garageSetup;

        [Header("Unit Summon UI")]
        [SerializeField] private UnitSlotsContainer _unitSlotsContainer;
        [Header("Combat Feedback")]
        [SerializeField] private DamageNumberSpawner _damageNumberSpawner;

        [Header("Status (Buff/Debuff)")]
        [Required, SerializeField] private StatusSetup _statusSetup;

        [Header("Wave (PvE)")]
        [SerializeField] private WaveSetup _waveSetup;
        [Tooltip("PvE일 때 필수. Combat.Initialize 직후 RegisterTarget.")]
        [SerializeField] private CoreObjectiveSetup _coreObjective;

        [Header("Scene Transition")]
        [SerializeField] private string _lobbySceneName = "LobbyScene";

        [Header("Scene Event Consumers")]
        [SerializeField] private MonoBehaviour[] _eventBusConsumers;

        private EventBus _eventBus;
        private DisposableScope _disposables;
        private IPlayerLookupPort _playerLookup;
        private readonly Queue<PlayerSetup> _pendingRemotePlayers = new();
        private bool _remotePlayerWiringReady;

        private readonly GameSceneGarageBootstrapFlow _garageBootstrapFlow = new();
        private readonly GameScenePlayerConnector _playerConnector = new();
        private readonly GameSceneAudioBootstrapFlow _audioBootstrapFlow = new();
        private readonly GameSceneEndReportingFlow _endReportingFlow = new();
        private readonly GameSceneLocalPlayerInitializationFlow _localPlayerInitializationFlow = new();

        private UnitSpec[] InitializeUnitAndGarage(PlayerSetup localPlayerSetup)
        {
            var bootstrapResult = _garageBootstrapFlow.Initialize(
                _eventBus,
                _unitSetup,
                _garageSetup,
                localPlayerSetup.PlayerId);

            var units = bootstrapResult.PlayerUnits;
            Debug.Log($"[GameSceneRoot] Computed {units.Length} unit specs for player {localPlayerSetup.PlayerId.Value}");
            return units;
        }

        private void Awake()
        {
            _playerSceneRegistry.PlayerArrived += OnPlayerArrived;
            _playerSceneRegistry.DrainPendingArrivals(OnPlayerArrived);
        }

        private void CompleteLocalPlayerInitialization()
        {
            _localPlayerInitializationFlow.Execute(new GameSceneLocalPlayerInitializationContext(
                _eventBus,
                _disposables,
                _localPlayerSetup,
                _camera,
                _cameraFollower,
                _playerSpecConfig,
                _playerSceneRegistry,
                _playerLookup,
                _statusSetup,
                _combatSetup,
                _zoneSetup,
                _projectileSetup,
                _coreObjective,
                _waveSetup,
                _unitSetup,
                _unitSlotsContainer,
                _energyBarView,
                _damageNumberSpawner,
                _audioBootstrapFlow,
                _endReportingFlow,
                ConnectPlayer,
                MarkRemotePlayerWiringReady,
                InitializeUnitAndGarage));
        }

        private void Start()
        {
            _remotePlayerWiringReady = false;
            _eventBus = new EventBus();
            _disposables = new DisposableScope();
            InitializeEventBusConsumers();

            _endReportingFlow.StartSession(
                _eventBus,
                _disposables,
                PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "unknown",
                Time.realtimeSinceStartup,
                _lobbySceneName);

            _playerLookup = new PlayerLookupAdapter(_playerSceneRegistry);
            _sceneErrorPresenter.Initialize(_eventBus);

            if (!PhotonNetwork.InRoom)
            {
                _eventBus.Publish(
                    new UiErrorRequestedEvent(
                        UiErrorMessage.Modal(
                            "You are not connected to a room. Please return to the lobby and join again.",
                            "GameScene"
                        )
                    )
                );
                return;
            }

            // Spawn local player
            var offset = Random.insideUnitCircle * _spawnRadius;
            var spawnPosition = new Vector3(offset.x, 0f, offset.y);
            var player = PhotonNetwork.Instantiate(
                _playerPrefabName,
                spawnPosition,
                Quaternion.identity);

            // PlayerSceneRegistry arrival is raised when the local Photon instance arrives.
            // CompleteLocalPlayerInitialization() runs from OnPlayerArrived.
        }

        private void InitializeEventBusConsumers()
        {
            if (_eventBusConsumers == null)
                return;

            for (var i = 0; i < _eventBusConsumers.Length; i++)
            {
                if (_eventBusConsumers[i] is IGameSceneEventBusConsumer consumer)
                    consumer.Initialize(_eventBus);
            }
        }

        private void ConnectPlayer(PlayerSetup setup)
        {
            if (!_playerConnector.Connect(
                setup,
                _eventBus,
                _statusSetup,
                _playerSceneRegistry,
                _playerLookup,
                new DefaultPlayerSpecProvider(_playerSpecConfig)))
                return;

            if (_healthHudPrefab != null)
            {
                var hudView = ComponentAccess.InstantiateComponent<PlayerHealthHudView>(
                    _healthHudPrefab,
                    transform);
                hudView.Initialize(
                    _eventBus,
                    setup.PlayerId,
                    setup.MaxHp,
                    setup.NetworkAdapter.IsMine,
                    setup.transform,
                    _camera);
            }

            _combatSetup.RegisterTarget(setup.PlayerId, setup.CombatTargetProvider);

            if (_waveSetup != null)
                _waveSetup.RegisterPlayer(setup.transform);
        }

        private void MarkRemotePlayerWiringReady()
        {
            _remotePlayerWiringReady = true;
            while (_pendingRemotePlayers.Count > 0)
                ConnectPlayer(_pendingRemotePlayers.Dequeue());
        }

        private void OnPlayerArrived(PlayerSetup setup)
        {
            if (setup == null)
                return;

            if (setup.NetworkAdapter.IsMine)
            {
                _localPlayerSetup = setup;
                CompleteLocalPlayerInitialization();
                return;
            }

            if (!_remotePlayerWiringReady)
            {
                _pendingRemotePlayers.Enqueue(setup);
                return;
            }

            ConnectPlayer(setup);
        }

        private void OnDestroy()
        {
            if (_playerSceneRegistry != null)
                _playerSceneRegistry.PlayerArrived -= OnPlayerArrived;

            _endReportingFlow.Dispose();

            _disposables?.Dispose();
        }

        public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
        {
            _endReportingFlow.HandleDisconnected();
        }

        public override void OnLeftRoom()
        {
            _endReportingFlow.HandleLeftRoom();
        }
    }
}
