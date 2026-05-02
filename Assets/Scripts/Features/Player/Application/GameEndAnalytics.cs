using Features.Combat.Application.Events;
using Features.Enemy.Application.Events;
using Features.Player.Application.Events;
using Features.Unit.Application.Events;
using Shared.EventBus;
using Shared.Gameplay;
using Shared.Kernel;
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
        private readonly GameEndContributionAnalyzer _contributionAnalyzer;

        private int _summonCount;
        private int _unitKillCount;

        public GameEndAnalytics(IEventSubscriber subscriber, IEventPublisher publisher)
            : this(subscriber, publisher, default, 0f)
        {
        }

        public GameEndAnalytics(
            IEventSubscriber subscriber,
            IEventPublisher publisher,
            DomainEntityId coreId,
            float coreMaxHealth)
        {
            _publisher = publisher;
            _playTimer = System.Diagnostics.Stopwatch.StartNew();
            _contributionAnalyzer = new GameEndContributionAnalyzer(coreId, coreMaxHealth);

            // 소환 이벤트 카운팅
            subscriber.Subscribe(this, new Action<UnitSummonCompletedEvent>(OnUnitSummoned));

            // 적 처치 이벤트 카운팅
            subscriber.Subscribe(this, new Action<EnemyDiedEvent>(OnEnemyDied));

            // 결과 카드에 필요한 전투 맥락 수집
            subscriber.Subscribe(this, new Action<EnemySpawnedEvent>(OnEnemySpawned));
            subscriber.Subscribe(this, new Action<DamageAppliedEvent>(OnDamageApplied));

            // 게임 종료 시 리포트 요청 이벤트 발행
            subscriber.Subscribe(this, new Action<GameEndEvent>(OnGameEnd));
        }

        private void OnUnitSummoned(UnitSummonCompletedEvent e)
        {
            _summonCount++;
            var unitSpec = e.UnitSpec;
            var loadoutKey = unitSpec == null
                ? string.Empty
                : LoadoutKey.Build(unitSpec.FrameId, unitSpec.FirepowerModuleId, unitSpec.MobilityModuleId);

            _contributionAnalyzer.RecordUnitDeployed(
                e.PlayerId,
                e.BattleEntityId,
                loadoutKey);
        }

        private void OnEnemyDied(EnemyDiedEvent e)
        {
            _unitKillCount++;
        }

        private void OnGameEnd(GameEndEvent e)
        {
            var playTime = (float)_playTimer.Elapsed.TotalSeconds;
            var finalPlayTime = e.PlayTimeSeconds > 0f ? e.PlayTimeSeconds : playTime;
            var finalSummonCount = e.SummonCount > 0 ? e.SummonCount : _summonCount;
            var finalUnitKillCount = e.UnitKillCount > 0 ? e.UnitKillCount : _unitKillCount;
            var contributionCards = _contributionAnalyzer.BuildContributionCards(finalSummonCount, finalUnitKillCount);

            _publisher.Publish(new GameEndReportRequestedEvent(
                isVictory: e.IsVictory,
                reachedWave: e.ReachedWave,
                playTimeSeconds: finalPlayTime,
                summonCount: finalSummonCount,
                unitKillCount: finalUnitKillCount,
                contributionCards: contributionCards,
                coreRemainingHealth: _contributionAnalyzer.CoreRemainingHealth,
                coreMaxHealth: _contributionAnalyzer.CoreMaxHealth
            ));
        }

        private void OnEnemySpawned(EnemySpawnedEvent e)
        {
            _contributionAnalyzer.RecordEnemySpawned(e);
        }

        private void OnDamageApplied(DamageAppliedEvent e)
        {
            _contributionAnalyzer.RecordDamageApplied(e);
        }

        public void Dispose()
        {
            _playTimer?.Stop();
        }
    }
}
