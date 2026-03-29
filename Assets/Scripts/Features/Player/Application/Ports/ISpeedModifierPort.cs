using Shared.Kernel;

namespace Features.Player.Application.Ports
{
    public interface ISpeedModifierPort
    {
        float GetModifiedSpeed(DomainEntityId playerId, float baseSpeed);
    }
}
