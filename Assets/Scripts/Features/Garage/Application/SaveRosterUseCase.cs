using Features.Garage.Application.Ports;
using Features.Garage.Domain;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Garage.Application
{
    /// <summary>
    /// 편성 저장 UseCase.
    /// 로컬 저장 + 네트워크 동기화.
    /// </summary>
    public sealed class SaveRosterUseCase
    {
        private readonly IGaragePersistencePort _persistence;
        private readonly IGarageNetworkPort _network;
        private readonly IEventPublisher _eventBus;

        public SaveRosterUseCase(
            IGaragePersistencePort persistence,
            IGarageNetworkPort network,
            IEventPublisher eventBus)
        {
            _persistence = persistence;
            _network = network;
            _eventBus = eventBus;
        }

        /// <summary>
        /// 편성 저장 실행.
        /// 로컬 JSON 저장 + Photon CustomProperties 동기화.
        /// </summary>
        public Result Execute(GarageRoster roster, out string errorMessage)
        {
            errorMessage = null;

            if (roster == null)
            {
                errorMessage = "저장할 편성 데이터가 없습니다.";
                return Result.Failure(errorMessage);
            }

            if (!roster.IsValid)
            {
                errorMessage = $"편성 유닛 수는 3~5기여야 합니다. (현재: {roster.Count}기)";
                return Result.Failure(errorMessage);
            }

            // 로컬 저장 (보조 캐시)
            _persistence.Save(roster);

            // 네트워크 동기화 (실제 전투 진입용 데이터)
            _network.SyncRoster(roster);
            _network.SyncReady(true);

            _eventBus.Publish(new RosterSavedEvent(roster));

            return Result.Success();
        }
    }
}
