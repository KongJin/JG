using Features.Combat.Application.Ports;
using Features.Player.Application;
using Features.Player.Application.Ports;
using Features.Player.Domain;
using Features.Player.Infrastructure;
using Features.Player.Presentation;
using Features.Skill.Application.Ports;
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
        public IManaPort ManaPort { get; private set; }
        public ManaAdapter ManaAdapterInstance { get; private set; }
        public Domain.Player DomainPlayer { get; private set; }
        public BleedoutTracker BleedoutTrackerInstance { get; private set; }
        public RescueChannelTracker RescueChannelTrackerInstance { get; private set; }
        public InvulnerabilityTracker InvulnerabilityTrackerInstance { get; private set; }

        public float MaxHp { get; private set; }
        public float MaxMana { get; private set; }
        public bool IsInitialized { get; private set; }

        void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info)
        {
            if (info.photonView.IsMine)
                return;

            RemoteArrived?.Invoke(this);
        }

        public void Initialize(EventBus eventBus, PlayerUseCases existingUseCases = null, ISpeedModifierPort speedModifier = null, PlayerSceneRegistry sceneRegistry = null, IPlayerLookupPort playerLookup = null)
        {
            if (IsInitialized)
                return;

            if (_networkAdapter.IsMine)
                InitializeLocal(eventBus, existingUseCases, speedModifier, sceneRegistry, playerLookup);
            else
                InitializeRemote(eventBus, playerLookup);
        }

        private void InitializeLocal(EventBus eventBus, PlayerUseCases existingUseCases, ISpeedModifierPort speedModifier, PlayerSceneRegistry sceneRegistry, IPlayerLookupPort playerLookup)
        {
            _disposables = new DisposableScope();

            var clock = new ClockAdapter();
            _useCases = existingUseCases != null
                ? existingUseCases
                : new PlayerUseCases(_motorAdapter, _networkAdapter, eventBus, clock, speedModifier, eventBus, playerLookup);

            var spawnResult = _useCases.Spawn(
                new PlayerSpec(
                    walkSpeed: 5f,
                    sprintMultiplier: 1.8f,
                    jumpForce: 8f,
                    gravity: 20f,
                    maxHp: 100f,
                    defense: 5f,
                    rotationSpeed: 720f,
                    maxMana: 100f,
                    manaRegenPerSecond: 5f
                ),
                _networkAdapter.StablePlayerId
            );

            if (spawnResult.IsFailure)
            {
                Debug.LogError($"[PlayerSetup] Spawn failed: {spawnResult.Error}");
                return;
            }

            var player = spawnResult.Value;
            DomainPlayer = player;
            _playerId = player.Id;

            _entityIdHolder.Set(player.Id);

            ManaAdapterInstance = new ManaAdapter(player, _networkAdapter, eventBus);
            ManaPort = ManaAdapterInstance;

            new PlayerNetworkEventHandler(eventBus, _networkAdapter, playerLookup);
            _combatTargetProvider = new PlayerCombatTargetProvider(player);
            CombatNetworkPort = new PlayerCombatNetworkPortAdapter(_networkAdapter);
            var damageHandler = new PlayerDamageEventHandler(player, eventBus, eventBus);

            _disposables.Add(EventBusSubscription.ForOwner(eventBus, _useCases));
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, damageHandler));

            var bleedoutTracker = new BleedoutTracker(player, eventBus, eventBus, _networkAdapter);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, bleedoutTracker));
            BleedoutTrackerInstance = bleedoutTracker;

            RescueChannelTrackerInstance = new RescueChannelTracker(eventBus, eventBus, _networkAdapter);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, RescueChannelTrackerInstance));

            var invulnerabilityTracker = new InvulnerabilityTracker(player, eventBus);
            _disposables.Add(EventBusSubscription.ForOwner(eventBus, invulnerabilityTracker));
            InvulnerabilityTrackerInstance = invulnerabilityTracker;

            _inputHandler.Initialize(player, _useCases, eventBus, RescueChannelTrackerInstance);
            _view.Initialize(true, eventBus);
            MaxHp = player.MaxHp;
            MaxMana = player.MaxMana;
            IsInitialized = true;
        }

        private void InitializeRemote(EventBus eventBus, IPlayerLookupPort playerLookup)
        {
            _disposables = new DisposableScope();

            _playerId = _networkAdapter.StablePlayerId;
            var remoteSpec = new PlayerSpec(
                walkSpeed: 0f,
                sprintMultiplier: 1f,
                jumpForce: 0f,
                gravity: 0f,
                maxHp: 100f,
                defense: 5f,
                rotationSpeed: 0f
            );
            var remotePlayer = PlayerUseCases.SpawnRemote(remoteSpec, _playerId).Value;
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
