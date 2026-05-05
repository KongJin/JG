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

    internal sealed class LobbyAuxiliarySurfaceBinder
    {
        private readonly VisualElement _recordsPage;
        private readonly VisualElement _accountPage;
        private readonly VisualElement _connectionPage;
        private readonly VisualTreeAsset _operationMemoryTree;
        private readonly VisualTreeAsset _accountSyncTree;
        private readonly VisualTreeAsset _connectionReconnectTree;
        private readonly Action<VisualElement, string, string, Action> _registerClick;
        private readonly Action _requestLobby;
        private readonly Action _requestGarage;
        private readonly Action _requestAccountRefresh;
        private readonly Action _requestConnection;

        public LobbyAuxiliarySurfaceBinder(
            VisualElement recordsPage,
            VisualElement accountPage,
            VisualElement connectionPage,
            VisualTreeAsset operationMemoryTree,
            VisualTreeAsset accountSyncTree,
            VisualTreeAsset connectionReconnectTree,
            Action<VisualElement, string, string, Action> registerClick,
            Action requestLobby,
            Action requestGarage,
            Action requestAccountRefresh,
            Action requestConnection)
        {
            _recordsPage = recordsPage;
            _accountPage = accountPage;
            _connectionPage = connectionPage;
            _operationMemoryTree = operationMemoryTree;
            _accountSyncTree = accountSyncTree;
            _connectionReconnectTree = connectionReconnectTree;
            _registerClick = registerClick;
            _requestLobby = requestLobby;
            _requestGarage = requestGarage;
            _requestAccountRefresh = requestAccountRefresh;
            _requestConnection = requestConnection;
        }

        public void EnsureRecords()
        {
            if (_recordsPage == null || _recordsPage.childCount > 0 || _operationMemoryTree == null)
                return;

            _operationMemoryTree.CloneTree(_recordsPage);
            _registerClick(_recordsPage, "BackButton", "ui_back", _requestLobby);
            _registerClick(_recordsPage, "GarageButton", "ui_select", _requestGarage);
        }

        public void EnsureAccount()
        {
            if (_accountPage == null || _accountPage.childCount > 0 || _accountSyncTree == null)
                return;

            _accountSyncTree.CloneTree(_accountPage);
            _registerClick(_accountPage, "ManualSyncRetryButton", "ui_retry", _requestAccountRefresh);
            _registerClick(_accountPage, "LinkAccountButton", "ui_click", _requestConnection);
        }

        public void EnsureConnection()
        {
            if (_connectionPage == null || _connectionPage.childCount > 0 || _connectionReconnectTree == null)
                return;

            _connectionReconnectTree.CloneTree(_connectionPage);
            _registerClick(_connectionPage, "BackButton", "ui_back", _requestLobby);
            _registerClick(_connectionPage, "ReturnLobbyButton", "ui_back", _requestLobby);
            _registerClick(_connectionPage, "ManualRetryButton", "ui_retry", _requestLobby);
        }
    }
}
