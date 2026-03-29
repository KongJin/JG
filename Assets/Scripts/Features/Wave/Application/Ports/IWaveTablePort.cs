namespace Features.Wave.Application.Ports
{
    public interface IWaveTablePort
    {
        int WaveCount { get; }
        float GetCountdownDuration(int waveIndex);
        int GetEnemyCount(int waveIndex);
    }
}
