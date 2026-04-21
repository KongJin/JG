namespace Features.Status.Application.Ports
{
    public interface IStatusTickPort
    {
        void Tick(float deltaTime);
    }
}
