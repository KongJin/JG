using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Features.Lobby.Presentation
{
    internal sealed class LobbyUitkClickScope : IDisposable
    {
        private readonly List<ButtonClickBinding> _bindings = new();
        private bool _isDisposed;

        public void Register(Button button, Action callback)
        {
            if (_isDisposed || button == null || callback == null)
                return;

            button.clicked += callback;
            _bindings.Add(new ButtonClickBinding(button, callback));
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            for (int i = 0; i < _bindings.Count; i++)
                _bindings[i].Unbind();

            _bindings.Clear();
        }

        private readonly struct ButtonClickBinding
        {
            public ButtonClickBinding(Button button, Action callback)
            {
                Button = button;
                Callback = callback;
            }

            private Button Button { get; }
            private Action Callback { get; }

            public void Unbind()
            {
                if (Button != null && Callback != null)
                    Button.clicked -= Callback;
            }
        }
    }
}
