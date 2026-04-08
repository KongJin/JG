using Features.Player.Application.Events;
using Features.Skill.Application.Events;
using Features.Wave.Application.Events;
using Shared.Analytics;
using Shared.EventBus;

namespace Features.Player.Application
{
    public sealed class GameAnalyticsEventHandler
    {
        readonly IAnalyticsPort _analytics;
        private readonly float _sessionStartTime;

        public GameAnalyticsEventHandler(IAnalyticsPort analytics, IEventSubscriber eventBus, float sessionStartTime)
        {
            _analytics = analytics;
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
            var playtime = UnityEngine.Time.realtimeSinceStartup - _sessionStartTime;
            _analytics.LogAction("game_end",
                new AnalyticsParams()
                    .Add("result", e.Message)
                    .Add("playtime", playtime)
                    .Add("is_local_dead", e.IsLocalPlayerDead ? 1 : 0)
                    .Build());
        }
    }
}
