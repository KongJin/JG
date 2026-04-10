using Shared.Kernel;

namespace Features.Enemy.Application.Ports
{
    public interface IEnemyContactDamagePort
    {
        Result ApplyContactDamage(DomainEntityId targetId, float damage, DomainEntityId attackerId);
    }
}
