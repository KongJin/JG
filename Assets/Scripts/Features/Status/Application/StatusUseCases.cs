using Features.Status.Application.Events;
using Features.Status.Application.Ports;
using Features.Status.Domain;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Status.Application
{
    public sealed class StatusUseCases
    {
        private readonly StatusContainerRegistry _registry;
        private readonly IEventPublisher _eventBus;
        private readonly IStatusNetworkPort _network;

        public StatusUseCases(
            StatusContainerRegistry registry,
            IEventPublisher eventBus,
            IStatusNetworkPort network)
        {
            _registry = registry;
            _eventBus = eventBus;
            _network = network;
        }

        public void ApplyStatus(
            DomainEntityId targetId,
            StatusType type,
            float magnitude,
            float duration,
            DomainEntityId sourceId,
            float tickInterval = 0f)
        {
            var container = _registry.GetOrCreate(targetId);
            var effect = new StatusEffect(type, magnitude, duration, sourceId, tickInterval);
            container.Apply(effect);

            _network.SendApplyStatus(targetId, type, magnitude, duration, sourceId, tickInterval);
            _eventBus.Publish(new StatusAppliedEvent(targetId, type, magnitude, duration));
        }

        public void ApplyStatusReplicated(
            DomainEntityId targetId,
            StatusType type,
            float magnitude,
            float duration,
            DomainEntityId sourceId,
            float tickInterval = 0f)
        {
            var container = _registry.GetOrCreate(targetId);
            var effect = new StatusEffect(type, magnitude, duration, sourceId, tickInterval);
            container.Apply(effect);

            _eventBus.Publish(new StatusAppliedEvent(targetId, type, magnitude, duration));
        }

        public float GetCombinedMagnitude(DomainEntityId targetId, StatusType type)
        {
            if (!_registry.TryGet(targetId, out var container))
                return 0f;
            return container.GetCombinedMagnitude(type);
        }

        public bool HasStatus(DomainEntityId targetId, StatusType type)
        {
            if (!_registry.TryGet(targetId, out var container))
                return false;
            return container.Has(type);
        }
    }
}
