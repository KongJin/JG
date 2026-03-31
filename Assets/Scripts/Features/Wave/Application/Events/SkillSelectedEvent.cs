using Shared.Kernel;

namespace Features.Wave.Application.Events
{
    public readonly struct SkillSelectedEvent
    {
        public SkillSelectedEvent(DomainEntityId playerId, string chosenSkillId, string displayName)
        {
            PlayerId = playerId;
            ChosenSkillId = chosenSkillId;
            DisplayName = displayName;
        }

        public DomainEntityId PlayerId { get; }
        public string ChosenSkillId { get; }
        public string DisplayName { get; }
    }
}
