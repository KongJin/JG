using System.Collections.Generic;
using Features.Status.Application.Events;
using Features.Status.Application.Ports;
using Features.Status.Domain;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Status.Application
{
    public sealed class StatusTickUseCase : IStatusTickPort
    {
        private readonly StatusContainerRegistry _registry;
        private readonly IEventPublisher _eventBus;
        private readonly IStatusNetworkCommandPort _network;
        private readonly bool _isMaster;
        private readonly List<StatusType> _expiringTypes = new List<StatusType>();

        public StatusTickUseCase(
            StatusContainerRegistry registry,
            IEventPublisher eventBus,
            IStatusNetworkCommandPort network,
            bool isMaster)
        {
            _registry = registry;
            _eventBus = eventBus;
            _network = network;
            _isMaster = isMaster;
        }

        public void Tick(float deltaTime)
        {
            foreach (var kvp in _registry.All)
            {
                var targetId = kvp.Key;
                var container = kvp.Value;
                TickContainer(targetId, container, deltaTime);
            }
        }

        private void TickContainer(DomainEntityId targetId, StatusContainer container, float deltaTime)
        {
            var effects = container.Effects;

            // Collect types that are about to expire before ticking
            _expiringTypes.Clear();
            for (var i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                effect.Tick(deltaTime);

                if (_isMaster && effect.ConsumeTickIfReady())
                {
                    var damage = StatusRule.CalculateBurnDamage(effect.Magnitude);
                    _network.SendTickDamage(targetId, damage, effect.SourceId);
                    _eventBus.Publish(new StatusTickDamageEvent(targetId, damage, effect.SourceId));
                }

                if (effect.IsExpired && !_expiringTypes.Contains(effect.Type))
                    _expiringTypes.Add(effect.Type);
            }

            container.RemoveExpired();

            for (var i = 0; i < _expiringTypes.Count; i++)
                _eventBus.Publish(new StatusExpiredEvent(targetId, _expiringTypes[i]));
        }
    }
}
