using Features.Combat;
using Features.Player.Application;
using Features.Player.Presentation;
using Features.Projectile;
using Features.Skill;
using Features.Zone;
using Photon.Pun;
using Shared.Analytics;
using Shared.ErrorHandling;
using Shared.EventBus;
using Shared.Ui;
using UnityEngine;

namespace Features.Player
{
    public sealed class GameSceneBootstrap : MonoBehaviourPunCallbacks
    {
        [Header("Player")]
        [SerializeField]
        private string _playerPrefabName = "PlayerCharacter";

        [SerializeField]
        private float _spawnRadius = 3f;

        [SerializeField]
        private Transform _cam;

        [SerializeField]
        private GameObject _healthHudPrefab;

        [SerializeField]
        private Canvas _hudCanvas;

        [SerializeField]
        private SkillSetup _skillSetup;

        [SerializeField]
        private ProjectileSpawner _projectileSpawner;

        [SerializeField]
        private CombatBootstrap _combatBootstrap;

        [SerializeField]
        private ZoneSetup _zoneSetup;

        [SerializeField]
        private SceneErrorPresenter _sceneErrorPresenter;

        private EventBus _eventBus;
        private IAnalyticsPort _analytics;
        private GameAnalyticsEventHandler _analyticsHandler;
        private string _matchId;
        private float _sceneStartTime;
        private bool _dropOffLogged;
        private PlayerSetup _localPlayerSetup;

        private void Start()
        {
            _eventBus = new EventBus();
            _analytics = new FirebaseAnalyticsAdapter();
            _matchId = PhotonNetwork.CurrentRoom?.Name ?? "unknown";
            _sceneStartTime = Time.realtimeSinceStartup;
            _analytics.LogGameStart(_matchId);
            RoundCounter.Increment();
            _analyticsHandler = new GameAnalyticsEventHandler(_analytics, _eventBus);

            if (_sceneErrorPresenter == null)
            {
                Debug.LogError("[GameScene] SceneErrorPresenter reference is missing.", this);
                return;
            }

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

            if (_combatBootstrap == null)
            {
                Debug.LogError("[GameScene] CombatBootstrap reference is missing.");
                return;
            }

            var offset = Random.insideUnitCircle * _spawnRadius;
            var spawnPosition = new Vector3(offset.x, 0f, offset.y);
            var player = PhotonNetwork.Instantiate(
                _playerPrefabName,
                spawnPosition,
                Quaternion.identity
            );
            var follower = _cam.GetComponent<CameraFollower>();
            if (follower == null)
                follower = _cam.gameObject.AddComponent<CameraFollower>();
            follower.Initialize(player.transform, _cam.position - player.transform.position);

            var localPlayerSetup = ConnectPlayer(player);
            var combatNetworkPort =
                localPlayerSetup != null && localPlayerSetup.NetworkAdapter != null
                    ? new Infrastructure.PlayerCombatNetworkPortAdapter(
                        localPlayerSetup.NetworkAdapter
                    )
                    : null;
            var localAuthorityId = localPlayerSetup != null ? localPlayerSetup.PlayerId : default;
            _combatBootstrap.Initialize(_eventBus, combatNetworkPort, localAuthorityId);
            RegisterCombatTarget(localPlayerSetup);

            if (_localPlayerSetup != null)
            {
                var camera = _cam.GetComponent<Camera>();
                _skillSetup.Initialize(
                    _eventBus,
                    player.transform,
                    camera,
                    _localPlayerSetup.PlayerId
                );
            }
            _projectileSpawner.Initialize(_eventBus, _eventBus);

            if (_zoneSetup == null)
            {
                Debug.LogError("[GameScene] ZoneSetup reference is missing.");
                return;
            }

            _zoneSetup.Initialize(_eventBus);

            foreach (var other in PhotonNetwork.PlayerListOthers)
                StartCoroutine(ConnectRemotePlayerDelayed(other));
        }

        private PlayerSetup ConnectPlayer(GameObject player)
        {
            var playerSetup = player.GetComponent<PlayerSetup>();
            if (playerSetup == null)
                return null;

            playerSetup.Initialize(_eventBus);

            if (_healthHudPrefab != null && _hudCanvas != null)
            {
                var hudGo = Instantiate(_healthHudPrefab, _hudCanvas.transform, false);
                var hudView = hudGo.GetComponent<PlayerHealthHudView>();
                if (hudView != null)
                {
                    var camera = _cam.GetComponent<Camera>();
                    hudView.Initialize(
                        _eventBus,
                        playerSetup.PlayerId,
                        playerSetup.MaxHp,
                        player.transform,
                        camera,
                        _hudCanvas
                    );
                }
            }
            else if (_healthHudPrefab != null)
            {
                Debug.LogError("[GameScene] Hud canvas reference is missing.", this);
            }

            if (playerSetup.UseCases != null)
            {
                _localPlayerSetup = playerSetup;
            }

            return playerSetup;
        }

        private void RegisterCombatTarget(PlayerSetup playerSetup)
        {
            if (playerSetup?.CombatTargetProvider == null)
                return;

            _combatBootstrap.RegisterTarget(playerSetup.PlayerId, playerSetup.CombatTargetProvider);
        }

        private void OnDestroy()
        {
            var playTime = Time.realtimeSinceStartup - _sceneStartTime;
            _analytics?.LogGameEnd(_matchId, playTime, RoundCounter.Current);
            if (_analyticsHandler != null)
                _eventBus?.UnsubscribeAll(_analyticsHandler);
        }

        public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
        {
            if (_dropOffLogged)
                return;
            _dropOffLogged = true;
            var elapsed = Time.realtimeSinceStartup - _sceneStartTime;
            _analytics?.LogDropOff("game_disconnect", elapsed);
        }

        public override void OnLeftRoom()
        {
            if (_dropOffLogged)
                return;
            _dropOffLogged = true;
            var elapsed = Time.realtimeSinceStartup - _sceneStartTime;
            _analytics?.LogDropOff("game_leave", elapsed);
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            StartCoroutine(ConnectRemotePlayerDelayed(newPlayer));
        }

        private System.Collections.IEnumerator ConnectRemotePlayerDelayed(
            Photon.Realtime.Player target
        )
        {
            for (var attempt = 0; attempt < 30; attempt++)
            {
                yield return null;
                foreach (var pv in FindObjectsByType<PhotonView>(FindObjectsSortMode.None))
                {
                    if (pv.Owner == target && pv.GetComponent<PlayerSetup>() != null)
                    {
                        RegisterCombatTarget(ConnectPlayer(pv.gameObject));
                        yield break;
                    }
                }
            }

            Debug.LogWarning($"[GameScene] Could not find PlayerSetup for player {target.NickName} after 30 frames.");
        }
    }
}
