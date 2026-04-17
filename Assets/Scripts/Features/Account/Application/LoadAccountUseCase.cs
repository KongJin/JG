using Features.Account.Application.Ports;
using Features.Account.Domain;
using Features.Garage.Domain;
namespace Features.Account.Application
{
    /// <summary>
    /// 계정 전체 데이터 로드 UseCase.
    /// </summary>
    public sealed class LoadAccountUseCase
    {
        private readonly IAccountDataPort _dataPort;
        private readonly IAuthPort _authPort;

        public LoadAccountUseCase(IAuthPort authPort, IAccountDataPort dataPort)
        {
            _authPort = authPort;
            _dataPort = dataPort;
        }

        public async System.Threading.Tasks.Task<AccountData> Execute()
        {
            var uid = _authPort.GetCurrentUid();
            var token = await _authPort.GetIdToken();

            var account = await _dataPort.LoadProfile(uid, token);
            var stats = await _dataPort.LoadStats(uid, token);
            var settings = await _dataPort.LoadSettings(uid, token);
            var garage = await _dataPort.LoadGarage(uid, token);

            return new AccountData
            {
                Profile = account,
                Stats = stats ?? new PlayerStats(),
                Settings = settings ?? new UserSettings(),
                GarageRoster = garage
            };
        }
    }

    public sealed class AccountData
    {
        public AccountProfile Profile;
        public PlayerStats Stats;
        public UserSettings Settings;
        public GarageRoster GarageRoster;
    }
}
