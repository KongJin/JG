using Shared.Attributes;
using System;
using Features.Wave.Application.Events;
using Features.Wave.Domain;
using Shared.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Wave.Presentation
{
    /// <summary>
    /// Wave 승패 시 결과 패널을 표시한다.
    /// GameEndEvent 발행 책임은 WaveGameEndBridge(Application)에 있다.
    /// </summary>
    public sealed class WaveEndView : MonoBehaviour
    {
        [Required, SerializeField] private GameObject panel;
        [Required, SerializeField] private Text resultText;
        [SerializeField] private Text statsText;
        [SerializeField] private Button returnToLobbyButton;

        private bool _gameEnded;
        private Action _onReturnToLobbyRequested;

        public void Initialize(IEventSubscriber subscriber, Action onReturnToLobbyRequested)
        {
            _onReturnToLobbyRequested = onReturnToLobbyRequested;
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
            Show("Victory!", isVictory: true);
        }

        private void OnDefeat(WaveDefeatEvent e)
        {
            if (_gameEnded) return;
            _gameEnded = true;
            Show("Defeat!", isVictory: false);
        }

        private void OnWaveHydrated(WaveHydratedEvent e)
        {
            switch (e.State)
            {
                case WaveState.Victory:
                    if (_gameEnded) return;
                    _gameEnded = true;
                    Show("Victory!", isVictory: true);
                    break;
                case WaveState.Defeat:
                    if (_gameEnded) return;
                    _gameEnded = true;
                    Show("Defeat!", isVictory: false);
                    break;
            }
        }

        private void Show(string message, bool isVictory)
        {
            if (panel != null) panel.SetActive(true);
            if (resultText != null) resultText.text = message;

            if (statsText != null)
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
            _onReturnToLobbyRequested?.Invoke();
        }
    }
}
