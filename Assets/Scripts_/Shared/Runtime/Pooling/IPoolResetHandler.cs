namespace Shared.Runtime.Pooling
{
    public interface IPoolResetHandler
    {
        void OnRentFromPool();
        void OnReturnToPool();
    }
}
