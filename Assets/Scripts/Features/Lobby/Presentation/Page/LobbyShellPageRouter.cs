using System;
using System.Collections.Generic;
using Shared.Ui;
using UnityEngine.UIElements;

namespace Features.Lobby.Presentation
{
    internal enum LobbyShellPageId
    {
        Lobby,
        Garage,
        Records,
        Account,
        Connection
    }

    internal sealed class LobbyShellPageRouter
    {
        private const string SelectedNavClass = "shared-nav-item--selected";

        private readonly IReadOnlyList<LobbyShellPageRoute> _routes;
        private readonly Label _shellTitle;
        private readonly Label _shellState;

        public LobbyShellPageRouter(
            IReadOnlyList<LobbyShellPageRoute> routes,
            Label shellTitle,
            Label shellState)
        {
            _routes = routes ?? Array.Empty<LobbyShellPageRoute>();
            _shellTitle = shellTitle;
            _shellState = shellState;
        }

        public void Show(LobbyShellPageId pageId)
        {
            var selectedRoute = FindRoute(pageId);
            selectedRoute.OnEnter?.Invoke();
            CurrentPageId = pageId;

            for (var i = 0; i < _routes.Count; i++)
            {
                var route = _routes[i];
                var isSelected = route.Id == pageId;
                UitkElementUtility.SetDisplay(route.Host, isSelected);
                UitkElementUtility.SetClass(route.NavButton, SelectedNavClass, isSelected);
            }

            if (_shellTitle != null)
                _shellTitle.text = selectedRoute.Title;
            if (_shellState != null)
                _shellState.text = selectedRoute.State;
        }

        public LobbyShellPageId? CurrentPageId { get; private set; }

        private LobbyShellPageRoute FindRoute(LobbyShellPageId pageId)
        {
            for (var i = 0; i < _routes.Count; i++)
            {
                if (_routes[i].Id == pageId)
                    return _routes[i];
            }

            throw new InvalidOperationException($"Lobby shell page route not found: {pageId}");
        }
    }

    internal readonly struct LobbyShellPageRoute
    {
        public LobbyShellPageRoute(
            LobbyShellPageId id,
            VisualElement host,
            Button navButton,
            string title,
            string state,
            Action onEnter = null)
        {
            Id = id;
            Host = host;
            NavButton = navButton;
            Title = title ?? string.Empty;
            State = state ?? string.Empty;
            OnEnter = onEnter;
        }

        public LobbyShellPageId Id { get; }
        public VisualElement Host { get; }
        public Button NavButton { get; }
        public string Title { get; }
        public string State { get; }
        public Action OnEnter { get; }
    }
}
