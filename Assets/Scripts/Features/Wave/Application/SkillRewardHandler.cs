using System;
using Features.Skill.Application.Ports;
using Features.Wave.Application.Events;
using Features.Wave.Application.Ports;
using Shared.EventBus;

namespace Features.Wave.Application
{
    public sealed class SkillRewardHandler
    {
        private readonly ISkillRewardPort _skillReward;
        private readonly ISkillUpgradeCommandPort _upgradeCommand;
        private bool _applied;

        public SkillRewardHandler(IEventSubscriber subscriber, ISkillRewardPort skillReward,
            ISkillUpgradeCommandPort upgradeCommand = null)
        {
            _skillReward = skillReward;
            _upgradeCommand = upgradeCommand;
            subscriber.Subscribe(this, new Action<SkillSelectionRequestedEvent>(OnSelectionRequested));
            subscriber.Subscribe(this, new Action<SkillSelectedEvent>(OnSkillSelected));
        }

        private void OnSelectionRequested(SkillSelectionRequestedEvent e)
        {
            _applied = false;
        }

        private void OnSkillSelected(SkillSelectedEvent e)
        {
            if (_applied) return;
            _applied = true;

            if (e.CandidateType == CandidateType.NewSkill)
            {
                _skillReward.AddToDeck(e.ChosenSkillId);
            }
            else if (e.CandidateType == CandidateType.Upgrade && _upgradeCommand != null)
            {
                _upgradeCommand.TryUpgrade(e.ChosenSkillId, e.Axis);
            }
        }
    }
}
