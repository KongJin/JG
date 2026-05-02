using Features.Unit.Domain;
using Shared.Kernel;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Features.Unit.Application.Events
{
    /// <summary>
    /// 유닛 소환 완료 이벤트.
    /// </summary>
    public readonly struct UnitSummonCompletedEvent
    {
        public DomainEntityId PlayerId { get; }
        public DomainEntityId BattleEntityId { get; }
        public UnitSpec UnitSpec { get; }

        public UnitSummonCompletedEvent(DomainEntityId playerId, DomainEntityId battleEntityId, UnitSpec unitSpec)
        {
            PlayerId = playerId;
            BattleEntityId = battleEntityId;
            UnitSpec = unitSpec;
        }
    }

    /// <summary>
    /// 유닛 소환 실패 이벤트.
    /// </summary>
    public readonly struct UnitSummonFailedEvent
    {
        public DomainEntityId PlayerId { get; }
        public UnitSpec UnitSpec { get; }
        public string Reason { get; }

        public UnitSummonFailedEvent(DomainEntityId playerId, UnitSpec unitSpec, string reason)
        {
            PlayerId = playerId;
            UnitSpec = unitSpec;
            Reason = reason;
        }
    }

    /// <summary>
    /// BattleEntity 사망 이벤트.
    /// </summary>
    public readonly struct UnitDiedEvent
    {
        public DomainEntityId EntityId { get; }
        public DomainEntityId OwnerId { get; }

        public UnitDiedEvent(DomainEntityId entityId, DomainEntityId ownerId)
        {
            EntityId = entityId;
            OwnerId = ownerId;
        }
    }
}
