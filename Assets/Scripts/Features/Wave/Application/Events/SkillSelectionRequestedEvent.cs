using Features.Wave.Application.Ports;

namespace Features.Wave.Application.Events
{
    public readonly struct SkillSelectionRequestedEvent
    {
        public SkillSelectionRequestedEvent(int waveIndex, SkillRewardCandidate[] candidates)
        {
            WaveIndex = waveIndex;
            Candidates = candidates;
        }

        public int WaveIndex { get; }
        public SkillRewardCandidate[] Candidates { get; }
    }
}
