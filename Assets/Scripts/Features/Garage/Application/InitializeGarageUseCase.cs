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

            // csharp-guardrails: allow-null-defense
            if (_cloudPort != null)
            {
                try
                {
                    roster = await _cloudPort.LoadGarageAsync();
                    // csharp-guardrails: allow-null-defense
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

            // csharp-guardrails: allow-null-defense
            if (roster == null)
            {
                // csharp-guardrails: allow-null-defense
                roster = _persistence?.Load() ?? new GarageRoster();
            }

            var loadedRoster = roster;
            // csharp-guardrails: allow-null-defense
            roster = _rosterMigration?.Migrate(roster) ?? roster;
            roster.Normalize();
            if (loadedFromCloud || !ReferenceEquals(roster, loadedRoster))
                // csharp-guardrails: allow-null-defense
                _persistence?.Save(roster);

            // csharp-guardrails: allow-null-defense
            _networkPort?.SyncRoster(roster);
            // csharp-guardrails: allow-null-defense
            _networkPort?.SyncReady(roster.IsValid);
            return roster;
        }
    }
}