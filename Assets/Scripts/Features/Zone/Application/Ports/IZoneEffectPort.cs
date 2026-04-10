using Features.Zone.Domain;
using Shared.Kernel;
using Shared.Math;

namespace Features.Zone.Application.Ports
{
    public interface IZoneEffectPort
    {
        void SpawnZone(
            Float3 position,
            float radius,
            float duration,
            DomainEntityId zoneId,
            DomainEntityId casterId,
            float baseDamage,
            ZoneStatusPayload statusPayload,
            float allyDamageScale = 1f);
    }
}
