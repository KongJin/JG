using Features.Account.Application.Ports;

namespace Features.Account.Application
{
    /// <summary>
    /// 계정 삭제 UseCase.
    /// Firestore 문서 삭제 → Auth 계정 삭제 순서.
    /// </summary>
    public sealed class DeleteAccountUseCase
    {
        private readonly IAuthPort _authPort;
        private readonly IAccountDataPort _dataPort;

        public DeleteAccountUseCase(IAuthPort authPort, IAccountDataPort dataPort)
        {
            _authPort = authPort;
            _dataPort = dataPort;
        }

        public async System.Threading.Tasks.Task Execute()
        {
            var uid = _authPort.GetCurrentUid();
            var token = await _authPort.GetIdToken();

            // 1. Firestore 문서 삭제
            await _dataPort.DeleteAccount(uid, token);

            // 2. Auth 계정 삭제
            await _authPort.DeleteAccount(token);

            _authPort.SignOut();
        }
    }
}
