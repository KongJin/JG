using Features.Player.Application.Events;
using Features.Skill.Application.Events;
using Features.Wave.Application.Events;
using Shared.Analytics;
using Shared.EventBus;
using System;

namespace Features.Player.Application
{
    public sealed class GameAnalyticsEventHandler
    {
        readonly IAnalyticsPort _analytics;
        private readonly Func<float> _getTime;
        private readonly float _sessionStartTime;

        public GameAnalyticsEventHandler(IAnalyticsPort analytics, IEventSubscriber eventBus, float sessionStartTime)
            : this(analytics, eventBus, sessionStartTime, null)
        {
        }

        /// <summary>
        /// getTime: 시간 조회 함수 주입. null이면 sessionStartTime 기준 상대 시간만 사용.
        /// Anti-pattern 규칙 준수: Application 레이어에서 Unity API 직접 사용 금지.
        /// </summary>
        public GameAnalyticsEventHandler(IAnalyticsPort analytics, IEventSubscriber eventBus, float sessionStartTime, Func<float> getTime)
        {
            _analytics = analytics;
            _getTime = getTime;
            _sessionStartTime = sessionStartTime;

            eventBus.Subscribe<SkillCastedEvent>(this, OnSkillCasted);
            eventBus.Subscribe<PlayerDiedEvent>(this, OnPlayerDied);
            eventBus.Subscribe<GameEndEvent>(this, OnGameEnd);
        }

        void OnSkillCasted(SkillCastedEvent e)
        {
            _analytics.LogAction("skill_used",
                new AnalyticsParams()
                    .Add("slot_index", e.SlotIndex)
                    .Add("skill_id", e.SkillId.ToString())
                    .Build());
        }

        void OnPlayerDied(PlayerDiedEvent e)
        {
            _analytics.LogAction("player_died",
                new AnalyticsParams()
                    .Add("player_id", e.PlayerId.ToString())
                    .Add("attacker_id", e.AttackerId.ToString())
                    .Build());
        }

        void OnGameEnd(GameEndEvent e)
        {
            var playtime = _getTime != null ? _getTime() - _sessionStartTime : 0f;
            _analytics.LogAction("game_end",
                new AnalyticsParams()
                    .Add("result", e.Message)
                    .Add("playtime", playtime)
                    .Build());
        }
    }
}
