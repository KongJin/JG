using System;
using System.Text;
using Features.Player.Application.Events;
using Features.Wave.Application.Events;
using Features.Wave.Domain;
using Shared.EventBus;
using UnityEngine;

namespace Features.Wave.Presentation
{
    public sealed class WaveEndView : MonoBehaviour
    {
        private bool _gameEnded;
        private Action _onReturnToLobbyRequested;

        public bool IsPanelVisible { get; private set; }
        public bool IsVictory { get; private set; }
        public string ResultText { get; private set; } = string.Empty;
        public string StatsText { get; private set; } = string.Empty;

        public void Initialize(IEventSubscriber subscriber, Action onReturnToLobbyRequested)
        {
            _onReturnToLobbyRequested = onReturnToLobbyRequested;
            subscriber.Subscribe(this, new Action<WaveVictoryEvent>(OnVictory));
            subscriber.Subscribe(this, new Action<WaveDefeatEvent>(OnDefeat));
            subscriber.Subscribe(this, new Action<WaveHydratedEvent>(OnWaveHydrated));
            subscriber.Subscribe(this, new Action<GameEndReportRequestedEvent>(OnGameEndReport));
            IsPanelVisible = false;
        }

        private void OnVictory(WaveVictoryEvent e)
        {
            if (_gameEnded)
                return;

            _gameEnded = true;
            Show("버텨냈다", isVictory: true);
        }

        private void OnDefeat(WaveDefeatEvent e)
        {
            if (_gameEnded)
                return;

            _gameEnded = true;
            Show("거점 붕괴", isVictory: false);
        }

        private void OnWaveHydrated(WaveHydratedEvent e)
        {
            switch (e.State)
            {
                case WaveState.Victory:
                    if (!_gameEnded)
                    {
                        _gameEnded = true;
                        Show("버텨냈다", isVictory: true);
                    }
                    break;
                case WaveState.Defeat:
                    if (!_gameEnded)
                    {
                        _gameEnded = true;
                        Show("거점 붕괴", isVictory: false);
                    }
                    break;
            }
        }

        private void OnGameEndReport(GameEndReportRequestedEvent e)
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

            StatsText = builder.ToString();
        }

        private void Show(string message, bool isVictory)
        {
            IsPanelVisible = true;
            IsVictory = isVictory;
            ResultText = message;
            StatsText = $"결과: {(isVictory ? "버텨냈다" : "거점 붕괴")}";
        }

        public void ReturnToLobby()
        {
            _onReturnToLobbyRequested?.Invoke();
        }
    }
}
