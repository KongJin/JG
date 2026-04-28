using UnityEngine;

namespace Features.Lobby.Presentation
{
    public sealed class LobbyGarageNavBarView : MonoBehaviour
    {
        private System.Action _onLobbySelected;
        private System.Action _onGarageSelected;

        public void Bind(System.Action onLobbySelected, System.Action onGarageSelected)
        {
            _onLobbySelected = onLobbySelected;
            _onGarageSelected = onGarageSelected;
        }

        public void SetState(bool lobbyActive) { }
        public void SelectLobby() => _onLobbySelected?.Invoke();
        public void SelectGarage() => _onGarageSelected?.Invoke();
    }
}
