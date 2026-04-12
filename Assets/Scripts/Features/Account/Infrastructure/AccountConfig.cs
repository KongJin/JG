using UnityEngine;

namespace Features.Account.Infrastructure
{
    /// <summary>
    /// Firebase 계정 설정. ScriptableObject로 Inspector에서 설정.
    /// </summary>
    [CreateAssetMenu(fileName = "AccountConfig", menuName = "Account/AccountConfig")]
    public sealed class AccountConfig : ScriptableObject
    {
        [Header("Firebase")]
        public string firebaseApiKey;
        public string projectId;

        [Header("Google OAuth (WebGL)")]
        [Tooltip("Google OAuth Web Client ID (Google Cloud Console에서 발급)")]
        public string googleWebClientId;
    }
}
