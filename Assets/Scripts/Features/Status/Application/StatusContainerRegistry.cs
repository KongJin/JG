using System.Collections.Generic;
using Features.Status.Domain;
using Shared.Kernel;

namespace Features.Status.Application
{
    public sealed class StatusContainerRegistry
    {
        private readonly Dictionary<DomainEntityId, StatusContainer> _containers =
            new Dictionary<DomainEntityId, StatusContainer>();

        public StatusContainer GetOrCreate(DomainEntityId targetId)
        {
            if (!_containers.TryGetValue(targetId, out var container))
            {
                container = new StatusContainer();
                _containers[targetId] = container;
            }
            return container;
        }

        public bool TryGet(DomainEntityId targetId, out StatusContainer container)
        {
            return _containers.TryGetValue(targetId, out container);
        }

        public IEnumerable<KeyValuePair<DomainEntityId, StatusContainer>> All => _containers;

        public void Remove(DomainEntityId targetId)
        {
            _containers.Remove(targetId);
        }
    }
}
