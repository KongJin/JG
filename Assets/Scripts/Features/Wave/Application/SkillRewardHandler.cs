using System;
using Features.Wave.Application.Events;
using Features.Wave.Application.Ports;
using Shared.EventBus;

namespace Features.Wave.Application
{
    public sealed class SkillRewardHandler
    {
        private readonly ISkillRewardPort _skillReward;

        public SkillRewardHandler(IEventSubscriber subscriber, ISkillRewardPort skillReward)
        {
            _skillReward = skillReward;
            subscriber.Subscribe(this, new Action<SkillSelectedEvent>(OnSkillSelected));
        }

        private void OnSkillSelected(SkillSelectedEvent e)
        {
            _skillReward.AddToDeck(e.ChosenSkillId);
        }
    }
}
