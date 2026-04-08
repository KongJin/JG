using Features.Unit.Domain;
using Shared.Kernel;

namespace Features.Unit.Application.Events
{
    /// <summary>
    /// 플레이어의 Unit 스펙 계산 완료 이벤트.
    /// </summary>
    public readonly struct UnitSpecsComputedEvent
    {
        public DomainEntityId PlayerId { get; }
        public Unit[] UnitSpecs { get; }

        public UnitSpecsComputedEvent(DomainEntityId playerId, Unit[] unitSpecs)
        {
            PlayerId = playerId;
            UnitSpecs = unitSpecs;
        }
    }
}
