namespace Features.Skill.Domain
{
    public readonly struct SkillRewardCandidate
    {
        public string SkillId { get; }
        public string DisplayName { get; }

        public SkillRewardCandidate(string skillId, string displayName)
        {
            SkillId = skillId;
            DisplayName = displayName;
        }
    }
}
