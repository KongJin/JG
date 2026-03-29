using System.Collections.Generic;
using Features.Combat;
using Features.Player.Application;
using Features.Player.Presentation;
using Features.Projectile;
using Features.Skill;
using Features.Wave;
using Features.Zone;
using Photon.Pun;
using Shared.Analytics;
using Shared.Attributes;
using Shared.ErrorHandling;
using Shared.EventBus;
using Shared.Kernel;
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

        [Header("Wave (PvE)")]
        [Required, SerializeField] private WaveBootstrap _waveBootstrap;

        private EventBus _eventBus;
        private IAnalyticsPort _analytics;
        private GameAnalyticsEventHandler _analyticsHandler;
        private GameEndEventHandler _gameEndHandler;
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

            // Analytics
            _analytics = new FirebaseAnalyticsAdapter();
            _matchId = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "unknown";
            _sceneStartTime = Time.realtimeSinceStartup;
            _analytics.LogGameStart(_matchId);
            RoundCounter.Increment();
            _analyticsHandler = new GameAnalyticsEventHandler(_analytics, _eventBus);

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
            localSetup.Initialize(_eventBus);

            // Combat
            _combatBootstrap.Initialize(_eventBus, localSetup.CombatNetworkPort, localSetup.PlayerId);
            if (_waveBootstrap == null)
                _gameEndHandler = new GameEndEventHandler(_eventBus, _eventBus, localSetup.PlayerId);

            ConnectPlayer(localSetup);

            _soundPlayer.Initialize(_eventBus, localSetup.PlayerId.Value);
            _skillSetup.Initialize(_eventBus, player.transform, _camera, localSetup.PlayerId);
            _projectileSpawner.Initialize(_eventBus, _eventBus);
            _zoneSetup.Initialize(_eventBus);

            // Wave (PvE)
            if (_waveBootstrap != null)
            {
                _waveBootstrap.Initialize(_eventBus, _combatBootstrap);
                _waveBootstrap.RegisterPlayer(player.transform);
            }

            _remotePlayerWiringReady = true;

            while (_pendingRemotePlayers.Count > 0)
                ConnectPlayer(_pendingRemotePlayers.Dequeue());
        }

        private void ConnectPlayer(PlayerSetup setup)
        {
            if (!setup.IsInitialized)
                setup.Initialize(_eventBus);

            if (!_playerSceneRegistry.TryRegister(setup))
                return;

            var hudGo = Instantiate(_healthHudPrefab, _hudCanvas.transform, false);
            var hudView = hudGo.GetComponent<PlayerHealthHudView>();
            if (hudView != null)
                hudView.Initialize(_eventBus, setup.PlayerId, setup.MaxHp, setup.transform, _camera, _hudCanvas);

            if (setup.CombatTargetProvider != null)
                _combatBootstrap.RegisterTarget(setup.PlayerId, setup.CombatTargetProvider);

            if (_waveBootstrap != null)
                _waveBootstrap.RegisterPlayer(setup.transform);
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
            if (_analyticsHandler != null && _eventBus != null)
                _eventBus.UnsubscribeAll(_analyticsHandler);
            if (_gameEndHandler != null && _eventBus != null)
                _eventBus.UnsubscribeAll(_gameEndHandler);
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
