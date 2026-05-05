using Shared.Attributes;
using Features.Player.Application;
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

        private InputAction _moveAction;

        private bool _inputDisabled;

        public void Initialize(
            Domain.Player player,
            PlayerUseCases useCases
        )
        {
            _player = player;
            _useCases = useCases;

            _moveAction = _inputActions.FindAction("Move");
// csharp-guardrails: allow-null-defense
            _moveAction?.Enable();
        }

        private void OnDestroy()
        {
            DisableInput();
        }

        private void Update()
        {
// csharp-guardrails: allow-null-defense
            if (_player == null || _moveAction == null || _inputDisabled)
                return;

            var raw = _moveAction.ReadValue<Vector2>();
            var input = new Float2(raw.x, raw.y);
            _useCases.Move(_player, input, Time.deltaTime);
        }

        public void EnableInput()
        {
            _inputDisabled = false;
// csharp-guardrails: allow-null-defense
            _moveAction?.Enable();
        }

        public void DisableInput()
        {
            _inputDisabled = true;
// csharp-guardrails: allow-null-defense
            if (_moveAction != null)
                _moveAction.Disable();
        }
    }
}
