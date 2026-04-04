namespace Features.Wave.Application.Ports
{
    /// <summary>
    /// 웨이브 스폰 개수에 곱할 배율. Unity 타입 없음 — Application/Ports에 둔다(anti_patterns).
    /// </summary>
    public interface IDifficultySpawnScale
    {
        float SpawnCountMultiplier { get; }
    }
}
