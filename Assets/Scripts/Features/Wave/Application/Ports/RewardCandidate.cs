using Features.Skill.Domain;

namespace Features.Wave.Application.Ports
{
    public enum CandidateType { NewSkill, Upgrade }

    public readonly struct RewardCandidate
    {
        public CandidateType Type { get; }
        public string SkillId { get; }
        public string DisplayName { get; }
        public GrowthAxis Axis { get; }
        public int CurrentLevel { get; }
        public string EffectDescription { get; }

        public RewardCandidate(string skillId, string displayName)
        {
            Type = CandidateType.NewSkill;
            SkillId = skillId;
            DisplayName = displayName;
            Axis = default;
            CurrentLevel = 0;
            EffectDescription = string.Empty;
        }

        public RewardCandidate(string skillId, string displayName,
            GrowthAxis axis, int currentLevel, string effectDescription)
        {
            Type = CandidateType.Upgrade;
            SkillId = skillId;
            DisplayName = displayName;
            Axis = axis;
            CurrentLevel = currentLevel;
            EffectDescription = effectDescription;
        }
    }
}
