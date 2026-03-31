namespace Features.Wave.Application.Ports
{
    public readonly struct SkillRewardCandidate
    {
        public SkillRewardCandidate(string skillId, string displayName)
        {
            SkillId = skillId;
            DisplayName = displayName;
        }

        public string SkillId { get; }
        public string DisplayName { get; }
    }

    public interface ISkillRewardPort
    {
        SkillRewardCandidate[] DrawCandidates(int count);
        void AddToDeck(string skillId);
    }
}
