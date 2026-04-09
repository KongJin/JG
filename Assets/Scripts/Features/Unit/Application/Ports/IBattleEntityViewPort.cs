using Features.Unit.Domain;
using Shared.EventBus;

namespace Features.Unit.Application.Ports
{
    /// <summary>
    /// BattleEntity 시각 표현 초기화 포트.
    /// Infrastructure는 이 계약만 통해 prefab-owned view를 초기화한다.
    /// </summary>
    public interface IBattleEntityViewPort
    {
        void Initialize(IEventSubscriber eventBus, BattleEntity battleEntity);
    }
}
