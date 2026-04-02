using System;
using Features.Combat.Domain;
using Features.Zone.Application.Events;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Combat.Application
{
    public sealed class ZoneDamageHandler
    {
        private readonly ApplyDamageUseCase _applyDamage;
        private readonly DomainEntityId _localAuthorityId;
        private readonly bool _hasAuthorityFilter;

        public ZoneDamageHandler(
            ApplyDamageUseCase applyDamage,
            IEventSubscriber eventBus,
            DomainEntityId localAuthorityId = default)
        {
            _applyDamage = applyDamage;
            _localAuthorityId = localAuthorityId;
            _hasAuthorityFilter = !string.IsNullOrWhiteSpace(localAuthorityId.Value);
            eventBus.Subscribe(this, new Action<ZoneTickEvent>(OnZoneTick));
        }

        private void OnZoneTick(ZoneTickEvent e)
        {
            if (_hasAuthorityFilter && !e.CasterId.Equals(_localAuthorityId))
                return;

            _applyDamage.Execute(e.TargetId, e.BaseDamage, DamageType.Magical, e.CasterId, e.AllyDamageScale);
        }
    }
}
