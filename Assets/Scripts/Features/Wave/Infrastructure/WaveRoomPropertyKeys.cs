namespace Features.Wave.Infrastructure
{
    /// <summary>
    /// Wave가 Room CustomProperties에서 읽는 키. Lobby의 Room 난이도 키 문자열과 동일해야 한다(state_ownership.md).
    /// </summary>
    public static class WaveRoomPropertyKeys
    {
        public const string DifficultyPreset = "difficultyPreset";
    }
}
