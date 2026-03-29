using Features.Combat.Application.Ports;
using Features.Player.Application;
using Features.Player.Domain;
using Features.Player.Infrastructure;
using Features.Player.Presentation;
using Photon.Pun;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
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

        private PlayerUseCases _useCases;
        private PlayerCombatTargetProvider _combatTargetProvider;
        private DomainEntityId _playerId;

        public ICombatTargetProvider CombatTargetProvider => _combatTargetProvider;
        public ICombatNetworkCommandPort CombatNetworkPort { get; private set; }
        public DomainEntityId PlayerId => _playerId;
        public PlayerNetworkAdapter NetworkAdapter => _networkAdapter;
        public PlayerUseCases UseCases => _useCases;

        public float MaxHp { get; private set; }
        public bool IsInitialized { get; private set; }

        void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info)
        {
            if (info.photonView.IsMine)
                return;

            RemoteArrived?.Invoke(this);
        }

        public void Initialize(EventBus eventBus, PlayerUseCases existingUseCases = null)
        {
            if (IsInitialized)
                return;

            if (_networkAdapter.IsMine)
                InitializeLocal(eventBus, existingUseCases);
            else
                InitializeRemote(eventBus);
        }

        private void InitializeLocal(EventBus eventBus, PlayerUseCases existingUseCases)
        {
            var clock = new ClockAdapter();
            _useCases = existingUseCases != null
                ? existingUseCases
                : new PlayerUseCases(_motorAdapter, _networkAdapter, eventBus, clock);

            var spawnResult = _useCases.Spawn(
                new PlayerSpec(
                    walkSpeed: 5f,
                    sprintMultiplier: 1.8f,
                    jumpForce: 8f,
                    gravity: 20f,
                    maxHp: 100f,
                    defense: 5f,
                    rotationSpeed: 720f
                ),
                _networkAdapter.StablePlayerId
            );

            if (spawnResult.IsFailure)
            {
                Debug.LogError($"[PlayerSetup] Spawn failed: {spawnResult.Error}");
                return;
            }

            var player = spawnResult.Value;
            _playerId = player.Id;

            if (_entityIdHolder != null)
                _entityIdHolder.Set(player.Id);

            new PlayerNetworkEventHandler(eventBus, _networkAdapter);
            _combatTargetProvider = new PlayerCombatTargetProvider(player);
            CombatNetworkPort = new PlayerCombatNetworkPortAdapter(_networkAdapter);
            new PlayerDamageEventHandler(player, eventBus, eventBus);

            _inputHandler.Initialize(player, _useCases, eventBus);
            _view.Initialize(true, eventBus);
            MaxHp = player.MaxHp;
            IsInitialized = true;
        }

        private void InitializeRemote(EventBus eventBus)
        {
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
            var remotePlayer = new Domain.Player(_playerId, remoteSpec);
            new PlayerNetworkEventHandler(eventBus, _networkAdapter, remotePlayer);
            _combatTargetProvider = new PlayerCombatTargetProvider(remotePlayer);
            new PlayerDamageEventHandler(remotePlayer, eventBus, eventBus);
            MaxHp = remotePlayer.MaxHp;

            if (_entityIdHolder != null)
                _entityIdHolder.Set(_playerId);

            _inputHandler.enabled = false;
            _motorAdapter.enabled = false;
            _view.Initialize(false, eventBus);
            IsInitialized = true;
        }
    }
}
