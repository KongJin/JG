using Shared.Attributes;
using System;
using System.Text;
using Features.Player.Application.Events;
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
        [SerializeField] private GameObject victoryOverlayVisual;
        [SerializeField] private GameObject defeatOverlayVisual;
        [SerializeField] private GameObject victoryReturnButton;
        [SerializeField] private GameObject defeatReturnButton;

        private bool _gameEnded;
        private Action _onReturnToLobbyRequested;

        public void Initialize(IEventSubscriber subscriber, Action onReturnToLobbyRequested)
        {
            _onReturnToLobbyRequested = onReturnToLobbyRequested;
            subscriber.Subscribe(this, new Action<WaveVictoryEvent>(OnVictory));
            subscriber.Subscribe(this, new Action<WaveDefeatEvent>(OnDefeat));
            subscriber.Subscribe(this, new Action<WaveHydratedEvent>(OnWaveHydrated));
            subscriber.Subscribe(this, new Action<GameEndReportRequestedEvent>(OnGameEndReport));

            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.gameObject.SetActive(false);
                returnToLobbyButton.onClick.AddListener(OnReturnToLobbyClicked);
            }

            AddReturnListener(victoryReturnButton);
            AddReturnListener(defeatReturnButton);
            SetOverlayActive(victoryOverlayVisual, false);
            SetOverlayActive(defeatOverlayVisual, false);
            panel.SetActive(false);
        }

        private void OnVictory(WaveVictoryEvent e)
        {
            if (_gameEnded) return;
            _gameEnded = true;
            Show("버텨냈다", isVictory: true);
        }

        private void OnDefeat(WaveDefeatEvent e)
        {
            if (_gameEnded) return;
            _gameEnded = true;
            Show("거점 붕괴", isVictory: false);
        }

        private void OnWaveHydrated(WaveHydratedEvent e)
        {
            switch (e.State)
            {
                case WaveState.Victory:
                    if (_gameEnded) return;
                    _gameEnded = true;
                    Show("버텨냈다", isVictory: true);
                    break;
                case WaveState.Defeat:
                    if (_gameEnded) return;
                    _gameEnded = true;
                    Show("거점 붕괴", isVictory: false);
                    break;
            }
        }

        private void OnGameEndReport(GameEndReportRequestedEvent e)
        {
            if (statsText != null)
            {
                var playTimeMin = e.PlayTimeSeconds / 60f;
                var playTimeSec = e.PlayTimeSeconds % 60f;
                var resultLabel = e.IsVictory ? "버텨냈다" : "거점 붕괴";
                var builder = new StringBuilder();
                builder.AppendLine($"결과: {resultLabel}");
                builder.AppendLine($"도달 공세: {e.ReachedWave}");
                builder.AppendLine($"작전 시간: {playTimeMin:F0}분 {playTimeSec:F0}초");

                if (e.CoreMaxHealth > 0f)
                {
                    var corePercent = Math.Max(0f, Math.Min(100f, e.CoreRemainingHealth / e.CoreMaxHealth * 100f));
                    builder.AppendLine($"거점 내구도: {corePercent:F0}%");
                }

                if (e.ContributionCards.Length > 0)
                {
                    builder.AppendLine();
                    builder.AppendLine("기여 카드");
                    for (var i = 0; i < e.ContributionCards.Length; i++)
                    {
                        var card = e.ContributionCards[i];
                        builder.AppendLine($"- {card.Title}: {card.Body}");
                    }
                }
                else
                {
                    builder.AppendLine();
                    builder.AppendLine($"기체 전개: {e.SummonCount}");
                    builder.AppendLine($"압박 정리: {e.UnitKillCount}");
                }

                statsText.text = builder.ToString();
            }
        }

        private void Show(string message, bool isVictory)
        {
            if (panel != null) panel.SetActive(true);
            if (resultText != null) resultText.text = message;

            if (statsText != null)
            {
                statsText.text = $"결과: {(isVictory ? "버텨냈다" : "거점 붕괴")}";
            }

            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.gameObject.SetActive(true);
            }

            SetOverlayActive(victoryOverlayVisual, isVictory);
            SetOverlayActive(defeatOverlayVisual, !isVictory);
        }

        private void AddReturnListener(GameObject buttonObject)
        {
            if (buttonObject == null)
                return;

            var button = buttonObject.GetComponent<Button>();
            if (button == null)
                return;

            button.onClick.AddListener(OnReturnToLobbyClicked);
        }

        private static void SetOverlayActive(GameObject overlay, bool active)
        {
            if (overlay != null)
                overlay.SetActive(active);
        }

        private void OnReturnToLobbyClicked()
        {
            _onReturnToLobbyRequested?.Invoke();
        }
    }
}
