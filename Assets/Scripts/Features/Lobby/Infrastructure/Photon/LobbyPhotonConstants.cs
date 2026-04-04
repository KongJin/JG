namespace Features.Lobby.Infrastructure.Photon
{
    internal static class LobbyPhotonConstants
    {
        internal const byte GameStartedEventCode = 100;
        internal const string RoomDisplayNameKey = "roomDisplayName";
        /// <summary>int 프리셋: 0 Normal, 1 Easy, 2 Hard. WaveRoomPropertyKeys.DifficultyPreset와 동일 문자열.</summary>
        internal const string DifficultyPresetKey = "difficultyPreset";
        internal const string MemberIdKey = "memberId";
        internal const string TeamKey = "team";
        internal const string IsReadyKey = "isReady";
        internal const string DisplayNameKey = "displayName";
    }
}
