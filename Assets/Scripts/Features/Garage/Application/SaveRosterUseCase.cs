using Features.Account.Application.Ports;
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
        private readonly IAccountDataPort _accountDataPort;
        private readonly IGarageNetworkPort _network;
        private readonly IEventPublisher _eventBus;
        private readonly System.Func<string> _uidProvider;

        public SaveRosterUseCase(
            IAccountDataPort accountDataPort,
            IGarageNetworkPort network,
            IEventPublisher eventBus,
            System.Func<string> uidProvider)
        {
            _accountDataPort = accountDataPort;
            _network = network;
            _eventBus = eventBus;
            _uidProvider = uidProvider;
        }

        /// <summary>
        /// 편성 저장 실행.
        /// Firestore 저장 + Photon CustomProperties 동기화.
        /// </summary>
        public async System.Threading.Tasks.Task<Result> Execute(GarageRoster roster)
        {
            if (roster == null)
            {
                return Result.Failure("저장할 편성 데이터가 없습니다.");
            }

            roster.Normalize();

            string errorMessage = null;

            // Firestore 저장 (클라우드 SSOT)
            try
            {
                var uid = _uidProvider?.Invoke();
                if (!string.IsNullOrEmpty(uid))
                {
                    var token = await GetIdToken();
                    if (!string.IsNullOrEmpty(token))
                    {
                        await _accountDataPort.SaveGarage(roster, uid, token);
                    }
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

        private async System.Threading.Tasks.Task<string> GetIdToken()
        {
            return await Features.Account.Infrastructure.AuthTokenProvider.GetIdToken();
        }
    }
}
