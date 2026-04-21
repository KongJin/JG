namespace Features.Player.Application.Ports
{
    public interface IEnergyRegenPort
    {
        void TickRegen(float deltaTime, float currentTime);
    }
}
