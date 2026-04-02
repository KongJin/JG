using System;
using Features.Skill.Application.Events;
using Shared.EventBus;

namespace Features.Skill.Application
{
    public sealed class StartSkillSelectionHandler
    {
        private readonly Action<string[]> _onSkillsSelected;
        private readonly IEventSubscriber _subscriber;
        private bool _handled;

        public StartSkillSelectionHandler(
            IEventSubscriber subscriber,
            Action<string[]> onSkillsSelected)
        {
            _subscriber = subscriber;
            _onSkillsSelected = onSkillsSelected;
            subscriber.Subscribe(this, new Action<StartSkillSelectedEvent>(OnSelected));
        }

        private void OnSelected(StartSkillSelectedEvent e)
        {
            if (_handled) return;
            _handled = true;
            _subscriber.UnsubscribeAll(this);
            _onSkillsSelected(e.ChosenSkillIds);
        }
    }
}
