using System.Collections.Generic;
using Features.Player.Domain;
using Shared.Kernel;
using Shared.Math;

namespace Features.Player.Application.Ports
{
    public interface IPlayerLookupPort
    {
        Domain.Player Resolve(DomainEntityId id);
        IEnumerable<PlayerLookupEntry> AllEntries();
    }

    public readonly struct PlayerLookupEntry
    {
        public readonly DomainEntityId Id;
        public readonly Domain.Player Player;
        public readonly Float3 Position;

        public PlayerLookupEntry(DomainEntityId id, Domain.Player player, Float3 position)
        {
            Id = id;
            Player = player;
            Position = position;
        }
    }
}
