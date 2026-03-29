using Features.Player.Application.Ports;
using Features.Status.Domain;
using Shared.Kernel;

namespace Features.Status.Application
{
    public sealed class SpeedModifierAdapter : ISpeedModifierPort
    {
        private readonly StatusContainerRegistry _registry;

        public SpeedModifierAdapter(StatusContainerRegistry registry)
        {
            _registry = registry;
        }

        public float GetModifiedSpeed(DomainEntityId playerId, float baseSpeed)
        {
            if (!_registry.TryGet(playerId, out var container))
                return baseSpeed;

            var haste = container.GetCombinedMagnitude(StatusType.Haste);
            var slow = container.GetCombinedMagnitude(StatusType.Slow);
            return StatusRule.ApplySpeedModifier(baseSpeed, haste, slow);
        }
    }
}
