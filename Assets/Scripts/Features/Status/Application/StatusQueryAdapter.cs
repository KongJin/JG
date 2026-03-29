using Features.Skill.Application.Ports;
using Features.Status.Domain;
using Shared.Kernel;

namespace Features.Status.Application
{
    public sealed class StatusQueryAdapter : IStatusQueryPort
    {
        private readonly StatusContainerRegistry _registry;

        public StatusQueryAdapter(StatusContainerRegistry registry)
        {
            _registry = registry;
        }

        public float GetMagnitude(DomainEntityId targetId, StatusType type)
        {
            if (!_registry.TryGet(targetId, out var container))
                return 0f;

            return container.GetCombinedMagnitude(type);
        }
    }
}
