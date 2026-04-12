using Features.Account.Application.Ports;
using Features.Account.Domain;
using Shared.Kernel;

namespace Features.Account.Application
{
    /// <summary>
    /// Google 로그인 UseCase.
    /// Google ID 토큰으로 Firebase Auth 인증 후 프로필 로드/생성.
    /// </summary>
    public sealed class SignInWithGoogleUseCase
    {
        private readonly IAuthPort _authPort;
        private readonly IAccountDataPort _dataPort;

        public SignInWithGoogleUseCase(IAuthPort authPort, IAccountDataPort dataPort)
        {
            _authPort = authPort;
            _dataPort = dataPort;
        }

        /// <summary>
        /// Google 로그인 실행.
        /// 기존 익명 계정이 있으면 Firebase Auth linking을 시도한다.
        /// </summary>
        public async System.Threading.Tasks.Task<Result<AccountProfile>> Execute(string googleIdToken)
        {
            if (string.IsNullOrWhiteSpace(googleIdToken))
                return Result<AccountProfile>.Failure("Google ID 토큰이 비어 있습니다.");

            var token = await _authPort.SignInWithGoogle(googleIdToken);
            var account = await _dataPort.LoadProfile(token.Uid, token.IdToken);

            if (account == null)
            {
                account = new AccountProfile(token.Uid, "google");
            }
            else
            {
                account.authType = "google";
            }

            await _dataPort.SaveProfile(account, token.IdToken);

            return Result<AccountProfile>.Success(account);
        }
    }
}
