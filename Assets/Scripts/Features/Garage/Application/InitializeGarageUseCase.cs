using Features.Garage.Application.Ports;
using Features.Garage.Domain;

namespace Features.Garage.Application
{
    /// <summary>
    /// 차고 초기화 UseCase.
    /// 로컬 저장소에서 이전 편성 복원 → 네트워크 동기화 확인.
    /// </summary>
    public sealed class InitializeGarageUseCase
    {
        private readonly ICloudGarageLoadPort _cloudPort;
        private readonly IGaragePersistencePort _persistence;
        private readonly IGarageNetworkPort _networkPort;
        private readonly IRosterMigrationPort _rosterMigration;

        public InitializeGarageUseCase(
            IGaragePersistencePort persistence,
            IGarageNetworkPort networkPort,
            ICloudGarageLoadPort cloudPort,
            IRosterMigrationPort rosterMigration = null)
        {
            _persistence = persistence;
            _networkPort = networkPort;
            _cloudPort = cloudPort;
            _rosterMigration = rosterMigration;
        }

        /// <summary>
        /// 차고 초기화 실행.
        /// 저장된 편성이 있으면 복원, 없으면 빈 편성 생성.
        /// </summary>
        public async System.Threading.Tasks.Task<GarageRoster> Execute()
        {
            GarageRoster roster = null;
            bool loadedFromCloud = false;

            if (_cloudPort != null)
            {
                try
                {
                    roster = await _cloudPort.LoadGarageAsync();
                    if (roster != null)
                    {
                        roster.Normalize();
                        loadedFromCloud = true;
                    }
                }
                catch
                {
                    roster = null;
                }
            }

            if (roster == null)
                roster = _persistence?.Load() ?? new GarageRoster();

            var loadedRoster = roster;
            roster = _rosterMigration?.Migrate(roster) ?? roster;
            roster.Normalize();
            if (loadedFromCloud || !ReferenceEquals(roster, loadedRoster))
                _persistence?.Save(roster);

            _networkPort?.SyncRoster(roster);
            _networkPort?.SyncReady(roster.IsValid);
            return roster;
        }
    }
}
