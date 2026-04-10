using Features.Skill.Application.Events;
using Features.Status.Domain;
using Features.Zone.Application.Events;
using Features.Zone.Domain;
using Shared.EventBus;

namespace Features.Zone.Application
{
    public sealed class ZoneEventHandler
    {
        private readonly SpawnZoneUseCase _spawnZone;

        public ZoneEventHandler(SpawnZoneUseCase spawnZone, IEventSubscriber eventBus)
        {
            _spawnZone = spawnZone;
            eventBus.Subscribe(this, new System.Action<ZoneRequestedEvent>(OnZoneRequested));
        }

        private void OnZoneRequested(ZoneRequestedEvent e)
        {
            var zonePayload = ConvertStatusPayload(e.Spec.StatusPayload);

            _spawnZone.Execute(
                e.CasterId, e.Position, e.Direction,
                e.Spec.Range, e.Spec.Duration,
                e.Spec.Damage, zonePayload,
                e.AllyDamageScale);
        }

        private static ZoneStatusPayload ConvertStatusPayload(StatusPayload statusPayload)
        {
            if (!statusPayload.HasEffect)
                return ZoneStatusPayload.None;

            var zoneType = statusPayload.Type switch
            {
                StatusType.Slow => ZoneStatusPayload.ZoneStatusType.Slow,
                StatusType.Haste => ZoneStatusPayload.ZoneStatusType.Haste,
                _ => ZoneStatusPayload.ZoneStatusType.None,
            };

            return ZoneStatusPayload.Create(zoneType, statusPayload.Magnitude, statusPayload.Duration, statusPayload.TickInterval);
        }
    }
}
