using Features.Garage.Application.Ports;
using Features.Garage.Domain;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Garage.Application
{
    /// <summary>
    /// 차고 초기화 UseCase.
    /// 로컬 저장소에서 이전 편성 복원 → 네트워크 동기화 확인.
    /// </summary>
    public sealed class InitializeGarageUseCase
    {
        private readonly IGaragePersistencePort _persistence;
        private readonly IEventPublisher _eventBus;

        public InitializeGarageUseCase(
            IGaragePersistencePort persistence,
            IEventPublisher eventBus)
        {
            _persistence = persistence;
            _eventBus = eventBus;
        }

        /// <summary>
        /// 차고 초기화 실행.
        /// 저장된 편성이 있으면 복원, 없으면 빈 편성 생성.
        /// </summary>
        public GarageRoster Execute()
        {
            GarageRoster roster = _persistence.Load();
            if (roster == null)
            {
                roster = new GarageRoster();
            }

            _eventBus.Publish(new GarageInitializedEvent(roster));
            return roster;
        }
    }
}
