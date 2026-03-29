namespace Features.Wave.Application.Ports
{
    public interface IPlayerPositionQuery
    {
        (float x, float y, float z) GetNearestPlayerPosition(float fromX, float fromY, float fromZ);
    }
}
