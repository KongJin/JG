using Features.Status.Application.Ports;
using Features.Status.Domain;
using Shared.Kernel;

namespace Features.Status.Application
{
    public sealed class StatusNetworkEventHandler
    {
        private readonly StatusUseCases _useCases;

        public StatusNetworkEventHandler(
            StatusUseCases useCases,
            IStatusNetworkCallbackPort callbacks)
        {
            _useCases = useCases;
            WireCallbackPort(callbacks);
        }

        public void WireCallbackPort(IStatusNetworkCallbackPort callbacks)
        {
            callbacks.OnRemoteStatusApplied = HandleRemoteStatusApplied;
            callbacks.OnRemoteTickDamage = HandleRemoteTickDamage;
        }

        private void HandleRemoteStatusApplied(
            DomainEntityId targetId,
            StatusType type,
            float magnitude,
            float duration,
            DomainEntityId sourceId,
            float tickInterval)
        {
            _useCases.ApplyStatusReplicated(targetId, type, magnitude, duration, sourceId, tickInterval);
        }

        private void HandleRemoteTickDamage(
            DomainEntityId targetId,
            float damage,
            DomainEntityId sourceId)
        {
            // Tick damage is applied via Combat feature's ApplyDamage.
            // This handler publishes StatusTickDamageEvent for any listener (e.g., VFX).
        }
    }
}
