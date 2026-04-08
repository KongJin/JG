using Features.Unit.Application.Events;
using Features.Unit.Application.Ports;
using Shared.EventBus;

namespace Features.Unit.Application
{
    /// <summary>
    /// BattleEntity 사망 처리 핸들러.
    /// UnitDiedEvent를 받아 Infrastructure에 제거를 위임한다.
    /// </summary>
    public sealed class UnitDeathEventHandler
    {
        private readonly IEventSubscriber _subscriber;
        private readonly IBattleEntityDespawnPort _despawnPort;

        public UnitDeathEventHandler(
            IEventSubscriber subscriber,
            IBattleEntityDespawnPort despawnPort)
        {
            _subscriber = subscriber;
            _despawnPort = despawnPort;
            _subscriber.Subscribe(this, new System.Action<UnitDiedEvent>(OnUnitDied));
        }

        private void OnUnitDied(UnitDiedEvent e)
        {
            _despawnPort.Despawn(e.EntityId);
        }
    }
}
