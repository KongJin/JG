using Features.Combat.Application.Ports;
using Features.Player.Application;
using Features.Player.Domain;
using Features.Player.Infrastructure;
using Features.Player.Presentation;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Time;
using UnityEngine;

namespace Features.Player
{
    public sealed class PlayerSetup : MonoBehaviour
    {
        [SerializeField]
        private PlayerNetworkAdapter _networkAdapter;

        [SerializeField]
        private PlayerMotorAdapter _motorAdapter;

        [SerializeField]
        private PlayerInputHandler _inputHandler;

        [SerializeField]
        private PlayerView _view;

        [SerializeField]
        private EntityIdHolder _entityIdHolder;

        private PlayerUseCases _useCases;
        private PlayerCombatTargetProvider _combatTargetProvider;
        private DomainEntityId _playerId;

        public ICombatTargetProvider CombatTargetProvider => _combatTargetProvider;
        public DomainEntityId PlayerId => _playerId;
        public PlayerNetworkAdapter NetworkAdapter => _networkAdapter;
        public PlayerUseCases UseCases => _useCases;

        public float MaxHp { get; private set; }

        public void Initialize(EventBus eventBus, PlayerUseCases existingUseCases = null)
        {
            if (_networkAdapter == null)
            {
                Debug.LogError("[PlayerSetup] PlayerNetworkAdapter is missing.");
                return;
            }

            if (_networkAdapter.IsMine)
                InitializeLocal(eventBus, existingUseCases);
            else
                InitializeRemote(eventBus);
        }

        private void InitializeLocal(EventBus eventBus, PlayerUseCases existingUseCases)
        {
            if (_motorAdapter == null)
            {
                Debug.LogError("[PlayerSetup] PlayerMotorAdapter is missing.");
                return;
            }

            var clock = new ClockAdapter();

            if (existingUseCases != null)
            {
                _useCases = existingUseCases;
            }
            else
            {
                _useCases = new PlayerUseCases(_motorAdapter, _networkAdapter, eventBus, clock);
            }

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
            new PlayerDamageEventHandler(player, eventBus, eventBus);

            if (_inputHandler == null)
            {
                Debug.LogError(
                    "[PlayerSetup] PlayerInputHandler is not assigned in Inspector.",
                    this
                );
                return;
            }

            _inputHandler.Initialize(player, _useCases, eventBus);

            if (_view == null)
            {
                Debug.LogError("[PlayerSetup] PlayerView is not assigned in Inspector.", this);
                return;
            }

            _view.Initialize(true, eventBus);
            MaxHp = player.MaxHp;
        }

        private void InitializeRemote(EventBus eventBus)
        {
            // 리모트 플레이어를 위한 경량 도메인 엔터티 생성 (CombatTarget용)
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

            if (_inputHandler == null)
                Debug.LogError(
                    "[PlayerSetup] PlayerInputHandler is not assigned in Inspector.",
                    this
                );
            else
                _inputHandler.enabled = false;

            if (_motorAdapter == null)
                Debug.LogError(
                    "[PlayerSetup] PlayerMotorAdapter is not assigned in Inspector.",
                    this
                );
            else
                _motorAdapter.enabled = false;

            if (_view == null)
            {
                Debug.LogError("[PlayerSetup] PlayerView is not assigned in Inspector.", this);
                return;
            }

            _view.Initialize(false, eventBus);
        }
    }
}
