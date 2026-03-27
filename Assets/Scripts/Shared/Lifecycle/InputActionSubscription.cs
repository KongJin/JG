using System;
using UnityEngine.InputSystem;

namespace Shared.Lifecycle
{
    public static class InputActionSubscription
    {
        public static DelegateDisposable BindPerformed(
            InputAction action,
            Action<InputAction.CallbackContext> handler
        )
        {
            if (action == null || handler == null)
                return new DelegateDisposable(null);

            action.Enable();
            action.performed += handler;

            return new DelegateDisposable(() =>
            {
                action.performed -= handler;
                action.Disable();
            });
        }
    }
}
