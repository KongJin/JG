using Features.Combat.Application.Ports;
using Features.Player.Application;
using Features.Player.Application.Ports;
using Features.Player.Domain;
using Features.Player.Infrastructure;
using Features.Player.Presentation;
using Features.Status.Infrastructure;
using Photon.Pun;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Lifecycle;
using Shared.Time;
using UnityEngine;


namespace Features.Player
{
    public sealed class PlayerSetup : MonoBehaviour, IPunInstantiateMagicCallback
    {
        public static event System.Action<PlayerSetup> RemoteArrived;
        public static event System.Action<PlayerSetup> LocalArrived;

        [Required, SerializeField]
        private PlayerNetworkAdapter _networkAdapter;

        [Required, SerializeField]
        private PlayerMotorAdapter _motorAdapter;

        [Required, SerializeField]
        private PlayerInputHandler _inputHandler;

        [Required, SerializeField]
        private PlayerView _view;

        [Required, SerializeField]
        private EntityIdHolder _entityIdHolder;

        [Required, SerializeField]
        private StatusNetworkAdapter _statusNetworkAdapter;

        [Header("Energy")]
        [Tooltip("Energy 재생 곡선 설정.")]
        [SerializeField] private EnergyRegenCurveConfig _regenCurveConfig;

        private PlayerUseCases _useCases;
        private PlayerCombatTargetProvider _combatTargetProvider;
        private DomainEntityId _playerId;
        private DisposableScope _disposables;

        public ICombatTargetProvider CombatTargetProvider => _combatTargetProvider;
        public ICombatNetworkCommandPort CombatNetworkPort { get; private set; }
        public DomainEntityId PlayerId => _playerId;
        public PlayerNetworkAdapter NetworkAdapter => _networkAdapter;
        public StatusNetworkAdapter StatusNetworkAdapter => _statusNetworkAdapter;
        public PlayerUseCases UseCases => _useCases;
        public IEnergyPort EnergyPort { get; private set; }
        public EnergyAdapter EnergyAdapterInstance { get; private set; }
        public Domain.Player DomainPlayer { get; private set; }

        public float MaxHp { get; private set; }
        public float MaxEnergy { get; private set; }
        public bool IsInitialized { get; private set; }

        void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info)
        {
            if (info.photonView.IsMine)
            {
                LocalArrived?.Invoke(this);
                return;
            }

            RemoteArrived?.Invoke(this);
        }

        /// <summary>
        /// 로컬 플레이어 초기화.
        /// </summary>
        public void InitializeLocal(
            EventBus eventBus,
            IPlayerSpecProvider specProvider,
            ISpeedModifierPort speedModifier,
            PlayerSceneRegistry sceneRegistry,
            IPlayerLookupPort playerLookup)
        {
            if (IsInitialized)
                return;

            _disposables = new DisposableScope();

            var clock = new ClockAdapter();
            _useCases = new PlayerUseCases(_motorAdapter, _networkAdapter, eventBus, clock);

            var spec = specProvider.GetLocalPlayerSpec();
            var spawnResult = _useCases.Spawn(spec, _networkAdapter.StablePlayerId);

            if (spawnResult.IsFailure)
            {
                Debug.LogError($"[PlayerSetup] Spawn failed: {spawnResult.Error}");
                return;
            }

            var player = spawnResult.Value;
            DomainPlayer = player;
            _playerId = player.Id;

            _entityIdHolder.Set(player.Id);

            EnergyAdapterInstance = new EnergyAdapter(player, _networkAdapter, eventBus, _regenCurveConfig.ToCurve(), Time.time);
            EnergyPort = EnergyAdapterInstance;

            new PlayerNetworkEventHandler(eventBus, _networkAdapter, playerLookup);
            _combatTargetProvider = new PlayerCombatTargetProvider(player);
            CombatNetworkPort = new PlayerCombatNetworkPortAdapter(_networkAdapter);
            var damageHandler = new PlayerDamageEventHandler(player, eventBus, eventBus);

            _disposables.Add(EventBusSubscription.ForOwner(eventBus, _useCases));
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, damageHandler));

            _inputHandler.Initialize(player, _useCases);
            _view.Initialize(true, eventBus, player.Id);
            MaxHp = player.MaxHp;
            MaxEnergy = player.MaxEnergy;
            IsInitialized = true;
        }

        /// <summary>
        /// 원격 플레이어 초기화.
        /// </summary>
        public void InitializeRemote(
            EventBus eventBus,
            IPlayerSpecProvider specProvider,
            IPlayerLookupPort playerLookup)
        {
            if (IsInitialized)
                return;

            _disposables = new DisposableScope();

            _playerId = _networkAdapter.StablePlayerId;
            var spec = specProvider.GetRemotePlayerSpec();
            var remotePlayer = PlayerUseCases.SpawnRemote(spec, _playerId).Value;
            DomainPlayer = remotePlayer;
            new PlayerNetworkEventHandler(eventBus, _networkAdapter, playerLookup);
            _combatTargetProvider = new PlayerCombatTargetProvider(remotePlayer);
            var damageHandler = new PlayerDamageEventHandler(remotePlayer, eventBus, eventBus);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, damageHandler));
            MaxHp = remotePlayer.MaxHp;

            _entityIdHolder.Set(_playerId);

            _inputHandler.enabled = false;
            _motorAdapter.enabled = false;
            _view.Initialize(false, eventBus);
            IsInitialized = true;
        }

        private void OnDestroy()
        {
            _disposables?.Dispose();
        }
    }
}
