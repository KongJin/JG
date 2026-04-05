using Shared.Kernel;

namespace Features.Enemy.Application.Ports
{
    public interface IPlayerPositionQuery
    {
        (float x, float y, float z) GetNearestPlayerPosition(float fromX, float fromY, float fromZ);

        bool TryGetNearestPlayerWithinHorizontalRadius(
            float fromX,
            float fromY,
            float fromZ,
            float radius,
            out float tx,
            out float ty,
            out float tz);
    }

    public interface ICoreObjectiveQuery
    {
        DomainEntityId CoreId { get; }
        float CoreMaxHp { get; }

        bool TryGetCoreWorldPosition(out float x, out float y, out float z);
    }
}
