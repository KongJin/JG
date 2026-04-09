using Features.Enemy.Application.Events;
using Features.Player.Application.Events;
using Features.Unit.Application.Events;
using Shared.EventBus;
using System;

namespace Features.Player.Application
{
    /// <summary>
    /// 게임 종료 시 전적/통계를 수집하고 GameEndReportRequestedEvent를 발행한다.
    /// 실제 로그 출력 책임은 Bootstrap에 있다.
    /// </summary>
    public sealed class GameEndAnalytics : IDisposable
    {
        private readonly IEventPublisher _publisher;
        private readonly System.Diagnostics.Stopwatch _playTimer;

        private int _summonCount;
        private int _unitKillCount;

        public GameEndAnalytics(IEventSubscriber subscriber, IEventPublisher publisher)
        {
            _publisher = publisher;
            _playTimer = System.Diagnostics.Stopwatch.StartNew();

            // 소환 이벤트 카운팅
            subscriber.Subscribe(this, new Action<UnitSummonCompletedEvent>(OnUnitSummoned));

            // 적 처치 이벤트 카운팅
            subscriber.Subscribe(this, new Action<EnemyDiedEvent>(OnEnemyDied));

            // 게임 종료 시 리포트 요청 이벤트 발행
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
            var playTime = (float)_playTimer.Elapsed.TotalSeconds;
            var finalPlayTime = e.PlayTimeSeconds > 0f ? e.PlayTimeSeconds : playTime;

            _publisher.Publish(new GameEndReportRequestedEvent(
                isVictory: e.IsVictory,
                reachedWave: e.ReachedWave,
                playTimeSeconds: finalPlayTime,
                summonCount: e.SummonCount > 0 ? e.SummonCount : _summonCount,
                unitKillCount: e.UnitKillCount > 0 ? e.UnitKillCount : _unitKillCount
            ));
        }

        public void Dispose()
        {
            _playTimer?.Stop();
        }
    }
}
