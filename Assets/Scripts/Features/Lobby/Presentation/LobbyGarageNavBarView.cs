using Features.Garage.Presentation.Theme;
using Shared.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Features.Lobby.Presentation
{
    public sealed class LobbyGarageNavBarView : MonoBehaviour
    {
        [Header("Buttons")]
        [Required, SerializeField] private Button _lobbyTabButton;
        [Required, SerializeField] private Button _garageTabButton;

        [Header("Labels")]
        [Required, SerializeField] private TMP_Text _lobbyTabText;
        [Required, SerializeField] private TMP_Text _garageTabText;

        [Header("Active Indicators")]
        [Required, SerializeField] private Image _lobbyTabBorder;
        [Required, SerializeField] private Image _garageTabBorder;

        [Header("Visuals")]
        [SerializeField] private Color _activeTabColor = new(0.286f, 0.463f, 1f, 1f);
        [SerializeField] private Color _inactiveTabColor = new(0.086f, 0.157f, 0.196f, 1f);
        [SerializeField] private Color _activeTextColor = Color.white;
        [SerializeField] private Color _inactiveTextColor = new(0.545f, 0.584f, 0.651f, 1f);

        private UnityAction _openLobbyAction;
        private UnityAction _openGarageAction;

        public void Bind(System.Action onLobbySelected, System.Action onGarageSelected)
        {
            if (_openLobbyAction != null)
                _lobbyTabButton.onClick.RemoveListener(_openLobbyAction);
            if (_openGarageAction != null)
                _garageTabButton.onClick.RemoveListener(_openGarageAction);

            _openLobbyAction = () => onLobbySelected?.Invoke();
            _openGarageAction = () => onGarageSelected?.Invoke();

            _lobbyTabButton.onClick.AddListener(_openLobbyAction);
            _garageTabButton.onClick.AddListener(_openGarageAction);
        }

        public void SetState(bool lobbyActive)
        {
            ApplyState(_lobbyTabButton, _lobbyTabText, _lobbyTabBorder, lobbyActive);
            ApplyState(_garageTabButton, _garageTabText, _garageTabBorder, !lobbyActive);
        }

        private static Color GetIndicatorColor(bool isActive)
        {
            return isActive ? ThemeColors.AccentBlue : Color.clear;
        }

        private void ApplyState(Button button, TMP_Text label, Image border, bool isActive)
        {
            if (button != null)
            {
                button.interactable = !isActive;

                if (button.targetGraphic is Image background)
                    background.color = isActive ? _activeTabColor : _inactiveTabColor;
            }

            if (label != null)
                label.color = isActive ? _activeTextColor : _inactiveTextColor;

            if (border != null)
            {
                border.enabled = isActive;
                border.color = GetIndicatorColor(isActive);
            }
        }
    }
}
