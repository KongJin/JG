using Shared.Kernel;

namespace Features.Wave.Application.Events
{
    /// <summary>
    /// 웨이브 보상 선택을 건너뛴다. 덱/업그레이드 적용 없이 다음 웨이브 카운트다운만 진행한다.
    /// </summary>
    public readonly struct SkillSelectionSkippedEvent
    {
        public SkillSelectionSkippedEvent(DomainEntityId playerId, int waveIndex)
        {
            PlayerId = playerId;
            WaveIndex = waveIndex;
        }

        public DomainEntityId PlayerId { get; }
        public int WaveIndex { get; }
    }
}
