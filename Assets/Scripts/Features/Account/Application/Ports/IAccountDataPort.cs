using Features.Account.Domain;

namespace Features.Account.Application.Ports
{
    /// <summary>
    /// Firestore REST API 포트.
    /// </summary>
    public interface IAccountDataPort
    {
        /// <summary>
        /// 계정 프로필 저장.
        /// </summary>
        System.Threading.Tasks.Task SaveProfile(Account account, string idToken);

        /// <summary>
        /// 계정 프로필 로드. 없으면 null.
        /// </summary>
        System.Threading.Tasks.Task<Account> LoadProfile(string uid, string idToken);

        /// <summary>
        /// 전적 데이터 저장.
        /// </summary>
        System.Threading.Tasks.Task SaveStats(PlayerStats stats, string uid, string idToken);

        /// <summary>
        /// 전적 데이터 로드.
        /// </summary>
        System.Threading.Tasks.Task<PlayerStats> LoadStats(string uid, string idToken);

        /// <summary>
        /// 편성 데이터 저장.
        /// </summary>
        System.Threading.Tasks.Task SaveGarage(object roster, string uid, string idToken);

        /// <summary>
        /// 편성 데이터 로드.
        /// </summary>
        System.Threading.Tasks.Task<object> LoadGarage(string uid, string idToken);

        /// <summary>
        /// 설정 저장.
        /// </summary>
        System.Threading.Tasks.Task SaveSettings(UserSettings settings, string uid, string idToken);

        /// <summary>
        /// 설정 로드.
        /// </summary>
        System.Threading.Tasks.Task<UserSettings> LoadSettings(string uid, string idToken);

        /// <summary>
        /// 계정 문서 전체 삭제.
        /// </summary>
        System.Threading.Tasks.Task DeleteAccount(string uid, string idToken);
    }
}
