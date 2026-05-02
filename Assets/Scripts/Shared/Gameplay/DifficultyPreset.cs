namespace Shared.Gameplay
{
    public static class DifficultyPreset
    {
        public const int Normal = 0;
        public const int Easy = 1;
        public const int Hard = 2;
        public const string RoomPropertyKey = "difficultyPreset";

        public static bool IsDefined(int presetId)
        {
            return presetId >= Normal && presetId <= Hard;
        }

        public static string ToShortLabel(int presetId)
        {
            return presetId switch
            {
                Easy => "Easy",
                Hard => "Hard",
                _ => "Normal",
            };
        }
    }
}
