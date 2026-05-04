namespace Features.Lobby.Presentation
{
    internal readonly struct LobbyCreateRoomInput
    {
        public LobbyCreateRoomInput(
            string roomName,
            int capacity,
            string displayName,
            int difficultyPresetId)
        {
            RoomName = roomName;
            Capacity = capacity;
            DisplayName = displayName;
            DifficultyPresetId = difficultyPresetId;
        }

        public string RoomName { get; }
        public int Capacity { get; }
        public string DisplayName { get; }
        public int DifficultyPresetId { get; }
    }
}
