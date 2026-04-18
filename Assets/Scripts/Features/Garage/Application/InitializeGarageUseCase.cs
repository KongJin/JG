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
        public interface ICloudGarageLoadPort
        {
            System.Threading.Tasks.Task<GarageRoster> LoadGarageAsync();
        }

        private readonly ICloudGarageLoadPort _cloudPort;
        private readonly IGaragePersistencePort _persistence;
        private readonly IGarageNetworkPort _networkPort;
        private readonly IEventPublisher _eventBus;

        public InitializeGarageUseCase(
            IGaragePersistencePort persistence,
            IGarageNetworkPort networkPort,
            ICloudGarageLoadPort cloudPort,
            IEventPublisher eventBus)
        {
            _persistence = persistence;
            _networkPort = networkPort;
            _cloudPort = cloudPort;
            _eventBus = eventBus;
        }

        /// <summary>
        /// 차고 초기화 실행.
        /// 저장된 편성이 있으면 복원, 없으면 빈 편성 생성.
        /// </summary>
        public async System.Threading.Tasks.Task<GarageRoster> Execute()
        {
            GarageRoster roster = null;

            if (_cloudPort != null)
            {
                try
                {
                    roster = await _cloudPort.LoadGarageAsync();
                    if (roster != null)
                    {
                        roster.Normalize();
                        _persistence?.Save(roster);
                    }
                }
                catch
                {
                    roster = null;
                }
            }

            if (roster == null)
                roster = _persistence?.Load() ?? new GarageRoster();

            roster.Normalize();
            _networkPort?.SyncRoster(roster);
            _networkPort?.SyncReady(roster.IsValid);
            _eventBus.Publish(new GarageInitializedEvent(roster));
            return roster;
        }
    }
}
