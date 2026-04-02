namespace Features.Skill.Application.Events
{
    public readonly struct StartSkillCandidate
    {
        public string SkillId { get; }
        public string DisplayName { get; }

        public StartSkillCandidate(string skillId, string displayName)
        {
            SkillId = skillId;
            DisplayName = displayName;
        }
    }

    public readonly struct StartSkillSelectionRequestedEvent
    {
        public StartSkillCandidate[] Candidates { get; }
        public int PickCount { get; }

        public StartSkillSelectionRequestedEvent(StartSkillCandidate[] candidates, int pickCount)
        {
            Candidates = candidates;
            PickCount = pickCount;
        }
    }
}
