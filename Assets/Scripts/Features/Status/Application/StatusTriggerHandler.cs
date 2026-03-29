using System;
using Features.Projectile.Application.Events;
using Features.Skill.Application.Events;
using Features.Status.Application.Events;
using Features.Zone.Application.Events;
using Shared.EventBus;

namespace Features.Status.Application
{
    /// <summary>
    /// Bridges hit/cast events to the Status system by publishing StatusApplyRequestedEvent
    /// when the originating skill carries a StatusPayload.
    /// </summary>
    public sealed class StatusTriggerHandler
    {
        private readonly IEventPublisher _publisher;

        public StatusTriggerHandler(IEventPublisher publisher, IEventSubscriber subscriber)
        {
            _publisher = publisher;
            subscriber.Subscribe(this, new Action<ProjectileHitEvent>(OnProjectileHit));
            subscriber.Subscribe(this, new Action<SelfRequestedEvent>(OnSelfRequested));
            subscriber.Subscribe(this, new Action<ZoneTickEvent>(OnZoneTick));
        }

        private void OnProjectileHit(ProjectileHitEvent e)
        {
            if (!e.StatusPayload.HasEffect) return;

            var sp = e.StatusPayload;
            _publisher.Publish(new StatusApplyRequestedEvent(
                e.TargetId, sp.Type, sp.Magnitude, sp.Duration, e.OwnerId, sp.TickInterval));
        }

        private void OnSelfRequested(SelfRequestedEvent e)
        {
            var sp = e.Spec.StatusPayload;
            if (!sp.HasEffect) return;

            _publisher.Publish(new StatusApplyRequestedEvent(
                e.CasterId, sp.Type, sp.Magnitude, sp.Duration, e.CasterId, sp.TickInterval));
        }

        private void OnZoneTick(ZoneTickEvent e)
        {
            if (!e.StatusPayload.HasEffect) return;

            var sp = e.StatusPayload;
            _publisher.Publish(new StatusApplyRequestedEvent(
                e.TargetId, sp.Type, sp.Magnitude, sp.Duration, e.CasterId, sp.TickInterval));
        }
    }
}
