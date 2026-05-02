using Shared.Gameplay;
using Shared.Kernel;

namespace Features.Lobby.Domain
{
    public static class LobbyRule
    {
        public static Result ValidateRoomName(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                return Result.Failure("Room name is required.");
            }

            var normalized = roomName.Trim();
            if (normalized.Length < 2)
            {
                return Result.Failure("Room name must be at least 2 characters.");
            }

            return Result.Success();
        }

        public static Result EnsureUniqueRoomName(Lobby lobby, string roomName)
        {
            if (lobby == null)
            {
                return Result.Failure("Lobby is required.");
            }

            if (lobby.FindRoomByName(roomName) != null)
            {
                return Result.Failure("Room name already exists.");
            }

            return Result.Success();
        }

        /// <summary>0 Normal, 1 Easy, 2 Hard.</summary>
        public static Result ValidateDifficultyPreset(int presetId)
        {
            if (!DifficultyPreset.IsDefined(presetId))
                return Result.Failure("Difficulty preset must be 0 (Normal), 1 (Easy), or 2 (Hard).");
            return Result.Success();
        }

        public static Result CanStartGame(Room room)
        {
            if (room == null)
            {
                return Result.Failure("Room is required.");
            }

            if (!room.CanStartGame())
            {
                return Result.Failure("All players must be ready and room must have at least two members.");
            }

            return Result.Success();
        }
    }
}
