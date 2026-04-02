using System.Collections.Generic;
using Features.Combat;
using Features.Combat.Application;
using Features.Player.Application;
using Features.Player.Application.Ports;
using Features.Player.Infrastructure;
using Features.Player.Presentation;
using Features.Projectile;
using Features.Skill;
using Features.Status;
using Features.Wave;
using Features.Zone;
using Photon.Pun;
using Shared.Analytics;
using Shared.Attributes;
using Shared.ErrorHandling;
using Shared.EventBus;
using Shared.Lifecycle;
using Shared.Runtime.Sound;
using Shared.Ui;
using UnityEngine;

namespace Features.Player
{
    public sealed class GameSceneBootstrap : MonoBehaviourPunCallbacks
    {
        [Header("Player")]
        [SerializeField] private string _playerPrefabName = "PlayerCharacter";
        [SerializeField] private float _spawnRadius = 3f;
        [Required, SerializeField] private Camera _camera;
        [Required, SerializeField] private CameraFollower _cameraFollower;
        [Required, SerializeField] private GameObject _healthHudPrefab;
        [Required, SerializeField] private Canvas _hudCanvas;
        [Required, SerializeField] private SkillSetup _skillSetup;
        [Required, SerializeField] private ProjectileSpawner _projectileSpawner;
        [Required, SerializeField] private CombatBootstrap _combatBootstrap;
        [Required, SerializeField] private ZoneSetup _zoneSetup;
        [Required, SerializeField] private SceneErrorPresenter _sceneErrorPresenter;
        [Required, SerializeField] private SoundPlayer _soundPlayer;
        [Required, SerializeField] private PlayerSceneRegistry _playerSceneRegistry;
        [Required, SerializeField] private ManaRegenTicker _manaRegenTicker;
        [Required, SerializeField] private ManaBarView _manaBarView;
        [Required, SerializeField] private BleedoutTicker _bleedoutTicker;
        [Required, SerializeField] private RescueChannelTicker _rescueChannelTicker;
        [Required, SerializeField] private DownedOverlayView _downedOverlayView;
        [Required, SerializeField] private InvulnerabilityTicker _invulnerabilityTicker;

        [Header("Status (Buff/Debuff)")]
        [Required, SerializeField] private StatusSetup _statusSetup;

        [Header("Wave (PvE)")]
        [SerializeField] private WaveBootstrap _waveBootstrap;

        private EventBus _eventBus;
        private DisposableScope _disposables;
        private IAnalyticsPort _analytics;
        private IPlayerLookupPort _playerLookup;
        private string _matchId;
        private float _sceneStartTime;
        private bool _dropOffLogged;
        private readonly Queue<PlayerSetup> _pendingRemotePlayers = new();
        private bool _remotePlayerWiringReady;

        private void Awake()
        {
            PlayerSetup.RemoteArrived += OnRemotePlayerArrived;
        }

        private void Start()
        {
            _remotePlayerWiringReady = false;
            _eventBus = new EventBus();
            _disposables = new DisposableScope();

            // Analytics
            _analytics = new FirebaseAnalyticsAdapter();
            _matchId = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "unknown";
            _sceneStartTime = Time.realtimeSinceStartup;
            _analytics.LogGameStart(_matchId);
            RoundCounter.Increment();
            var analyticsHandler = new GameAnalyticsEventHandler(_analytics, _eventBus);
            _disposables.Add(EventBusSubscription.ForOwner(_eventBus, analyticsHandler));

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
                Quaternion.identity
            );

            _cameraFollower.Initialize(player.transform, _camera.transform.position - player.transform.position);

            var localSetup = player.GetComponent<PlayerSetup>();

            // Status (must initialize before PlayerSetup so SpeedModifier is ready)
            _statusSetup.Initialize(_eventBus, localSetup.StatusNetworkAdapter, localSetup.StatusNetworkAdapter, PhotonNetwork.IsMasterClient);

            localSetup.Initialize(_eventBus, speedModifier: _statusSetup.SpeedModifier, sceneRegistry: _playerSceneRegistry, playerLookup: _playerLookup);

            // Combat
            FriendlyFireScalingAdapter ffScaling = null;
            if (_waveBootstrap != null)
            {
                ffScaling = new FriendlyFireScalingAdapter(_eventBus);
                _disposables.Add(EventBusSubscription.ForOwner(_eventBus, ffScaling));
            }
            _combatBootstrap.Initialize(_eventBus, localSetup.CombatNetworkPort, localSetup.PlayerId, new EntityAffiliationAdapter(), ffScaling);
            if (_waveBootstrap == null)
            {
                var gameEndHandler = new GameEndEventHandler(_eventBus, _eventBus, localSetup.PlayerId);
                _disposables.Add(EventBusSubscription.ForOwner(_eventBus, gameEndHandler));
            }

            ConnectPlayer(localSetup);

            _manaRegenTicker.Initialize(localSetup.ManaAdapterInstance);
            _manaBarView.Initialize(_eventBus, localSetup.PlayerId, localSetup.MaxMana);
            _bleedoutTicker.Initialize(localSetup.BleedoutTrackerInstance);
            _rescueChannelTicker.Initialize(localSetup.RescueChannelTrackerInstance, localSetup.UseCases);
            _downedOverlayView.Initialize(_eventBus, localSetup.PlayerId, localSetup.BleedoutTrackerInstance, localSetup.RescueChannelTrackerInstance);
            _invulnerabilityTicker.Initialize(localSetup.InvulnerabilityTrackerInstance);

            _soundPlayer.Initialize(_eventBus, localSetup.PlayerId.Value);

            // ProjectileSpawner, ZoneSetup은 EventBus만 필요 → 선택 전에 초기화
            // 선택 UI 중에도 원격 스킬 이벤트(ProjectileRequestedEvent, ZoneRequestedEvent) 수신 가능
            _projectileSpawner.Initialize(_eventBus, _eventBus);
            _zoneSetup.Initialize(_eventBus);

            // 원격 플레이어 wiring도 선택 전에 완료 — Status RPC 유실 방지
            _remotePlayerWiringReady = true;
            while (_pendingRemotePlayers.Count > 0)
                ConnectPlayer(_pendingRemotePlayers.Dequeue());

            // 스킬 선택 시작 (WaveBootstrap은 SkillReward 필요 → 선택 완료 후 초기화)
            _skillSetup.InitializePreSelection(
                _eventBus, player.transform, _camera,
                localSetup.PlayerId, localSetup.ManaPort,
                _statusSetup.StatusQuery,
                onComplete: () =>
                {
                    if (_waveBootstrap != null)
                    {
                        _waveBootstrap.Initialize(_eventBus, _combatBootstrap, localSetup.PlayerId,
                            _skillSetup.SkillReward, _skillSetup.SkillIcon, _skillSetup.SkillUpgradeCommand);
                        _waveBootstrap.RegisterPlayer(player.transform);
                    }
                });
        }

        private void ConnectPlayer(PlayerSetup setup)
        {
            if (!setup.IsInitialized)
                setup.Initialize(_eventBus, playerLookup: _playerLookup);

            if (!_playerSceneRegistry.TryRegister(setup))
                return;

            var hudGo = Instantiate(_healthHudPrefab, _hudCanvas.transform, false);
            var hudView = hudGo.GetComponent<PlayerHealthHudView>();
            hudView.Initialize(_eventBus, setup.PlayerId, setup.MaxHp, setup.transform, _camera, _hudCanvas);

            _combatBootstrap.RegisterTarget(setup.PlayerId, setup.CombatTargetProvider);

            if (_waveBootstrap != null)
                _waveBootstrap.RegisterPlayer(setup.transform);

            // Wire remote player's StatusNetworkAdapter so RPC callbacks reach the shared handler
            if (!setup.NetworkAdapter.IsMine)
                _statusSetup.RegisterRemoteCallbackPort(setup.StatusNetworkAdapter);

            // Hydrate remote players from CustomProperties AFTER registry registration
            // so that IPlayerLookupPort.Resolve() can find the domain player
            if (!setup.NetworkAdapter.IsMine)
                setup.NetworkAdapter.HydrateFromProperties();
        }

        private void OnRemotePlayerArrived(PlayerSetup setup)
        {
            if (!_remotePlayerWiringReady)
            {
                _pendingRemotePlayers.Enqueue(setup);
                return;
            }

            ConnectPlayer(setup);
        }

        private void OnDestroy()
        {
            PlayerSetup.RemoteArrived -= OnRemotePlayerArrived;

            var playTime = Time.realtimeSinceStartup - _sceneStartTime;
            if (_analytics != null)
                _analytics.LogGameEnd(_matchId, playTime, RoundCounter.Current);

            _disposables?.Dispose();
        }

        public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
        {
            if (_dropOffLogged)
                return;
            _dropOffLogged = true;
            if (_analytics != null)
                _analytics.LogDropOff("game_disconnect", Time.realtimeSinceStartup - _sceneStartTime);
        }

        public override void OnLeftRoom()
        {
            if (_dropOffLogged)
                return;
            _dropOffLogged = true;
            if (_analytics != null)
                _analytics.LogDropOff("game_leave", Time.realtimeSinceStartup - _sceneStartTime);
        }
    }
}
