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

        private InputAction _moveAction;
        private InputAction _jumpAction;

        public void Initialize(
            Domain.Player player,
            PlayerUseCases useCases,
            IEventSubscriber eventBus
        )
        {
            _player = player;
            _useCases = useCases;
            _eventBus = eventBus;

            _moveAction = _inputActions.FindAction("Move");
            _jumpAction = _inputActions.FindAction("Jump");

            _moveAction.Enable();
            _jumpAction.Enable();

            _jumpAction.performed += OnJump;
            _eventBus?.Subscribe(this, new System.Action<PlayerDiedEvent>(OnPlayerDied));
        }

        private void OnDestroy()
        {
            DisableInput();
            _eventBus?.UnsubscribeAll(this);
        }

        private void Update()
        {
            if (_player == null || _moveAction == null)
                return;

            var raw = _moveAction.ReadValue<Vector2>();
            var input = new Float2(raw.x, raw.y);
            _useCases.Move(_player, input, Time.deltaTime);
        }

        private void OnJump(InputAction.CallbackContext ctx)
        {
            if (_player == null)
                return;
            _useCases.Jump(_player);
        }

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            if (_player == null || e.PlayerId != _player.Id)
                return;

            DisableInput();
            enabled = false;
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
        }
    }
}
