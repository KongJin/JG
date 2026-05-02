using Features.Wave.Application;
using Photon.Pun;
using Shared.Gameplay;

namespace Features.Wave.Infrastructure
{
    public static class RoomDifficultyReader
    {
        public static int ReadPresetId()
        {
            var room = PhotonNetwork.CurrentRoom;
            if (room == null)
                return DifficultySpawnScale.PresetNormal;

            if (!room.CustomProperties.TryGetValue(DifficultyPreset.RoomPropertyKey, out var raw) ||
                raw == null)
                return DifficultySpawnScale.PresetNormal;

            return raw switch
            {
                int i => i,
                byte b => b,
                short s => s,
                long l => (int)l,
                _ => DifficultySpawnScale.PresetNormal,
            };
        }
    }
}
