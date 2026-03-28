using Features.Player.Application.Events;
using Features.Skill.Application.Events;
using Shared.Analytics;
using Shared.EventBus;

namespace Features.Player.Application
{
    public sealed class GameAnalyticsEventHandler
    {
        readonly IAnalyticsPort _analytics;

        public GameAnalyticsEventHandler(IAnalyticsPort analytics, IEventSubscriber eventBus)
        {
            _analytics = analytics;

            eventBus.Subscribe<SkillCastedEvent>(this, OnSkillCasted);
            eventBus.Subscribe<PlayerDiedEvent>(this, OnPlayerDied);
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
    }
}
