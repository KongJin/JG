using Features.Garage.Application.Ports;
using Features.Garage.Domain;
using Shared.EventBus;
using Shared.Kernel;

namespace Features.Garage.Application
{
    /// <summary>
    /// 편성 저장 UseCase.
    /// Firestore 저장 + 네트워크 동기화.
    /// </summary>
    public sealed class SaveRosterUseCase
    {
        /// <summary>
        /// 클라우드 저장 포트. 없으면 Photon 동기화만 수행.
        /// </summary>
        public interface ICloudGaragePort
        {
            System.Threading.Tasks.Task SaveGarageAsync(GarageRoster roster);
        }

        private readonly ICloudGaragePort _cloudPort;
        private readonly IGarageNetworkPort _network;
        private readonly IEventPublisher _eventBus;

        public SaveRosterUseCase(
            ICloudGaragePort cloudPort,
            IGarageNetworkPort network,
            IEventPublisher eventBus)
        {
            _cloudPort = cloudPort;
            _network = network;
            _eventBus = eventBus;
        }

        /// <summary>
        /// 편성 저장 실행.
        /// 클라우드 저장 (선택) + Photon CustomProperties 동기화.
        /// </summary>
        public async System.Threading.Tasks.Task<Result> Execute(GarageRoster roster)
        {
            if (roster == null)
            {
                return Result.Failure("저장할 편성 데이터가 없습니다.");
            }

            roster.Normalize();

            string errorMessage = null;

            // 클라우드 저장 (선택)
            try
            {
                if (_cloudPort != null)
                {
                    await _cloudPort.SaveGarageAsync(roster);
                }
            }
            catch (System.Exception ex)
            {
                errorMessage = $"클라우드 저장 실패: {ex.Message}";
            }

            // 네트워크 동기화 (실제 전투 진입용 데이터)
            _network.SyncRoster(roster);
            _network.SyncReady(roster.IsValid);

            _eventBus.Publish(new RosterSavedEvent(roster));

            if (errorMessage != null)
                return Result.Failure(errorMessage);

            return Result.Success();
        }
    }
}
