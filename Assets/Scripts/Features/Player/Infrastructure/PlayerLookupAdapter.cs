using System.Collections.Generic;
using Features.Player.Application.Ports;
using Shared.Kernel;
using Shared.Math;

namespace Features.Player.Infrastructure
{
    public sealed class PlayerLookupAdapter : IPlayerLookupPort
    {
        private readonly PlayerSceneRegistry _registry;

        public PlayerLookupAdapter(PlayerSceneRegistry registry)
        {
            _registry = registry;
        }

        public Domain.Player Resolve(DomainEntityId id)
        {
            if (_registry.TryGet(id, out var setup) && setup.DomainPlayer != null)
                return setup.DomainPlayer;

            return null;
        }

        public IEnumerable<PlayerLookupEntry> AllEntries()
        {
            foreach (var setup in _registry.All)
            {
                if (setup.DomainPlayer == null || setup.transform == null)
                    continue;

                var t = setup.transform.position;
                yield return new PlayerLookupEntry(
                    setup.PlayerId,
                    setup.DomainPlayer,
                    new Float3(t.x, t.y, t.z)
                );
            }
        }
    }
}
