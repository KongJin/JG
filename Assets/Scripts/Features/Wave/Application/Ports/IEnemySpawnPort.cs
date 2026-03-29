namespace Features.Wave.Application.Ports
{
    public interface IEnemySpawnPort
    {
        void SpawnEnemy(string prefabName, float x, float y, float z);
    }
}
