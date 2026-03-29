using System;
using Features.Status.Application.Events;
using Shared.EventBus;

namespace Features.Status.Application
{
    public sealed class StatusEventHandler
    {
        private readonly StatusUseCases _useCases;

        public StatusEventHandler(IEventSubscriber subscriber, StatusUseCases useCases)
        {
            _useCases = useCases;
            subscriber.Subscribe(this, new Action<StatusApplyRequestedEvent>(OnApplyRequested));
        }

        private void OnApplyRequested(StatusApplyRequestedEvent e)
        {
            _useCases.ApplyStatus(e.TargetId, e.Type, e.Magnitude, e.Duration, e.SourceId, e.TickInterval);
        }
    }
}
