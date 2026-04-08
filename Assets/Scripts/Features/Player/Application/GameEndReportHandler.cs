using Features.Player.Application.Events;
using Shared.EventBus;
using System;
using UnityEngine;

namespace Features.Player.Application
{
    /// <summary>
    /// Bootstrap 계층에서 게임 종료 통계를 Debug.Log로 출력한다.
    /// </summary>
    public sealed class GameEndReportHandler : IDisposable
    {
        public GameEndReportHandler(IEventSubscriber subscriber)
        {
            subscriber.Subscribe(this, new Action<GameEndReportRequestedEvent>(OnReport));
        }

        private void OnReport(GameEndReportRequestedEvent e)
        {
            Debug.Log($"[GameEnd] ===== Game Result =====");
            Debug.Log($"  Result:     {(e.IsVictory ? "Victory" : "Defeat")}");
            Debug.Log($"  Wave:       {e.ReachedWave}");
            Debug.Log($"  Play Time:  {e.PlayTimeSeconds:F1}s ({e.PlayTimeSeconds / 60f:F1}m)");
            Debug.Log($"  Summons:    {e.SummonCount}");
            Debug.Log($"  Unit Kills: {e.UnitKillCount}");
            Debug.Log($"  K/D Ratio:  {(e.SummonCount > 0 ? (float)e.UnitKillCount / e.SummonCount : 0):F2}");
            Debug.Log($"[GameEnd] =========================");
        }

        public void Dispose()
        {
        }
    }
}
