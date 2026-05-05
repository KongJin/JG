using Features.Account.Application.Ports;
using Features.Account.Domain;

namespace Features.Account.Application
{
    /// <summary>
    /// 계정 데이터 저장 UseCase.
    /// </summary>
    public sealed class SaveAccountUseCase
    {
        private readonly IAccountDataPort _dataPort;
        private readonly IAuthPort _authPort;

        public SaveAccountUseCase(IAuthPort authPort, IAccountDataPort dataPort)
        {
            _authPort = authPort;
            _dataPort = dataPort;
        }

        public async System.Threading.Tasks.Task Execute(AccountProfile profile, PlayerStats stats, UserSettings settings)
        {
            var token = await _authPort.GetIdToken();
            var uid = profile.uid;

            await _dataPort.SaveProfile(profile, token);
            await _dataPort.SaveStats(stats, uid, token);
            await _dataPort.SaveSettings(settings, uid, token);
        }
    }
}

