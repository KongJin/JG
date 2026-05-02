namespace Features.Lobby.Presentation
{
    public static class DifficultyPresetFormatter
    {
        public static string ToShortLabel(int presetId)
        {
            return presetId switch
            {
                1 => "Easy",
                2 => "Hard",
                _ => "Normal",
            };
        }
    }
}
