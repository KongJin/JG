using System;

namespace Features.Account.Domain
{
    /// <summary>
    /// 계정 엔티티. Firebase UID 기반.
    /// </summary>
    [Serializable]
    public sealed class AccountProfile
    {
        public string uid;
        public string displayName;
        public string authType; // "anonymous" | "google"
        public long createdAtUnixMs;
        public long lastNicknameChangeUnixMs;

        public AccountProfile() { }

        public AccountProfile(string uid, string authType)
        {
            this.uid = uid;
            this.displayName = uid.Substring(0, Math.Min(8, uid.Length));
            this.authType = authType;
            this.createdAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            this.lastNicknameChangeUnixMs = 0;
        }

        public DateTime CreatedAt => DateTimeOffset.FromUnixTimeMilliseconds(createdAtUnixMs).UtcDateTime;

        public string DefaultDisplayName => uid?.Substring(0, Math.Min(8, uid.Length)) ?? string.Empty;
    }
}
