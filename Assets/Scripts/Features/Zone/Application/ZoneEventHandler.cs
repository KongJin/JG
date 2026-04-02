using Features.Skill.Application.Events;
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
            _spawnZone.Execute(
                e.CasterId, e.Position, e.Direction,
                e.Spec.Range, e.Spec.Duration,
                e.Spec.Damage, e.Spec.StatusPayload,
                e.AllyDamageScale);
        }
    }
}
