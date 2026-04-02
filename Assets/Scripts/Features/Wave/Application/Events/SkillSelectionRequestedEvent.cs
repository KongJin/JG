using Features.Wave.Application.Ports;

namespace Features.Wave.Application.Events
{
    public readonly struct SkillSelectionRequestedEvent
    {
        public SkillSelectionRequestedEvent(int waveIndex, RewardCandidate[] candidates, float selectionDuration = 10f)
        {
            WaveIndex = waveIndex;
            Candidates = candidates;
            SelectionDuration = selectionDuration;
        }

        public int WaveIndex { get; }
        public RewardCandidate[] Candidates { get; }
        public float SelectionDuration { get; }
    }
}
