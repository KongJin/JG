using System;

namespace Features.Account.Domain
{
    /// <summary>
    /// 사용자 설정 (ValueObject).
    /// </summary>
    [Serializable]
    public sealed class UserSettings
    {
        public float masterVolume = 1f;
        public float bgmVolume = 0.8f;
        public float sfxVolume = 1f;
        public string language = "ko";

        public UserSettings() { }

        public UserSettings Clone() => (UserSettings)MemberwiseClone();
    }
}
