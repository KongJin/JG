using Shared.Attributes;
using System;
using Features.Player.Application.Events;
using Features.Wave.Application.Events;
using Features.Wave.Domain;
using Photon.Pun;
using Shared.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Wave.Presentation
{
    public sealed class WaveEndView : MonoBehaviour
    {
        [Required, SerializeField] private GameObject panel;
        [Required, SerializeField] private Text resultText;
        [SerializeField] private Text statsText;
        [SerializeField] private Button returnToLobbyButton;

        private IEventPublisher _publisher;
        private bool _gameEnded;

        public void Initialize(IEventSubscriber subscriber, IEventPublisher publisher)
        {
            _publisher = publisher;

            subscriber.Subscribe(this, new Action<WaveVictoryEvent>(OnVictory));
            subscriber.Subscribe(this, new Action<WaveDefeatEvent>(OnDefeat));
            subscriber.Subscribe(this, new Action<WaveHydratedEvent>(OnWaveHydrated));

            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.gameObject.SetActive(false);
                returnToLobbyButton.onClick.AddListener(OnReturnToLobbyClicked);
            }

            panel.SetActive(false);
        }

        private void OnVictory(WaveVictoryEvent e)
        {
            if (_gameEnded) return;
            _gameEnded = true;

            Show("Victory!", buildStats: true, isVictory: true);
            _publisher.Publish(new GameEndEvent(
                isVictory: true,
                message: "Victory!"));
        }

        private void OnDefeat(WaveDefeatEvent e)
        {
            if (_gameEnded) return;
            _gameEnded = true;

            Show("Defeat!", buildStats: true, isVictory: false);
            _publisher.Publish(new GameEndEvent(
                isVictory: false,
                message: "Defeat!"));
        }

        private void OnWaveHydrated(WaveHydratedEvent e)
        {
            switch (e.State)
            {
                case WaveState.Victory:
                    if (_gameEnded) return;
                    _gameEnded = true;
                    Show("Victory!", buildStats: true, isVictory: true);
                    _publisher.Publish(new GameEndEvent(
                        isVictory: true,
                        message: "Victory!"));
                    break;
                case WaveState.Defeat:
                    if (_gameEnded) return;
                    _gameEnded = true;
                    Show("Defeat!", buildStats: true, isVictory: false);
                    _publisher.Publish(new GameEndEvent(
                        isVictory: false,
                        message: "Defeat!"));
                    break;
            }
        }

        private void Show(string message, bool buildStats, bool isVictory)
        {
            if (panel != null) panel.SetActive(true);
            if (resultText != null) resultText.text = message;

            if (buildStats && statsText != null)
            {
                statsText.text = $"결과: {(isVictory ? "승리" : "패배")}";
            }

            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.gameObject.SetActive(true);
            }
        }

        private void OnReturnToLobbyClicked()
        {
            // TODO: SceneLoaderPort를 통해 Lobby 씬으로 전환
            // 임시: PhotonNetwork.LeaveRoom()
            if (Photon.Pun.PhotonNetwork.InRoom)
            {
                Photon.Pun.PhotonNetwork.LeaveRoom();
            }
        }
    }
}
