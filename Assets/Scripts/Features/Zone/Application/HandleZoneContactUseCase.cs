using Features.Status.Application.Events;
using Features.Status.Domain;
using Features.Zone.Application.Events;
using Features.Zone.Domain;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Zone.Application
{
    public sealed class HandleZoneContactUseCase
    {
        private readonly IEventPublisher _publisher;

        public HandleZoneContactUseCase(IEventPublisher publisher)
        {
            _publisher = publisher;
        }

        public void Execute(
            DomainEntityId zoneId,
            DomainEntityId casterId,
            DomainEntityId targetId,
            float baseDamage,
            ZoneStatusPayload statusPayload,
            float allyDamageScale)
        {
            _publisher.Publish(new ZoneTickEvent(
                zoneId,
                casterId,
                targetId,
                baseDamage,
                statusPayload,
                allyDamageScale));

            var statusType = ConvertZoneStatusType(statusPayload.Type);
            if (!statusType.HasValue)
                return;

            _publisher.Publish(new StatusApplyRequestedEvent(
                targetId,
                statusType.Value,
                statusPayload.Magnitude,
                statusPayload.Duration,
                casterId,
                statusPayload.TickInterval));
        }

        private static StatusType? ConvertZoneStatusType(ZoneStatusPayload.ZoneStatusType zoneType)
        {
            return zoneType switch
            {
                ZoneStatusPayload.ZoneStatusType.Slow => StatusType.Slow,
                ZoneStatusPayload.ZoneStatusType.Haste => StatusType.Haste,
                ZoneStatusPayload.ZoneStatusType.DoT => StatusType.Burn,
                ZoneStatusPayload.ZoneStatusType.None => null,
                _ => null,
            };
        }
    }
}
