using Features.Skill.Domain;
using Features.Wave.Application.Ports;
using Shared.Kernel;

namespace Features.Wave.Application.Events
{
    public readonly struct SkillSelectedEvent
    {
        public SkillSelectedEvent(
            DomainEntityId playerId,
            string chosenSkillId,
            string displayName,
            CandidateType candidateType = CandidateType.NewSkill,
            GrowthAxis axis = default)
        {
            PlayerId = playerId;
            ChosenSkillId = chosenSkillId;
            DisplayName = displayName;
            CandidateType = candidateType;
            Axis = axis;
        }

        public DomainEntityId PlayerId { get; }
        public string ChosenSkillId { get; }
        public string DisplayName { get; }
        public CandidateType CandidateType { get; }
        public GrowthAxis Axis { get; }
    }
}
