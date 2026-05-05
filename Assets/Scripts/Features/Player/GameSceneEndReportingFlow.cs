using Features.Lobby.Infrastructure;
using Features.Player.Application;
using Features.Player.Application.Events;
using Features.Player.Infrastructure;
using Shared.Analytics;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Lifecycle;
using UnityEngine;

namespace Features.Player
{
    internal sealed class GameSceneEndReportingFlow
    {
        private IAnalyticsPort _analytics;
        private EventBus _eventBus;
        private string _matchId = "unknown";
        private string _lobbySceneName = "LobbyScene";
        private float _sceneStartTime;
        private bool _dropOffLogged;
        private bool _sessionStarted;
        private bool _endHandlersRegistered;

        public void StartSession(
            EventBus eventBus,
            DisposableScope disposables,
            string matchId,
            float sceneStartTime,
            string lobbySceneName)
        {
            if (_sessionStarted)
                return;

            _eventBus = eventBus;
            _matchId = string.IsNullOrWhiteSpace(matchId) ? "unknown" : matchId;
            _sceneStartTime = sceneStartTime;
            _lobbySceneName = string.IsNullOrWhiteSpace(lobbySceneName) ? "LobbyScene" : lobbySceneName;
            _dropOffLogged = false;
            _sessionStarted = true;

            _analytics = new FirebaseAnalyticsAdapter();
            _analytics.LogGameStart(_matchId);
            RoundCounter.Increment();

            var analyticsHandler = new GameAnalyticsEventHandler(
                _analytics,
                eventBus,
                sceneStartTime,
                () => Time.realtimeSinceStartup);
            disposables.Add(EventBusSubscription.ForOwner(eventBus, analyticsHandler));
        }

        public void RegisterEndHandlers(
            DisposableScope disposables,
            DomainEntityId coreId,
            float coreMaxHp)
        {
// csharp-guardrails: allow-null-defense
            if (_endHandlersRegistered || _eventBus == null)
                return;

            _endHandlersRegistered = true;

            var gameEndHandler = new GameEndEventHandler(_eventBus);
            disposables.Add(EventBusSubscription.ForOwner(_eventBus, gameEndHandler));

            var gameEndAnalytics = new GameEndAnalytics(_eventBus, _eventBus, coreId, coreMaxHp);
            disposables.Add(EventBusSubscription.ForOwner(_eventBus, gameEndAnalytics));

            var operationRecordHandler = new OperationRecordGameEndHandler(
                _eventBus,
                new SaveOperationRecordUseCase(new OperationRecordJsonStore()),
                logWarning: Debug.LogWarning);
            disposables.Add(EventBusSubscription.ForOwner(_eventBus, operationRecordHandler));

            _eventBus.Subscribe(this, new System.Action<GameEndReportRequestedEvent>(OnGameEndReport));
            disposables.Add(EventBusSubscription.ForOwner(_eventBus, this));
        }

        public void Dispose()
        {
            if (!_sessionStarted)
                return;

            var playTime = Time.realtimeSinceStartup - _sceneStartTime;
            // csharp-guardrails: allow-null-defense
            _analytics?.LogGameEnd(_matchId, playTime, RoundCounter.Current);
            _analytics = null;
            _eventBus = null;
            _sessionStarted = false;
            _endHandlersRegistered = false;
        }

        public void HandleDisconnected()
        {
            LogDropOffOnce("game_disconnect");
        }

        public void HandleLeftRoom()
        {
            LogDropOffOnce("game_leave");
            new SceneLoaderAdapter().LoadScene(_lobbySceneName);
        }

        private void LogDropOffOnce(string context)
        {
            if (_dropOffLogged)
                return;

            _dropOffLogged = true;
// csharp-guardrails: allow-null-defense
            _analytics?.LogDropOff(context, Time.realtimeSinceStartup - _sceneStartTime);
        }

        private void OnGameEndReport(GameEndReportRequestedEvent e)
        {
            Debug.Log("[GameEnd] ===== Game Result =====");
            Debug.Log($"  Result:     {(e.IsVictory ? "Victory" : "Defeat")}");
            Debug.Log($"  Wave:       {e.ReachedWave}");
            Debug.Log($"  Play Time:  {e.PlayTimeSeconds:F1}s ({e.PlayTimeSeconds / 60f:F1}m)");
            Debug.Log($"  Summons:    {e.SummonCount}");
            Debug.Log($"  Unit Kills: {e.UnitKillCount}");
            if (e.CoreMaxHealth > 0f)
                Debug.Log($"  Core HP:    {e.CoreRemainingHealth:F0}/{e.CoreMaxHealth:F0}");

            for (var i = 0; i < e.ContributionCards.Length; i++)
            {
                var card = e.ContributionCards[i];
                Debug.Log($"  Card {i + 1}:   {card.Title} - {card.Body}");
            }

            Debug.Log("[GameEnd] =========================");
// csharp-guardrails: allow-null-defense
            _analytics?.LogGameResult(e.IsVictory, e.ReachedWave, e.PlayTimeSeconds, e.SummonCount, e.UnitKillCount);
        }
    }
}
