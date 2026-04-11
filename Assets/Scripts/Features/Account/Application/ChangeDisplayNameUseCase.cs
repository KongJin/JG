using Features.Account.Application.Ports;
using Features.Account.Domain;
using Shared.Kernel;

namespace Features.Account.Application
{
    /// <summary>
    /// 닉네임 변경 UseCase. 월 1회 제한.
    /// </summary>
    public sealed class ChangeDisplayNameUseCase
    {
        private const long CooldownMs = 30L * 24 * 60 * 60 * 1000; // 30일

        private readonly IAccountDataPort _dataPort;
        private readonly IAuthPort _authPort;

        public ChangeDisplayNameUseCase(IAuthPort authPort, IAccountDataPort dataPort)
        {
            _authPort = authPort;
            _dataPort = dataPort;
        }

        public async System.Threading.Tasks.Task<Result> Execute(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName) || newName.Length > 20)
            {
                return Result.Failure("닉네임은 20자 이하여야 합니다.");
            }

            var uid = _authPort.GetCurrentUid();
            var token = await _authPort.GetIdToken();
            var account = await _dataPort.LoadProfile(uid, token);

            if (account == null)
            {
                return Result.Failure("계정 정보를 찾을 수 없습니다.");
            }

            long now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long cooldownEnd = account.createdAtUnixMs + CooldownMs;

            // 최초 생성 직후에는 변경 가능
            if (now < cooldownEnd && account.displayName != account.uid.Substring(0, System.Math.Min(8, account.uid.Length)))
            {
                long remainingMs = cooldownEnd - now;
                int remainingDays = (int)(remainingMs / (1000 * 60 * 60 * 24)) + 1;
                return Result.Failure($"닉네임은 한 달에 한 번만 변경할 수 있습니다. ({remainingDays}일 후 가능)");
            }

            account.displayName = newName.Trim();
            await _dataPort.SaveProfile(account, token);

            return Result.Success();
        }
    }
}
