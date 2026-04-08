using Features.Enemy.Application.Events;
using Features.Player.Application.Events;
using Features.Unit.Application.Events;
using Shared.EventBus;
using System;
using UnityEngine;

namespace Features.Player.Application
{
    /// <summary>
    /// 게임 종료 시 전적/통계를 기록한다.
    /// 현재는 Debug.Log만 기록, 나중에는 Firebase/서버 전송으로 확장 가능.
    /// </summary>
    public sealed class GameEndAnalytics : IDisposable
    {
        private readonly System.Diagnostics.Stopwatch _playTimer;

        private int _summonCount;
        private int _unitKillCount;

        public GameEndAnalytics(IEventSubscriber subscriber)
        {
            _playTimer = System.Diagnostics.Stopwatch.StartNew();

            // 소환 이벤트 카운팅
            subscriber.Subscribe(this, new Action<UnitSummonCompletedEvent>(OnUnitSummoned));

            // 적 처치 이벤트 카운팅
            subscriber.Subscribe(this, new Action<EnemyDiedEvent>(OnEnemyDied));

            // 게임 종료 시 로그 기록
            subscriber.Subscribe(this, new Action<GameEndEvent>(OnGameEnd));
        }

        private void OnUnitSummoned(UnitSummonCompletedEvent e)
        {
            _summonCount++;
        }

        private void OnEnemyDied(EnemyDiedEvent e)
        {
            _unitKillCount++;
        }

        private void OnGameEnd(GameEndEvent e)
        {
            var playTime = _playTimer.Elapsed.TotalSeconds;
            var finalPlayTime = e.PlayTimeSeconds > 0f ? e.PlayTimeSeconds : playTime;

            LogAnalytics(
                isVictory: e.IsVictory,
                reachedWave: e.ReachedWave,
                playTimeSeconds: finalPlayTime,
                summonCount: e.SummonCount > 0 ? e.SummonCount : _summonCount,
                unitKillCount: e.UnitKillCount > 0 ? e.UnitKillCount : _unitKillCount
            );
        }

        private void LogAnalytics(bool isVictory, int reachedWave, float playTimeSeconds, int summonCount, int unitKillCount)
        {
            // TODO: Firebase Analytics / 서버 전송
            Debug.Log($"[GameEndAnalytics] ===== Game Result =====");
            Debug.Log($"  Result:     {(isVictory ? "Victory" : "Defeat")}");
            Debug.Log($"  Wave:       {reachedWave}");
            Debug.Log($"  Play Time:  {playTimeSeconds:F1}s ({playTimeSeconds / 60f:F1}m)");
            Debug.Log($"  Summons:    {summonCount}");
            Debug.Log($"  Unit Kills: {unitKillCount}");
            Debug.Log($"  K/D Ratio:  {(summonCount > 0 ? (float)unitKillCount / summonCount : 0):F2}");
            Debug.Log($"[GameEndAnalytics] =========================");
        }

        public void Dispose()
        {
            _playTimer?.Stop();
        }
    }
}
