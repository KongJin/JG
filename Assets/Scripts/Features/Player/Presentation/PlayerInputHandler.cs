using Shared.Attributes;
using Features.Player.Application;
using Features.Player.Application.Events;
using Shared.EventBus;
using Shared.Math;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Features.Player.Presentation
{
    public sealed class PlayerInputHandler : MonoBehaviour
    {
        [Required, SerializeField] private InputActionAsset _inputActions;

        private PlayerUseCases _useCases;
        private Domain.Player _player;
        private IEventSubscriber _eventBus;
        private RescueChannelTracker _channelTracker;

        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _interactAction;

        private bool _inputDisabled;

        public void Initialize(
            Domain.Player player,
            PlayerUseCases useCases,
            IEventSubscriber eventBus,
            RescueChannelTracker channelTracker = null
        )
        {
            _player = player;
            _useCases = useCases;
            _eventBus = eventBus;
            _channelTracker = channelTracker;

            _moveAction = _inputActions.FindAction("Move");
            _jumpAction = _inputActions.FindAction("Jump");
            _interactAction = _inputActions.FindAction("Interact");

            _moveAction.Enable();
            _jumpAction.Enable();

            _jumpAction.performed += OnJump;

            if (_interactAction != null)
            {
                _interactAction.Enable();
                _interactAction.started += OnInteractStarted;
                _interactAction.canceled += OnInteractCancelled;
            }

            _eventBus?.Subscribe(this, new System.Action<PlayerDownedEvent>(OnPlayerDowned));
            _eventBus?.Subscribe(this, new System.Action<PlayerDiedEvent>(OnPlayerDied));
            _eventBus?.Subscribe(this, new System.Action<PlayerRescuedEvent>(OnPlayerRescued));
            _eventBus?.Subscribe(this, new System.Action<PlayerRespawnedEvent>(OnPlayerRespawned));
        }

        private void OnDestroy()
        {
            DisableInput();
            _eventBus?.UnsubscribeAll(this);
        }

        private void Update()
        {
            if (_player == null || _moveAction == null || _inputDisabled)
                return;

            var raw = _moveAction.ReadValue<Vector2>();
            var input = new Float2(raw.x, raw.y);
            _useCases.Move(_player, input, Time.deltaTime);
        }

        private void OnJump(InputAction.CallbackContext ctx)
        {
            if (_player == null || _inputDisabled)
                return;
            _useCases.Jump(_player);
        }

        private void OnInteractStarted(InputAction.CallbackContext ctx)
        {
            if (_player == null || _inputDisabled || _channelTracker == null)
                return;

            var pos = transform.position;
            var targetId = _useCases.FindRescueTarget(new Float3(pos.x, pos.y, pos.z));
            if (string.IsNullOrWhiteSpace(targetId.Value))
                return;

            var result = _useCases.StartRescueChannel(_player.Id, targetId);
            if (result.IsSuccess)
                _channelTracker.Start(_player.Id, targetId);
        }

        private void OnInteractCancelled(InputAction.CallbackContext ctx)
        {
            if (_channelTracker == null || !_channelTracker.IsActive)
                return;

            _useCases.CancelRescueChannel(_channelTracker.TargetId);
            _channelTracker.Cancel();
        }

        private void OnPlayerDowned(PlayerDownedEvent e)
        {
            if (_player == null || e.PlayerId != _player.Id)
                return;

            DisableInput();
            _inputDisabled = true;
        }

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            if (_player == null || e.PlayerId != _player.Id)
                return;

            DisableInput();
            _inputDisabled = true;
        }

        private void OnPlayerRescued(PlayerRescuedEvent e)
        {
            if (_player == null || e.RescuedId != _player.Id)
                return;

            EnableInput();
        }

        private void OnPlayerRespawned(PlayerRespawnedEvent e)
        {
            if (_player == null || e.PlayerId != _player.Id)
                return;

            EnableInput();
        }

        private void EnableInput()
        {
            _inputDisabled = false;
            _moveAction?.Enable();
            _jumpAction?.Enable();
            _interactAction?.Enable();

            if (_jumpAction != null)
                _jumpAction.performed += OnJump;

            if (_interactAction != null)
            {
                _interactAction.started += OnInteractStarted;
                _interactAction.canceled += OnInteractCancelled;
            }
        }

        private void DisableInput()
        {
            if (_jumpAction != null)
            {
                _jumpAction.performed -= OnJump;
                _jumpAction.Disable();
            }

            if (_moveAction != null)
                _moveAction.Disable();

            if (_interactAction != null)
            {
                _interactAction.started -= OnInteractStarted;
                _interactAction.canceled -= OnInteractCancelled;
                _interactAction.Disable();
            }
        }
    }
}
