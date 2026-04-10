namespace Features.Wave.Infrastructure
{
    /// <summary>
    /// Wave가 Room CustomProperties에서 읽는 키. Lobby write-side 코드와 동일 문자열을 유지한다.
    /// </summary>
    public static class WaveRoomPropertyKeys
    {
        public const string DifficultyPreset = "difficultyPreset";
    }
}
