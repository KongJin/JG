using Features.Account.Application.Ports;
using Features.Account.Domain;
using Shared.Kernel;

namespace Features.Account.Application
{
    /// <summary>
    /// 익명 로그인 UseCase.
    /// </summary>
    public sealed class SignInAnonymouslyUseCase
    {
        private readonly IAuthPort _authPort;
        private readonly IAccountDataPort _dataPort;

        public SignInAnonymouslyUseCase(IAuthPort authPort, IAccountDataPort dataPort)
        {
            _authPort = authPort;
            _dataPort = dataPort;
        }

        public async System.Threading.Tasks.Task<Result<AccountProfile>> Execute()
        {
            var token = await _authPort.SignInAnonymously();
            var account = await _dataPort.LoadProfile(token.Uid, token.IdToken);

            if (account == null)
            {
                account = new AccountProfile(token.Uid, "anonymous");
                await _dataPort.SaveProfile(account, token.IdToken);
            }

            return Result<AccountProfile>.Success(account);
        }
    }
}

