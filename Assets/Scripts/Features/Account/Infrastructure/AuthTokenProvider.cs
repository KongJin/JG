namespace Features.Account.Infrastructure
{
    /// <summary>
    /// 현재 인증 토큰 제공자.
    /// 순환 의존성 방지를 위해 정적 접근 제공.
    /// </summary>
    public static class AuthTokenProvider
    {
        private static System.Func<string> _uidProvider;
        private static System.Func<System.Threading.Tasks.Task<string>> _tokenProvider;

        public static void SetProviders(System.Func<string> uidProvider, System.Func<System.Threading.Tasks.Task<string>> tokenProvider)
        {
            _uidProvider = uidProvider;
            _tokenProvider = tokenProvider;
        }

        public static string GetCurrentUid() => _uidProvider?.Invoke();

        public static System.Threading.Tasks.Task<string> GetIdToken() => _tokenProvider?.Invoke() ?? System.Threading.Tasks.Task.FromResult<string>(null);
    }
}
