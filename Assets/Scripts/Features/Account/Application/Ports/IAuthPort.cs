namespace Features.Account.Application.Ports
{
    /// <summary>
    /// Firebase Auth REST API 포트.
    /// </summary>
    public interface IAuthPort
    {
        /// <summary>
        /// 익명 로그인. idToken + UID 발급.
        /// </summary>
        System.Threading.Tasks.Task<AuthToken> SignInAnonymously();

        /// <summary>
        /// Google 로그인. Google ID 토큰으로 Firebase Auth 토큰 교환.
        /// </summary>
        System.Threading.Tasks.Task<AuthToken> SignInWithGoogle(string googleIdToken);

        /// <summary>
        /// 로그아웃. 로컬 토큰 정리.
        /// </summary>
        void SignOut();

        /// <summary>
        /// 현재 UID 반환.
        /// </summary>
        string GetCurrentUid();

        /// <summary>
        /// 현재 유효한 idToken 반환 (만료 시 자동 갱신).
        /// </summary>
        System.Threading.Tasks.Task<string> GetIdToken();

        /// <summary>
        /// 계정 삭제 (Auth 측).
        /// </summary>
        System.Threading.Tasks.Task DeleteAccount(string idToken);
    }

    /// <summary>
    /// Firebase Auth 토큰 응답.
    /// </summary>
    public sealed class AuthToken
    {
        public string IdToken;
        public string RefreshToken;
        public string Uid;
        public long ExpiresInMs;
    }
}
